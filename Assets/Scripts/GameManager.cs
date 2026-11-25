
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game References")]
    public GameState gameState;
    public AIController aiController;
    public Mixed_Reality_UI mixedRealityUI;
    public GameObject startPanel;
    public GameObject battleUIRoot;

    [Header("Game Settings")]
    public int startingHealth = 20;
    public int startingHandSize = 8;

    [Header("Shield Settings")]
    [SerializeField] private int maxShield = 8;

    [Header("Card Visual Effects")]
    public CardEffectPlayer cardEffectPlayer;

    [Header("Turn Presentation")]
    [SerializeField] private float actionMessageDuration = 3f;
    [SerializeField] private float effectWaitTimeout = 60f;

    [Header("Research Evaluation Logging")]
    [SerializeField] private string participantId;

    private GameEvaluationLogger evaluationLogger;
    private bool isProcessingTurn = false;

    public bool IsProcessingTurn => isProcessingTurn;

    public event Action<Card, bool> CardPlayed;
    public event Action GameEnded;

    private void Awake()
    {
        evaluationLogger = new GameEvaluationLogger();

        if (gameState == null)
            gameState = FindFirstObjectByType<GameState>();

        if (aiController == null)
            aiController = FindFirstObjectByType<AIController>();

        if (mixedRealityUI == null)
            mixedRealityUI = FindFirstObjectByType<Mixed_Reality_UI>();

        if (cardEffectPlayer == null)
            cardEffectPlayer = FindFirstObjectByType<CardEffectPlayer>();
    }

    // START GAME
    // scannedPlayerHand: Card instances created from the detected physical cards
    // (already shown on the board). These exact instances become the player's hand.
    // When null, the player's hand is dealt randomly (previous behaviour).
    public void StartGame(IList<Card> scannedPlayerHand = null)
    {
        if (gameState == null)
        {
            Debug.LogError("GameManager: GameState is not assigned.");
            return;
        }

        if (startingHealth < 1)
        {
            Debug.LogError("Starting health must be at least 1.");
            return;
        }

        if (startingHandSize < 1 || startingHandSize > 8)
        {
            Debug.LogError("Starting hand size must be between 1 and 8.");
            return;
        }

        if (maxShield < 0)
        {
            Debug.LogError("Maximum shield cannot be negative.");
            return;
        }

        if (evaluationLogger != null && evaluationLogger.IsGameActive)
            evaluationLogger.FinishGame(
                "Abandoned",
                gameState,
                "Game restarted"
            );

        StopAllCoroutines();
        isProcessingTurn = false;

        gameState.currentTurn = 0;
        gameState.gameOver = false;

        gameState.players.Clear();
        gameState.drawPile.Clear();
        gameState.discardPile.Clear();
        gameState.playerDiscardPile.Clear();
        gameState.aiDiscardPile.Clear();
        gameState.recentPlayerActions.Clear();

        if (cardEffectPlayer != null)
        {
            cardEffectPlayer.ClearEffect();
            cardEffectPlayer.ClearAllShields();
        }

        Player humanPlayer = CreatePlayer();
        Player aiPlayer = CreatePlayer();

        gameState.players.Add(humanPlayer);
        gameState.players.Add(aiPlayer);

        List<Card> deck = CreateDeck();
        ShuffleDeck(deck);

        // Player hand: the scanned physical cards first.
        if (scannedPlayerHand != null)
        {
            foreach (Card scanned in scannedPlayerHand)
            {
                if (scanned == null ||
                    humanPlayer.hand.Count >= startingHandSize)
                    break;

                int index = deck.FindIndex(c =>
                    c.suit == scanned.suit && c.rank == scanned.rank);

                if (index < 0)
                {
                    Debug.LogWarning(
                        $"Scanned card {scanned.suit} {scanned.rank} is not in the deck or was duplicated."
                    );
                    continue;
                }

                // Use the scanned instance (it is already on the board).
                humanPlayer.hand.Add(scanned);
                deck.RemoveAt(index);
            }
        }

        // Top up the player's hand if fewer cards were scanned than needed.
        while (humanPlayer.hand.Count < startingHandSize && deck.Count > 0)
        {
            humanPlayer.hand.Add(deck[0]);
            deck.RemoveAt(0);
        }

        // AI hand is always random from what remains.
        while (aiPlayer.hand.Count < startingHandSize && deck.Count > 0)
        {
            aiPlayer.hand.Add(deck[0]);
            deck.RemoveAt(0);
        }

        gameState.drawPile.AddRange(deck);

        gameState.UpdateState();

        if (evaluationLogger == null)
            evaluationLogger = new GameEvaluationLogger();

        participantId = GameEvaluationLogger.StartNewParticipant();

        Debug.Log($"[Research Logging] Participant: {participantId}");

        evaluationLogger.BeginGame(participantId, startingHealth);

        if (startPanel != null)
            startPanel.SetActive(false);

        if (battleUIRoot != null)
            battleUIRoot.SetActive(true);

        if (mixedRealityUI != null)
            mixedRealityUI.PrepareForNewGame();

        UpdateVisuals();
        SyncShieldEffects();
    }

    // END MATCH
    public void EndMatch()
    {
        if (evaluationLogger != null && evaluationLogger.IsGameActive)
            evaluationLogger.FinishGame(
                "Abandoned",
                gameState,
                "Match ended manually"
            );

        StopAllCoroutines();
        isProcessingTurn = false;

        if (cardEffectPlayer != null)
        {
            cardEffectPlayer.ClearEffect();
            cardEffectPlayer.ClearAllShields();
        }

        if (battleUIRoot != null)
            battleUIRoot.SetActive(false);
    }

    private Player CreatePlayer()
    {
        return new Player
        {
            health = startingHealth,
            maxHealth = startingHealth,
            shield = 0,
            hand = new List<Card>()
        };
    }

    private List<Card> CreateDeck()
    {
        List<Card> deck = new List<Card>();
        int[] ranks = { 2, 4, 6, 8 };

        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            foreach (int rank in ranks)
            {
                deck.Add(new Card(
                    suit,
                    rank,
                    Card.GetAbilityForSuit(suit)
                ));
            }
        }

        return deck;
    }

    private void ShuffleDeck(List<Card> deck)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);

            Card temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    // CARD RECOGNITION
    public bool HandleRecognizedCard(Suit suit, int rank)
    {
        if (gameState == null ||
            gameState.gameOver ||
            gameState.players == null ||
            gameState.players.Count < 2)
            return false;

        if (gameState.currentTurn != 0 || isProcessingTurn)
            return false;

        Player player = gameState.players[0];

        if (player.hand == null)
            return false;

        Card matchedCard = player.hand.Find(card =>
            card != null &&
            card.suit == suit &&
            card.rank == rank &&
            card.IsValid()
        );

        if (matchedCard == null)
        {
            Debug.LogWarning($"Card {suit} {rank} is not in hand.");

            if (mixedRealityUI != null)
                mixedRealityUI.showInvalidCardMessage();

            return false;
        }

        if (mixedRealityUI != null)
            mixedRealityUI.setCardPresent(true);

        ProcessPlayerCard(matchedCard);

        return true;
    }

    // PLAYER TURN
    public void ProcessPlayerCard(Card card)
    {
        if (gameState == null ||
            gameState.gameOver ||
            isProcessingTurn ||
            gameState.players == null ||
            gameState.players.Count < 2 ||
            gameState.currentTurn != 0)
            return;

        Player player = gameState.players[0];

        if (card == null ||
            !card.IsValid() ||
            player.hand == null ||
            !player.hand.Contains(card))
        {
            Debug.LogWarning("Invalid player card.");
            return;
        }

        StartCoroutine(ProcessFullTurnSequence(card));
    }

    // PLAYER + AI TURN SEQUENCE
    private IEnumerator ProcessFullTurnSequence(Card playerCard)
    {
        isProcessingTurn = true;

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        try
        {
            // PLAYER ACTION
            if (!player.hand.Remove(playerCard))
                yield break;

            gameState.RecordPlayerAction(playerCard.ability);

            ApplyAbility(playerCard, player, ai);
            DiscardCard(playerCard, player);

            CheckWinner();

            yield return WaitForActionPresentation();

            if (gameState.gameOver)
                yield break;

            // AI TURN
            EndTurn();

            if (gameState.gameOver)
                yield break;

            yield return new WaitForSeconds(0.25f);

            Card aiCard = null;

            if (aiController != null &&
                ai.hand != null &&
                ai.hand.Count > 0)
            {
                aiCard = aiController.SelectCard(gameState);
            }

            if (aiCard == null ||
                ai.hand == null ||
                !ai.hand.Contains(aiCard))
            {
                Debug.LogWarning("AI has no legal card.");
                EndGameByRemainingHealth();
            }
            else
            {
                ai.hand.Remove(aiCard);

                ApplyAbility(aiCard, ai, player);
                DiscardCard(aiCard, ai);

                CheckWinner();

                yield return WaitForActionPresentation();
            }

            if (gameState.gameOver)
                yield break;

            // RETURN TO PLAYER
            EndTurn();
            CheckEndOfDeck();
        }
        finally
        {
            isProcessingTurn = false;
        }
    }

    // WAIT FOR MESSAGE AND EFFECT
    private IEnumerator WaitForActionPresentation()
    {
        float elapsed = 0f;

        while (elapsed < actionMessageDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        while (
            cardEffectPlayer != null &&
            (
                cardEffectPlayer.IsPlayingQueuedEffect ||
                cardEffectPlayer.QueuedEffectCount > 0 ||
                cardEffectPlayer.ActiveTemporaryEffectCount > 0
            ) &&
            elapsed < effectWaitTimeout
        )
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (cardEffectPlayer != null &&
            (
                cardEffectPlayer.IsPlayingQueuedEffect ||
                cardEffectPlayer.QueuedEffectCount > 0 ||
                cardEffectPlayer.ActiveTemporaryEffectCount > 0
            ))
        {
            Debug.LogWarning("Timed out waiting for particle effects.");
        }

        yield return null;
    }

    // STANDALONE AI TURN
    public void ProcessAITurn()
    {
        if (gameState == null ||
            gameState.gameOver ||
            isProcessingTurn ||
            gameState.players == null ||
            gameState.players.Count < 2 ||
            gameState.currentTurn != 1)
            return;

        StartCoroutine(ProcessStandaloneAITurn());
    }

    private IEnumerator ProcessStandaloneAITurn()
    {
        isProcessingTurn = true;

        try
        {
            Player ai = gameState.players[1];
            Player player = gameState.players[0];

            Card aiCard = null;

            if (aiController != null &&
                ai.hand != null &&
                ai.hand.Count > 0)
            {
                aiCard = aiController.SelectCard(gameState);
            }

            if (aiCard != null && ai.hand.Contains(aiCard))
            {
                ai.hand.Remove(aiCard);

                ApplyAbility(aiCard, ai, player);
                DiscardCard(aiCard, ai);

                CheckWinner();

                yield return WaitForActionPresentation();
            }
            else
            {
                Debug.LogWarning("AI has no legal card.");
                EndGameByRemainingHealth();
            }

            if (!gameState.gameOver)
            {
                EndTurn();
                CheckEndOfDeck();
            }
        }
        finally
        {
            isProcessingTurn = false;
        }
    }

    // APPLY CARD ABILITY
    public void ApplyAbility(Card card, Player user, Player target)
    {
        if (card == null ||
            user == null ||
            target == null ||
            gameState == null ||
            gameState.gameOver ||
            user.health <= 0 ||
            target.health <= 0)
            return;

        bool isHuman =
            gameState.players != null &&
            gameState.players.Count >= 2 &&
            user == gameState.players[0];

        string actor = isHuman ? "You" : "Opponent";
        string targetName = isHuman ? "Opponent" : "You";

        int strength = card.rank;

        switch (card.ability)
        {
            case AbilityType.Attack:
            {
                int damage = DealDamage(
                    target,
                    strength,
                    out int absorbed
                );

                if (cardEffectPlayer != null)
                    cardEffectPlayer.PlayAttack(isHuman);

                string message = damage > 0
                    ? $"{targetName} -{damage} HP"
                    : "Attack blocked!";

                if (absorbed > 0 && damage > 0)
                    message += $" ({absorbed} blocked)";

                ShowAbilityMessage(message);
                break;
            }

            case AbilityType.Heal:
            {
                int healed = user.heal(strength);

                if (cardEffectPlayer != null)
                    cardEffectPlayer.PlayHeal(isHuman);

                ShowAbilityMessage($"{actor} +{healed} HP");
                break;
            }

            case AbilityType.Drain:
            {
                int damage = DealDamage(
                    target,
                    strength,
                    out int absorbed
                );

                int healed = user.heal(strength);

                if (cardEffectPlayer != null)
                    cardEffectPlayer.PlayDrain(isHuman);

                string message =
                    $"Drain: {targetName} -{damage} HP, {actor} +{healed} HP";

                if (absorbed > 0)
                    message += $" ({absorbed} blocked)";

                ShowAbilityMessage(message);
                break;
            }

            case AbilityType.Shield:
            {
                int previousShield = user.shield;

                int shieldToAdd = Mathf.Min(
                    strength,
                    Mathf.Max(0, maxShield - user.shield)
                );

                user.addShield(shieldToAdd);

                int actualShieldGained =
                    user.shield - previousShield;

                ShowAbilityMessage(
                    $"{actor} +{actualShieldGained} Shield"
                );

                break;
            }
        }

        gameState.UpdateState();
        UpdateVisuals();
        SyncShieldEffects();
    }

    private void ShowAbilityMessage(string message)
    {
        if (mixedRealityUI != null)
        {
            mixedRealityUI.showAbilityMessage(
                message,
                actionMessageDuration
            );
        }
    }

    // DAMAGE
    private int DealDamage(
        Player target,
        int damage,
        out int absorbed)
    {
        absorbed = 0;

        if (target == null)
            return 0;

        damage = Mathf.Max(0, damage);

        int previousShield = target.shield;
        int previousHealth = target.health;

        target.takeDamage(damage);

        absorbed = previousShield - target.shield;

        int actualHealthDamage =
            previousHealth - target.health;

        return actualHealthDamage;
    }

    // REPLACEMENT CARD
    private void DrawReplacement(Player player)
    {
        if (player == null || gameState == null)
            return;

        int playerIndex = gameState.players.IndexOf(player);

        if (playerIndex < 0 || playerIndex > 1)
            return;

        if (gameState.drawPile.Count == 0)
            return;

        Card drawnCard = gameState.drawPile[0];

        gameState.drawPile.RemoveAt(0);
        player.hand.Add(drawnCard);
    }

    // DISCARD
    private void DiscardCard(Card card, Player owner)
    {
        if (card == null || owner == null || gameState == null)
            return;

        gameState.discardPile.Add(card);

        int ownerIndex = gameState.players.IndexOf(owner);

        if (ownerIndex == 0)
            gameState.playerDiscardPile.Add(card);
        else if (ownerIndex == 1)
            gameState.aiDiscardPile.Add(card);

        if (evaluationLogger != null && ownerIndex >= 0)
        {
            string actor = ownerIndex == 0 ? "Player" : "AI";

            int playerHp = gameState.players.Count > 0
                ? gameState.players[0].health : 0;

            int aiHp = gameState.players.Count > 1
                ? gameState.players[1].health : 0;

            evaluationLogger.RecordCardAction(
                actor,
                card,
                playerHp,
                aiHp
            );
        }

        CardPlayed?.Invoke(card, ownerIndex == 0);
    }

    // WINNER CHECK
    public void CheckWinner()
    {
        if (gameState == null ||
            gameState.gameOver ||
            gameState.players == null ||
            gameState.players.Count < 2)
            return;

        Player humanPlayer = gameState.players[0];
        Player aiPlayer = gameState.players[1];

        if (humanPlayer.health <= 0)
            EndGame("AI wins!", false);
        else if (aiPlayer.health <= 0)
            EndGame("You win!", true);
    }

    // HEALTH-BASED WINNER
    public void EndGameByRemainingHealth()
    {
        if (gameState == null ||
            gameState.gameOver ||
            gameState.players == null ||
            gameState.players.Count < 2)
            return;

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        if (player.health > ai.health)
        {
            EndGame("You win! More HP remaining.", true, "VICTORY!");
        }
        else if (ai.health > player.health)
        {
            EndGame("Opponent wins! More HP remaining.", false, "DEFEAT!");
        }
        else
        {
            EndGame("Draw! Equal HP.", false, "DRAW!");
        }
    }

    // END TURN
    public void EndTurn()
    {
        if (gameState == null || gameState.gameOver)
            return;

        gameState.currentTurn = 1 - gameState.currentTurn;

        gameState.UpdateState();

        UpdateVisuals();
        SyncShieldEffects();
    }

    // CLEAR SHIELDS
    private void ClearAllShieldState()
    {
        if (gameState != null && gameState.players != null)
        {
            foreach (Player player in gameState.players)
            {
                if (player != null)
                    player.clearShield();
            }
        }
    }

    // GAME OVER
    private void EndGame(
        string message,
        bool playerWon,
        string result = null)
    {
        if (gameState == null || gameState.gameOver)
            return;

        gameState.gameOver = true;

        string evaluationOutcome =
            (result == "DRAW!" ||
             message.StartsWith("DRAW", StringComparison.OrdinalIgnoreCase))
                ? "Draw"
                : (playerWon ? "Player Win" : "AI Win");

        if (evaluationLogger != null)
            evaluationLogger.FinishGame(
                evaluationOutcome,
                gameState,
                message
            );

        ClearAllShieldState();

        string finalResult = result ??
            (playerWon ? "VICTORY!" : "DEFEAT!");

        UpdateVisuals();

        if (mixedRealityUI != null)
        {
            mixedRealityUI.showGameOver(
                finalResult,
                message
            );
        }

        StartCoroutine(FinishGameOverSequence(finalResult));
    }

    private IEnumerator FinishGameOverSequence(string result)
    {
        yield return new WaitForSeconds(actionMessageDuration);

        float elapsed = 0f;

        while (
            cardEffectPlayer != null &&
            (
                cardEffectPlayer.IsPlayingQueuedEffect ||
                cardEffectPlayer.QueuedEffectCount > 0 ||
                cardEffectPlayer.ActiveTemporaryEffectCount > 0
            ) &&
            elapsed < effectWaitTimeout
        )
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (mixedRealityUI != null)
        {
            mixedRealityUI.ShowFinalGameOverPanel(result);
        }
        else if (cardEffectPlayer != null)
        {
            cardEffectPlayer.ClearEffect();
            cardEffectPlayer.ClearAllShields();
        }

        GameEnded?.Invoke();
    }

    // UI UPDATE
    private void UpdateVisuals()
    {
        if (mixedRealityUI == null ||
            gameState == null ||
            gameState.players == null ||
            gameState.players.Count < 2)
            return;

        Player humanPlayer = gameState.players[0];
        Player aiPlayer = gameState.players[1];

        mixedRealityUI.updateUI(
            humanPlayer.health,
            humanPlayer.shield,
            aiPlayer.health,
            aiPlayer.shield,
            gameState.currentTurn == 0,
            gameState.gameOver
        );
    }

    // SHIELD VISUALS
    private void SyncShieldEffects()
    {
        if (cardEffectPlayer == null ||
            gameState == null ||
            gameState.players == null ||
            gameState.players.Count < 2)
            return;

        Player humanPlayer = gameState.players[0];
        Player aiPlayer = gameState.players[1];

        bool playerHasActiveShield =
            !gameState.gameOver &&
            humanPlayer.health > 0 &&
            humanPlayer.shield > 0;

        bool aiHasActiveShield =
            !gameState.gameOver &&
            aiPlayer.health > 0 &&
            aiPlayer.shield > 0;

        cardEffectPlayer.SetShield(true, playerHasActiveShield);
        cardEffectPlayer.SetShield(false, aiHasActiveShield);
    }

    // END OF DECK
    private void CheckEndOfDeck()
    {
        if (gameState == null ||
            gameState.gameOver ||
            gameState.players == null ||
            gameState.players.Count < 2)
            return;

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        bool playerFinished =
            player.hand == null || player.hand.Count == 0;

        bool aiFinished =
            ai.hand == null || ai.hand.Count == 0;

        if (!playerFinished || !aiFinished)
            return;

        if (player.health > ai.health)
        {
            EndGame("You win! More HP remaining.", true);
        }
        else if (ai.health > player.health)
        {
            EndGame("Opponent wins! More HP remaining.", false);
        }
        else
        {
            EndGame("Draw! Equal HP.", false, "DRAW!");
        }

        gameState.UpdateState();
    }
}
