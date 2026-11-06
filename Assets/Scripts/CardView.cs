using UnityEngine;

public class CardView : MonoBehaviour
{
    [SerializeField] private Renderer faceRenderer;   // the Face child's renderer

    private Card card;

    private void Awake()
    {
        if (faceRenderer == null)
            faceRenderer = GetComponentInChildren<Renderer>();
    }

    public void Setup(Card card, bool faceUp = true)
    {
        this.card = card;
        if (faceUp) ShowFace(); else ShowBack();
    }

    public void ShowFace()
    {
        string suitLetter = card.suit switch
        {
            Suit.Spades => "S",
            Suit.Clubs => "C",
            Suit.Diamonds => "D",
            Suit.Hearts => "H",
            _ => "S"
        };

        string path = $"CardFaces/{suitLetter}_{card.rank}";
        Material m = Resources.Load<Material>(path);

        if (m == null)
        {
            Debug.LogWarning($"CardView: missing material at Resources/{path}");
            return;
        }

        faceRenderer.sharedMaterial = m;
    }

    public void ShowBack()
    {
        if (faceRenderer == null) return;

        Material m = Resources.Load<Material>("CardFaces/CardBack");
        if (m == null)
        {
            Debug.LogWarning("CardView: missing material at Resources/CardFaces/CardBack");
            return;
        }
        faceRenderer.sharedMaterial = m;
    }
}