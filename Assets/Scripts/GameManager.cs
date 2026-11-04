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

    private bool isProcessingTurn = false;

    public bool IsProcessingTurn => isProcessingTurn;

    private void Awake()
    {
        if (gameState == null)
            gameState = FindFirstObjectByType<GameState>();

        if (aiController == null)
            aiController = FindFirstObjectByType<AIController>();

        if (mixedRealityUI == null)
            mixedRealityUI = FindFirstObjectByType<Mixed_Reality_UI>();

        if (cardEffectPlayer == null)
            cardEffectPlayer = FindFirstObjectByType<CardEffectPlayer>();
    }

    // ----------------------------------
    // START GAME
    // ----------------------------------

    public void StartGame()
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

        // Deal unique cards alternately.
        for (int i = 0; i < startingHandSize; i++)
        {
            humanPlayer.hand.Add(deck[0]);
            deck.RemoveAt(0);

            aiPlayer.hand.Add(deck[0]);
            deck.RemoveAt(0);
        }

        // Any undealt cards remain in the draw pile.
        gameState.drawPile.AddRange(deck);

        gameState.UpdateState();

        if (startPanel != null)
            startPanel.SetActive(false);

        if (battleUIRoot != null)
            battleUIRoot.SetActive(true);

        if (mixedRealityUI != null)
            mixedRealityUI.PrepareForNewGame();

        UpdateVisuals();

        Debug.Log("Game started.");
        Debug.Log($"Player HP: {humanPlayer.health}");
        Debug.Log($"AI HP: {aiPlayer.health}");
        Debug.Log($"Player cards: {humanPlayer.hand.Count}");
        Debug.Log($"AI cards: {aiPlayer.hand.Count}");
        Debug.Log($"Cards remaining in draw pile: {gameState.drawPile.Count}");
        Debug.Log($"Maximum shield: {maxShield}");
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

    // ----------------------------------
    // CARD RECOGNITION
    // ----------------------------------

    public bool HandleRecognizedCard(Suit suit, int rank)
    {
        if (gameState == null ||
            gameState.gameOver ||
            gameState.players == null ||
            gameState.players.Count < 2)
        {
            return false;
        }

        if (gameState.currentTurn != 0 || isProcessingTurn)
        {
            Debug.LogWarning("It is not currently the player's turn.");
            return false;
        }

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
            Debug.LogWarning(
                $"Card {suit} {rank} is not in the player's hand."
            );

            if (mixedRealityUI != null)
                mixedRealityUI.showInvalidCardMessage();

            return false;
        }

        if (mixedRealityUI != null)
            mixedRealityUI.setCardPresent(true);

        ProcessPlayerCard(matchedCard);

        return true;
    }

    // ----------------------------------
    // PLAYER TURN ENTRY POINT
    // ----------------------------------

    public void ProcessPlayerCard(Card card)
    {
        if (gameState == null ||
            gameState.gameOver ||
            isProcessingTurn ||
            gameState.players == null ||
            gameState.players.Count < 2 ||
            gameState.currentTurn != 0)
        {
            return;
        }

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

    // ----------------------------------
    // COMPLETE PLAYER + AI TURN SEQUENCE
    // ----------------------------------

    private IEnumerator ProcessFullTurnSequence(Card playerCard)
    {
        isProcessingTurn = true;

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        try
        {
            // PLAYER ACTION

            Debug.Log("========== PLAYER TURN ==========");

            if (mixedRealityUI != null)
                mixedRealityUI.showPlayerTurnMessage();

            if (!player.hand.Remove(playerCard))
                yield break;

            gameState.RecordPlayerAction(playerCard.ability);

            Debug.Log($"Player played {playerCard}");

            ApplyAbility(playerCard, player, ai);

            DiscardCard(playerCard, player);

            CheckWinner();

            yield return WaitForActionPresentation();

            if (gameState.gameOver)
                yield break;

            // SWITCH TO AI TURN

            EndTurn();

            if (gameState.gameOver)
                yield break;

            // AI ACTION

            Debug.Log("========== OPPONENT TURN ==========");

            if (mixedRealityUI != null)
                mixedRealityUI.showOpponentTurnMessage();

            yield return new WaitForSeconds(0.25f);

            Card aiCard = null;

            if (aiController != null && ai.hand.Count > 0)
                aiCard = aiController.SelectCard(gameState);

            if (aiCard == null ||
                ai.hand == null ||
                !ai.hand.Contains(aiCard))
            {
                Debug.Log("AI has no legal card and passes.");
            }
            else
            {
                ai.hand.Remove(aiCard);

                Debug.Log($"AI played {aiCard}");

                ApplyAbility(aiCard, ai, player);

                DiscardCard(aiCard, ai);

                CheckWinner();

                yield return WaitForActionPresentation();
            }

            if (gameState.gameOver)
                yield break;

            // RETURN TO PLAYER TURN

            EndTurn();

            Debug.Log("========== PLAYER TURN READY ==========");

            CheckEndOfDeck();
        }
        finally
        {
            isProcessingTurn = false;
        }
    }

    // ----------------------------------
    // WAIT FOR MESSAGE AND EFFECT
    // ----------------------------------

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
            Debug.LogWarning(
                "GameManager: Timed out waiting for particle effects."
            );
        }

        yield return null;
    }

    // ----------------------------------
    // AI TURN COMPATIBILITY
    // ----------------------------------

    public void ProcessAITurn()
    {
        if (gameState == null ||
            gameState.gameOver ||
            isProcessingTurn ||
            gameState.players == null ||
            gameState.players.Count < 2 ||
            gameState.currentTurn != 1)
        {
            return;
        }

        StartCoroutine(ProcessStandaloneAITurn());
    }

    private IEnumerator ProcessStandaloneAITurn()
    {
        isProcessingTurn = true;

        try
        {
            Player ai = gameState.players[1];
            Player player = gameState.players[0];

            if (mixedRealityUI != null)
                mixedRealityUI.showOpponentTurnMessage();

            Card aiCard = null;

            if (aiController != null && ai.hand.Count > 0)
                aiCard = aiController.SelectCard(gameState);

            if (aiCard != null && ai.hand.Contains(aiCard))
            {
                ai.hand.Remove(aiCard);

                Debug.Log($"AI played {aiCard}");

                ApplyAbility(aiCard, ai, player);

                DiscardCard(aiCard, ai);

                CheckWinner();

                yield return WaitForActionPresentation();
            }
            else
            {
                Debug.Log("AI has no legal card and passes.");
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

    // ----------------------------------
    // APPLY CARD ABILITY
    // ----------------------------------

    public void ApplyAbility(Card card, Player user, Player target)
    {
        if (card == null ||
            user == null ||
            target == null ||
            gameState == null ||
            gameState.gameOver ||
            user.health <= 0 ||
            target.health <= 0)
        {
            return;
        }

        bool isHuman =
            gameState.players != null &&
            gameState.players.Count >= 2 &&
            user == gameState.players[0];

        string actor = isHuman ? "Player" : "Opponent";
        string targetName = isHuman ? "opponent" : "player";

        int strength = card.rank;

        Debug.Log(
            $"[ABILITY START] {actor} played {card.suit} {card.rank} " +
            $"({card.ability}) | " +
            $"User Shield: {user.shield} | Target Shield: {target.shield}"
        );

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
                    ? $"{actor} dealt {damage} damage to {targetName}!"
                    : $"{actor}'s attack was blocked!";

                if (absorbed > 0)
                    message += $" {absorbed} damage absorbed by shield.";

                ShowAbilityMessage(message);

                break;
            }

            case AbilityType.Heal:
            {
                int healed = user.heal(strength);

                if (cardEffectPlayer != null)
                    cardEffectPlayer.PlayHeal(isHuman);

                ShowAbilityMessage(
                    $"{actor} restored +{healed} HP!"
                );

                break;
            }

            case AbilityType.Drain:
            {
                // Damage opponent by the card's full rank.
                // Opponent shield absorbs damage before health.
                int damage = DealDamage(
                    target,
                    strength,
                    out int absorbed
                );

                // Heal user by the card's full rank,
                // even if opponent shield absorbs all damage.
                // Player.heal() prevents health exceeding maxHealth.
                int healed = user.heal(strength);

                if (cardEffectPlayer != null)
                    cardEffectPlayer.PlayDrain(isHuman);

                string message =
                    $"{actor} used Drain {strength}: " +
                    $"{damage} HP damage dealt, " +
                    $"{healed} HP restored.";

                if (absorbed > 0)
                {
                    message += $" {absorbed} damage absorbed by shield.";
                }

                ShowAbilityMessage(message);

                break;
            }

            case AbilityType.Shield:
            {
                int previousShield = user.shield;

                // Add shield without exceeding the maximum.
                int shieldToAdd = Mathf.Min(
                    strength,
                    Mathf.Max(0, maxShield - user.shield)
                );

                user.addShield(shieldToAdd);

                int actualShieldGained =
                    user.shield - previousShield;

                ShowAbilityMessage(
                    $"{actor} gained +{actualShieldGained} shield!"
                );

                break;
            }
        }

        Debug.Log(
            $"[ABILITY END] {actor} used {card.ability} | " +
            $"Player: {gameState.players[0].health} HP / " +
            $"{gameState.players[0].shield} Shield | " +
            $"AI: {gameState.players[1].health} HP / " +
            $"{gameState.players[1].shield} Shield"
        );

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

        Debug.Log($"Ability message: {message}");
    }

    // ----------------------------------
    // DAMAGE
    // ----------------------------------

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

        // Player.takeDamage() applies damage to shield first,
        // then applies any remaining damage to health.
        target.takeDamage(damage);

        // Calculate actual shield and health changes.
        absorbed = previousShield - target.shield;

        int actualHealthDamage =
            previousHealth - target.health;

        Debug.Log(
            $"[DAMAGE CALCULATION] Incoming: {damage} | " +
            $"Shield absorbed: {absorbed} | " +
            $"HP damage: {actualHealthDamage} | " +
            $"Remaining Shield: {target.shield} | " +
            $"Remaining HP: {target.health}"
        );

        return actualHealthDamage;
    }

    // ----------------------------------
    // REPLACEMENT CARD METHOD
    // ----------------------------------

    // Kept for compatibility, but NOT called during gameplay.
    private void DrawReplacement(Player player)
    {
        if (player == null || gameState == null)
            return;

        int playerIndex = gameState.players.IndexOf(player);

        if (playerIndex < 0 || playerIndex > 1)
            return;

        if (gameState.drawPile.Count == 0)
        {
            Debug.Log("No replacement cards available.");
            return;
        }

        Card drawnCard = gameState.drawPile[0];

        gameState.drawPile.RemoveAt(0);
        player.hand.Add(drawnCard);

        Debug.Log($"Replacement card drawn: {drawnCard}");
    }

    // ----------------------------------
    // DISCARD
    // ----------------------------------

    private void DiscardCard(Card card, Player owner)
    {
        if (card == null || owner == null || gameState == null)
            return;

        gameState.discardPile.Add(card);

        if (gameState.players.IndexOf(owner) == 0)
            gameState.playerDiscardPile.Add(card);
        else if (gameState.players.IndexOf(owner) == 1)
            gameState.aiDiscardPile.Add(card);
    }

    // ----------------------------------
    // WINNER CHECK
    // ----------------------------------

    public void CheckWinner()
    {
        if (gameState == null ||
            gameState.gameOver ||
            gameState.players == null ||
            gameState.players.Count < 2)
        {
            return;
        }

        Player humanPlayer = gameState.players[0];
        Player aiPlayer = gameState.players[1];

        if (humanPlayer.health <= 0)
            EndGame("AI wins!", false);
        else if (aiPlayer.health <= 0)
            EndGame("You win!", true);
    }

    // ----------------------------------
    // END TURN
    // ----------------------------------

    public void EndTurn()
    {
        if (gameState == null || gameState.gameOver)
            return;

        gameState.currentTurn = 1 - gameState.currentTurn;

        // Remaining shield carries over between turns.
        gameState.UpdateState();

        UpdateVisuals();
        SyncShieldEffects();

        if (mixedRealityUI != null)
        {
            if (gameState.currentTurn == 0)
                mixedRealityUI.showPlayerTurnMessage();
            else
                mixedRealityUI.showOpponentTurnMessage();
        }
    }

    // ----------------------------------
    // CLEAR SHIELDS
    // ----------------------------------

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

        if (cardEffectPlayer != null)
        {
            cardEffectPlayer.SetShield(true, false);
            cardEffectPlayer.SetShield(false, false);
        }
    }

    // ----------------------------------
    // GAME OVER
    // ----------------------------------

    private void EndGame(string message, bool playerWon)
    {
        if (gameState == null || gameState.gameOver)
            return;

        gameState.gameOver = true;

        ClearAllShieldState();

        Debug.Log(message);

        if (mixedRealityUI != null)
            mixedRealityUI.showGameOver(playerWon);

        UpdateVisuals();
    }

    // ----------------------------------
    // UI UPDATE
    // ----------------------------------

    private void UpdateVisuals()
    {
        if (mixedRealityUI == null ||
            gameState == null ||
            gameState.players == null ||
            gameState.players.Count < 2)
        {
            return;
        }

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

    // ----------------------------------
    // SHIELD VISUAL SYNCHRONIZATION
    // ----------------------------------

    private void SyncShieldEffects()
    {
        if (cardEffectPlayer == null ||
            gameState == null ||
            gameState.players == null ||
            gameState.players.Count < 2)
        {
            return;
        }

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

        cardEffectPlayer.SetShield(
            true,
            playerHasActiveShield
        );

        cardEffectPlayer.SetShield(
            false,
            aiHasActiveShield
        );
    }

    // ----------------------------------
    // END OF DECK CHECK
    // ----------------------------------

    private void CheckEndOfDeck()
    {
        if (gameState == null ||
            gameState.gameOver ||
            gameState.players == null ||
            gameState.players.Count < 2)
        {
            return;
        }

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        bool playerFinished =
            player.hand == null || player.hand.Count == 0;

        bool aiFinished =
            ai.hand == null || ai.hand.Count == 0;

        if (!playerFinished || !aiFinished)
            return;

        Debug.Log("========== ALL CARDS USED ==========");

        Debug.Log($"Final player health: {player.health}");
        Debug.Log($"Final AI health: {ai.health}");

        if (player.health > ai.health)
        {
            EndGame("PLAYER WINS! Higher remaining health.", true);
        }
        else if (ai.health > player.health)
        {
            EndGame("OPPONENT WINS! Higher remaining health.", false);
        }
        else
        {
            gameState.gameOver = true;

            ClearAllShieldState();

            Debug.Log("DRAW! Both players have equal health.");

            if (mixedRealityUI != null)
            {
                mixedRealityUI.showAbilityMessage(
                    "DRAW! Both players have equal health.",
                    actionMessageDuration
                );
            }

            UpdateVisuals();
        }

        gameState.UpdateState();

        Debug.Log("========== MATCH FINISHED ==========");
    }
}