using UnityEngine;

public class CardView : MonoBehaviour
{
    [Header("Face renderer")]
    [SerializeField] private Renderer faceRenderer;   // the Face child's renderer
    [Tooltip("Which material slot on the renderer shows the face/back.")]
    [SerializeField] private int materialIndex = 0;

    private Card card;

    private void Awake()
    {
        ResolveRenderer();
    }

    private void ResolveRenderer()
    {
        if (faceRenderer != null) return;

        faceRenderer = GetComponentInChildren<Renderer>();

        if (faceRenderer == null)
            Debug.LogWarning("CardView: no Renderer found. Assign faceRenderer on the card prefab.", this);
    }

    public void Setup(Card card, bool faceUp = true)
    {
        this.card = card;
        if (faceUp) ShowFace(); else ShowBack();
    }

    public void ShowFace()
    {
        if (card == null)
        {
            Debug.LogWarning("CardView.ShowFace called before Setup; no card data.", this);
            return;
        }

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
            Debug.LogWarning($"CardView: missing material at Resources/{path}", this);
            return;
        }

        Apply(m);
    }

    public void ShowBack()
    {
        Material m = Resources.Load<Material>("CardFaces/CardBack");

        if (m == null)
        {
            Debug.LogWarning("CardView: missing material at Resources/CardFaces/CardBack", this);
            return;
        }

        Apply(m);
    }

    private void Apply(Material m)
    {
        ResolveRenderer();
        if (faceRenderer == null) return;

        Material[] mats = faceRenderer.sharedMaterials;

        if (materialIndex < 0 || materialIndex >= mats.Length)
        {
            Debug.LogWarning(
                $"CardView: materialIndex {materialIndex} is out of range " +
                $"({mats.Length} slots on '{faceRenderer.name}').", this);
            return;
        }

        mats[materialIndex] = m;
        faceRenderer.sharedMaterials = mats;
    }
}