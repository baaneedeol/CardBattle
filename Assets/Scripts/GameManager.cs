
using System;
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

    private bool isProcessingTurn = false;

    private void Awake()
    {
        if (gameState == null)
            gameState = FindFirstObjectByType<GameState>();

        if (aiController == null)
            aiController = FindFirstObjectByType<AIController>();

        if (mixedRealityUI == null)
            mixedRealityUI = FindFirstObjectByType<Mixed_Reality_UI>();
    }

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
            Debug.LogError(
                "Starting hand size must be between 1 and 8."
            );
            return;
        }

        gameState.currentTurn = 0;
        gameState.gameOver = false;

        gameState.players.Clear();
        gameState.drawPile.Clear();
        gameState.discardPile.Clear();
        gameState.playerDiscardPile.Clear();
        gameState.aiDiscardPile.Clear();
        gameState.recentPlayerActions.Clear();

        Player humanPlayer = CreatePlayer();
        Player aiPlayer = CreatePlayer();

        gameState.players.Add(humanPlayer);
        gameState.players.Add(aiPlayer);

        List<Card> deck = CreateDeck();
        ShuffleDeck(deck);

        // Deal unique cards alternately to both players.
        for (int i = 0; i < startingHandSize; i++)
        {
            humanPlayer.hand.Add(deck[0]);
            deck.RemoveAt(0);

            aiPlayer.hand.Add(deck[0]);
            deck.RemoveAt(0);
        }

        // Any undealt cards remain available to draw.
        gameState.drawPile.AddRange(deck);

        isProcessingTurn = false;
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
        Debug.Log($"Cards in draw pile: {gameState.drawPile.Count}");
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
        Player opponent = gameState.players[1];

        if (card == null ||
            !card.IsValid() ||
            player.hand == null ||
            !player.hand.Contains(card))
        {
            Debug.LogWarning("Invalid player card.");
            return;
        }

        isProcessingTurn = true;

        try
        {
            player.hand.Remove(card);
            gameState.RecordPlayerAction(card.ability);

            Debug.Log($"Player played {card}");

            ApplyAbility(card, player, opponent);
            CheckWinner();

            if (!gameState.gameOver)
            {
                // Draw before discarding this card, preventing
                // the same card from being immediately redrawn.
                DrawReplacement(player);
                DiscardCard(card, player);

                EndTurn();
                ProcessAITurn();
            }
            else
            {
                DiscardCard(card, player);
            }
        }
        finally
        {
            isProcessingTurn = false;
        }
    }

    public void ProcessAITurn()
    {
        if (gameState == null ||
            gameState.gameOver ||
            gameState.players == null ||
            gameState.players.Count < 2 ||
            gameState.currentTurn != 1)
        {
            return;
        }

        if (aiController == null)
        {
            Debug.LogError("AIController is not assigned.");
            return;
        }

        Player aiPlayer = gameState.players[1];
        Player humanPlayer = gameState.players[0];

        if (aiPlayer.hand == null || aiPlayer.hand.Count == 0)
            DrawReplacement(aiPlayer);

        Card selectedCard = aiController.SelectCard(gameState);

        if (selectedCard == null ||
            aiPlayer.hand == null ||
            !aiPlayer.hand.Contains(selectedCard))
        {
            Debug.Log("AI has no legal card and passes.");
            EndTurn();
            return;
        }

        aiPlayer.hand.Remove(selectedCard);

        Debug.Log($"AI played {selectedCard}");

        ApplyAbility(selectedCard, aiPlayer, humanPlayer);
        CheckWinner();

        if (!gameState.gameOver)
        {
            DrawReplacement(aiPlayer);
            DiscardCard(selectedCard, aiPlayer);
            EndTurn();
        }
        else
        {
            DiscardCard(selectedCard, aiPlayer);
        }
    }

    public void ApplyAbility(Card card, Player user, Player target)
    {
        if (card == null || user == null || target == null)
            return;

        bool isHuman =
            gameState != null &&
            gameState.players.Count >= 2 &&
            user == gameState.players[0];

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

                if (isHuman)
                {
                    if (mixedRealityUI != null)
                    {
                        mixedRealityUI.showAttackMessage(damage);
                        mixedRealityUI.showCardEffect("Attack", damage);

                        if (absorbed > 0)
                        {
                            mixedRealityUI.showAbilityMessage(
                                $"Opponent's shield absorbed {absorbed} damage."
                            );
                        }
                    }
                }
                else if (mixedRealityUI != null)
                {
                    mixedRealityUI.showAbilityMessage(
                        $"Opponent attacked! {damage} HP damage dealt."
                    );
                }

                break;
            }

            case AbilityType.Heal:
            {
                int healed = user.heal(strength);

                if (isHuman)
                {
                    if (mixedRealityUI != null)
                    {
                        mixedRealityUI.showHealMessage(healed);
                        mixedRealityUI.showCardEffect("Heal", healed);
                    }
                }
                else if (mixedRealityUI != null)
                {
                    mixedRealityUI.showAbilityMessage(
                        $"Opponent healed {healed} HP."
                    );
                }

                break;
            }

            case AbilityType.Drain:
            {
                int damage = DealDamage(
                    target,
                    strength,
                    out int absorbed
                );

                // This game rule heals by card rank, even when
                // a shield absorbs some or all incoming damage.
                int healed = user.heal(strength);

                if (isHuman)
                {
                    if (mixedRealityUI != null)
                    {
                        mixedRealityUI.showDrainMessage(damage, healed);
                        mixedRealityUI.showCardEffect(
                            "Drain", damage, healed
                        );

                        if (absorbed > 0)
                        {
                            mixedRealityUI.showAbilityMessage(
                                $"Opponent's shield absorbed {absorbed} damage."
                            );
                        }
                    }
                }
                else if (mixedRealityUI != null)
                {
                    mixedRealityUI.showAbilityMessage(
                        $"Opponent used Drain: {damage} HP damage, " +
                        $"{healed} HP restored."
                    );
                }

                break;
            }

            case AbilityType.Shield:
            {
                user.addShield(strength);

                if (isHuman)
                {
                    if (mixedRealityUI != null)
                    {
                        mixedRealityUI.showShieldMessage(strength);
                        mixedRealityUI.showCardEffect(
                            "Shield", strength
                        );
                    }
                }
                else if (mixedRealityUI != null)
                {
                    mixedRealityUI.showAbilityMessage(
                        $"Opponent gained {strength} shield."
                    );
                }

                break;
            }
        }

        gameState.UpdateState();
        UpdateVisuals();
    }

    private int DealDamage(
        Player target,
        int damage,
        out int absorbed)
    {
        damage = Mathf.Max(0, damage);
        absorbed = Mathf.Min(target.shield, damage);

        // Player.takeDamage consumes shield before health.
        return target.takeDamage(damage);
    }

    private void DrawReplacement(Player player)
    {
        if (player == null || gameState == null)
            return;

        int playerIndex = gameState.players.IndexOf(player);

        if (playerIndex < 0 || playerIndex > 1)
            return;

        // When the shared undealt deck is empty, recycle only
        // this player's own discarded cards.
        if (gameState.drawPile.Count == 0)
        {
            List<Card> ownDiscard = playerIndex == 0
                ? gameState.playerDiscardPile
                : gameState.aiDiscardPile;

            if (ownDiscard.Count > 0)
            {
                foreach (Card discardedCard in ownDiscard)
                    gameState.discardPile.Remove(discardedCard);

                gameState.drawPile.AddRange(ownDiscard);
                ownDiscard.Clear();

                ShuffleDeck(gameState.drawPile);
            }
        }

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

    private void DiscardCard(Card card, Player owner)
    {
        if (card == null || owner == null || gameState == null)
            return;

        // Keep the overall discard list for tracking.
        gameState.discardPile.Add(card);

        // Track ownership so discarded cards return to the
        // player who originally played them.
        if (gameState.players.IndexOf(owner) == 0)
            gameState.playerDiscardPile.Add(card);
        else if (gameState.players.IndexOf(owner) == 1)
            gameState.aiDiscardPile.Add(card);
    }

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

    public void EndTurn()
    {
        if (gameState == null || gameState.gameOver)
            return;

        gameState.currentTurn = 1 - gameState.currentTurn;

        // Shield expires when its owner begins their next turn.
        Player currentPlayer =
            gameState.players[gameState.currentTurn];

        currentPlayer.clearShield();

        gameState.UpdateState();
        UpdateVisuals();

        if (mixedRealityUI != null)
        {
            if (gameState.currentTurn == 0)
                mixedRealityUI.showPlayerTurnMessage();
            else
                mixedRealityUI.showOpponentTurnMessage();
        }
    }

    private void EndGame(string message, bool playerWon)
    {
        gameState.gameOver = true;

        Debug.Log(message);

        if (mixedRealityUI != null)
            mixedRealityUI.showGameOver(playerWon);

        UpdateVisuals();
    }

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
}
