
using UnityEngine;

public class GameFlow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TablePlacer placer;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameState gameState;
    [SerializeField] private CardEffectPlayer effects;
    [SerializeField] private Mixed_Reality_UI ui;
    [SerializeField] private GameObject startPanel;

    public GameBoard board;

    public event System.Action MatchReady;

    public bool IsMatchReady { get; private set; }

    private void OnEnable()
    {
        if (placer != null)
        {
            placer.Placed += OnPlaced;
            placer.Cleared += OnCleared;
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

        gameManager.StartGame();

        board.DealHands(
            gameState.players[0],
            gameState.players[1]
        );

        IsMatchReady = true;

        MatchReady?.Invoke();
    }

    private void OnCleared()
    {
        IsMatchReady = false;

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

        IsMatchReady = false;

        gameManager.StartGame();

        board.DealHands(
            gameState.players[0],
            gameState.players[1]
        );

        IsMatchReady = true;

        MatchReady?.Invoke();
    }

    public void ReturnToStartMenu()
    {
        IsMatchReady = false;

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
