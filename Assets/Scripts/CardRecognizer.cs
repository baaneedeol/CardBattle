
using UnityEngine;

public class CardRecognizer : MonoBehaviour
{
    [Header("Game References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Mixed_Reality_UI mixedRealityUI;

    [Header("Recognition Settings")]
    [SerializeField] private bool logRecognitions = true;

    private bool cardCurrentlyDetected;
    private Suit lastDetectedSuit;
    private int lastDetectedRank;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (mixedRealityUI == null)
            mixedRealityUI = FindFirstObjectByType<Mixed_Reality_UI>();

        if (gameManager == null)
            Debug.LogError("CardRecognizer: No GameManager found.");
    }

    // Entry point for a vision system that recognizes suit and rank.
    public void OnCardDetected(Suit suit, int rank)
    {
        if (cardCurrentlyDetected &&
            lastDetectedSuit == suit &&
            lastDetectedRank == rank)
        {
            return;
        }

        cardCurrentlyDetected = true;
        lastDetectedSuit = suit;
        lastDetectedRank = rank;

        SubmitRecognizedCard(suit, rank);
    }

    // Convenience entry point for systems returning suit names.
    public void SubmitRecognizedCard(string suitName, int rank)
    {
        if (!System.Enum.TryParse(
                suitName,
                true,
                out Suit suit) ||
            !System.Enum.IsDefined(typeof(Suit), suit))
        {
            Debug.LogWarning(
                $"CardRecognizer: Unknown suit '{suitName}'."
            );
            return;
        }

        OnCardDetected(suit, rank);
    }

    // Validates a recognized card and sends it to GameManager.
    public void SubmitRecognizedCard(Suit suit, int rank)
    {
        if (gameManager == null)
        {
            Debug.LogError(
                "CardRecognizer: GameManager is not assigned."
            );
            return;
        }

        if (rank != 2 && rank != 4 &&
            rank != 6 && rank != 8)
        {
            Debug.LogWarning(
                $"CardRecognizer: Unsupported rank {rank}."
            );
            return;
        }

        if (!System.Enum.IsDefined(typeof(Suit), suit))
        {
            Debug.LogWarning(
                "CardRecognizer: Unsupported suit."
            );
            return;
        }

        if (logRecognitions)
            Debug.Log($"Recognized card: {suit} {rank}");

        bool accepted =
            gameManager.HandleRecognizedCard(suit, rank);

        if (!accepted && logRecognitions)
        {
            Debug.Log(
                "Card was recognized but not played. " +
                "Check the player's hand and current turn."
            );
        }
    }

    // Call this when the physical card leaves the recognition area.
    public void OnCardLost()
    {
        cardCurrentlyDetected = false;

        if (mixedRealityUI != null)
            mixedRealityUI.setCardPresent(false);
    }

    public void ResetDetection()
    {
        OnCardLost();
    }

    // Temporary methods for testing through the Inspector.
    public void TestSpades2()
    {
        OnCardDetected(Suit.Spades, 2);
    }

    public void TestHearts4()
    {
        OnCardDetected(Suit.Hearts, 4);
    }

    public void TestDiamonds6()
    {
        OnCardDetected(Suit.Diamonds, 6);
    }

    public void TestClubs8()
    {
        OnCardDetected(Suit.Clubs, 8);
    }
}
