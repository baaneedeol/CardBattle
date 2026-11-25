
using System.Collections.Generic;
using UnityEngine;

public class GameFlow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TablePlacer placer;
    [SerializeField] private CardHandScanner scanner;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameState gameState;
    [SerializeField] private CardEffectPlayer effects;
    [SerializeField] private Mixed_Reality_UI ui;
    [SerializeField] private GameObject startPanel;

    public GameBoard board;

    // Card instances created from recognized cards, in scan (= slot) order.
    private readonly List<Card> scannedCards = new List<Card>();

    public event System.Action MatchReady;

    public bool IsMatchReady { get; private set; }

    private void OnEnable()
    {
        if (placer != null)
        {
            placer.Placed += OnPlaced;
            placer.Cleared += OnCleared;
        }

        if (scanner != null)
        {
            scanner.ScanProgress += OnScanProgress;
            scanner.CardLocked += OnCardLocked;
            scanner.HandScanned += OnHandScanned;
        }

        if (gameManager != null)
        {
            gameManager.CardPlayed += OnCardPlayed;
            gameManager.GameEnded += OnGameEnded;
        }
    }

    private void OnDisable()
    {
        if (placer != null)
        {
            placer.Placed -= OnPlaced;
            placer.Cleared -= OnCleared;
        }

        if (scanner != null)
        {
            scanner.ScanProgress -= OnScanProgress;
            scanner.CardLocked -= OnCardLocked;
            scanner.HandScanned -= OnHandScanned;
            scanner.CancelScan();
        }

        if (gameManager != null)
        {
            gameManager.CardPlayed -= OnCardPlayed;
            gameManager.GameEnded -= OnGameEnded;
        }
    }


    public void BeginPlacement()
    {
        Debug.Log("In Placement");

        if (startPanel != null)
            startPanel.SetActive(false);

        ui?.showMessage(
            "Aim at a table, rotate with the stick, pull the trigger.",
            0f
        );

        placer.Begin();
    }

    private void OnPlaced(GameObject boardObj)
    {
        board = boardObj.GetComponent<GameBoard>();

        if (board == null)
        {
            Debug.LogError(
                "Board prefab has no GameBoard component."
            );
            return;
        }

        if (effects != null)
        {
            effects.SetEffectPoints(
                board.PlayerEffectPoint,
                board.OpponentEffectPoint
            );
        }

        // Table is placed: scan the player's physical hand before dealing.
        BeginHandScan();
    }

    // CARD SCAN STEP
    private void BeginHandScan()
    {
        IsMatchReady = false;

        if (scanner == null)
        {
            Debug.LogWarning(
                "No CardHandScanner assigned; dealing a random hand."
            );
            StartMatch(null);
            return;
        }

        int handSize = gameManager.startingHandSize;

        // Fresh board and empty scan list; recognized cards appear as they lock in.
        scannedCards.Clear();
        board.PrepareForScan();

        ui?.showMessage(
            $"Hold up your {handSize} cards so they can be scanned.",
            0f
        );

        scanner.BeginScan(handSize);
    }

    private void OnScanProgress(int found, int required)
    {
        if (found == 0 || found >= required)
            return;

        ui?.showMessage($"Scanned {found} / {required} cards...", 0f);
    }

    // Each recognized card is placed on the board immediately.
    private void OnCardLocked(Suit suit, int rank, int index)
    {
        if (board == null)
            return;

        Card card = new Card(suit, rank, Card.GetAbilityForSuit(suit));

        if (board.PlaceRecognizedCard(card, index))
            scannedCards.Add(card);
    }

    private void OnHandScanned(List<(Suit suit, int rank)> cards)
    {
        // Ignore a late result if the table was cleared mid-scan.
        if (board == null)
            return;

        StartMatch(scannedCards);
    }

    private void StartMatch(IList<Card> scannedHand)
    {
        gameManager.StartGame(scannedHand);

        if (scannedHand != null)
        {
            // Player cards are already on the board; add the AI hand only.
            board.DealAIHand(gameState.players[1]);
        }
        else
        {
            board.DealHands(
                gameState.players[0],
                gameState.players[1]
            );
        }

        IsMatchReady = true;

        MatchReady?.Invoke();
    }

    private void OnCleared()
    {
        IsMatchReady = false;

        if (scanner != null)
            scanner.CancelScan();

        scannedCards.Clear();

        gameManager.EndMatch();

        board = null;

        ui?.showMessage(
            "Aim at a table and pull the trigger.",
            0f
        );
    }

    private void OnGameEnded()
    {
        IsMatchReady = false;

        // Keep the board available until the user
        // chooses Restart or Exit.
    }

    public void RestartMatch()
    {
        if (board == null)
        {
            Debug.LogWarning(
                "Cannot restart: no board is currently placed."
            );
            return;
        }

        if (gameManager == null || gameManager.IsProcessingTurn)
        {
            Debug.LogWarning(
                "Cannot restart while a turn is processing."
            );
            return;
        }

        // Rescan so the new virtual hand matches the physical cards held.
        BeginHandScan();
    }

    public void ReturnToStartMenu()
    {
        IsMatchReady = false;

        if (scanner != null)
            scanner.CancelScan();

        // Stop the current match without deleting the placed board.
        if (gameManager != null)
            gameManager.EndMatch();

        // Reset the UI.
        if (ui != null)
            ui.PrepareForStartMenu();

        // Return to the start panel.
        if (startPanel != null)
            startPanel.SetActive(true);
    }

    private void OnCardPlayed(Card card, bool byPlayer)
    {
        if (board != null)
            board.PlayCard(card, byPlayer);
    }
}
