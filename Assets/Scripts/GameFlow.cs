using UnityEngine;

public class GameFlow : MonoBehaviour
{
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
        placer.Placed += OnPlaced;
        placer.Cleared += OnCleared;
        gameManager.CardPlayed += OnCardPlayed;
        gameManager.GameEnded += OnGameEnded;
    }

    private void OnDisable()
    {
        placer.Placed -= OnPlaced;
        placer.Cleared -= OnCleared;
        gameManager.CardPlayed -= OnCardPlayed;
        gameManager.GameEnded -= OnGameEnded;
    }

    // Start button OnClick -> this (not Mixed_Reality_UI.startGame)
    public void BeginPlacement()
    {
        Debug.Log("In Placement");
        if (startPanel != null) startPanel.SetActive(false);
        ui?.showMessage("Aim at a table, rotate with the stick, pull the trigger.", 0f);
        placer.Begin();
    }

    private void OnPlaced(GameObject boardObj)
    {
        board = boardObj.GetComponent<GameBoard>();
        if (board == null) { Debug.LogError("Board prefab has no GameBoard component."); return; }

        effects.SetEffectPoints(board.PlayerEffectPoint, board.OpponentEffectPoint);
        gameManager.StartGame();
        board.DealHands(gameState.players[0], gameState.players[1]);

        IsMatchReady = true;
        MatchReady?.Invoke();
    }

    private void OnCleared()
    {
        IsMatchReady = false;
        gameManager.EndMatch();
        board = null;
        ui?.showMessage("Aim at a table and pull the trigger.", 0f);
    }

    private void OnGameEnded()
    {
        IsMatchReady = false;
        if (startPanel != null) startPanel.SetActive(true);
    }

    private void OnCardPlayed(Card card, bool byPlayer)
    {
        if (board != null) board.PlayCard(card, byPlayer);
    }

}