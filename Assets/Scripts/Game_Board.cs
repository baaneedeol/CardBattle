using System.Collections.Generic;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
    [Header("Slots (0-7 player, 8-15 AI)")]
    [SerializeField] private Transform[] slots;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private float surfaceOffset = 0.003f;

    [Header("Discard + effects")]
    [SerializeField] private Transform playerDiscard;
    [SerializeField] private Transform aiDiscard;
    [SerializeField] private float discardStackStep = 0.002f;
    [SerializeField] private Transform playerEffectPoint;
    [SerializeField] private Transform opponentEffectPoint;

    [Header("Slot layout")]
    [SerializeField] private int columns = 8;
    [SerializeField] private float columnSpacing = 0.075f;
    [SerializeField] private float rowSpacing = 0.25f;
    [SerializeField] private float surfaceHeight = 0.02f;

    private const int AISlotStart = 8;

    private readonly Dictionary<int, GameObject> cards = new Dictionary<int, GameObject>();
    private readonly Dictionary<Card, int> slotOfCard = new Dictionary<Card, int>();

    public Transform PlayerEffectPoint => playerEffectPoint;
    public Transform OpponentEffectPoint => opponentEffectPoint;
    public int SlotCount => slots != null ? slots.Length : 0;

    // ---------- Dealing ----------

    public void DealHands(Player player, Player ai)
    {
        Clear();
        ArrangeSlots();

        for (int i = 0; i < player.hand.Count; i++)
            Place(player.hand[i], i, true);

        for (int i = 0; i < ai.hand.Count; i++)
            Place(ai.hand[i], AISlotStart + i, false);
    }

    private void Place(Card data, int slotIndex, bool faceUp)
    {
        GameObject go = PlaceCard(cardPrefab, slotIndex, data, faceUp);
        if (go != null) slotOfCard[data] = slotIndex;
    }

    /// <summary>Layout test: same card in every slot.</summary>
    public void DealAll()
    {
        ArrangeSlots();
        for (int i = 0; i < SlotCount; i++)
            PlaceCard(cardPrefab, i);
    }

    public GameObject PlaceCard(GameObject prefab, int slotIndex,
                                Card cardData = null, bool faceUp = true)
    {
        if (prefab == null || slotIndex < 0 ||
            slotIndex >= SlotCount || slots[slotIndex] == null)
        {
            Debug.LogWarning($"PlaceCard: invalid prefab or slot index {slotIndex}.", this);
            return null;
        }

        RemoveCard(slotIndex);

        Transform slot = slots[slotIndex];
        GameObject cardObj = Instantiate(prefab, slot);
        cardObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        Renderer[] renderers = cardObj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float lift = slot.position.y - bounds.min.y;
            cardObj.transform.position += slot.up * (lift + surfaceOffset);
        }
        else
        {
            Debug.LogWarning("Card prefab has no renderers.", cardObj);
        }

        if (cardData != null && cardObj.TryGetComponent(out CardView view))
            view.Setup(cardData, faceUp);

        cards[slotIndex] = cardObj;
        return cardObj;
    }

    // ---------- Playing a card ----------

    /// <summary>Moves a dealt card to its discard spot and reveals it.</summary>
    public void PlayCard(Card cardData, bool byPlayer)
    {
        if (!slotOfCard.TryGetValue(cardData, out int slotIndex)) return;
        if (!cards.TryGetValue(slotIndex, out GameObject go) || go == null) return;

        cards.Remove(slotIndex);
        slotOfCard.Remove(cardData);

        Transform discard = byPlayer ? playerDiscard : aiDiscard;
        if (discard == null) { Destroy(go); return; }

        go.transform.SetParent(discard, false);
        go.transform.localPosition =
            Vector3.up * (surfaceOffset + discardStackStep * discard.childCount);
        go.transform.localRotation = Quaternion.identity;

        if (go.TryGetComponent(out CardView view))
            view.ShowFace();
            Debug.Log($"[Board] PlayCard {cardData} byPlayer={byPlayer} -> {discard.name} " +
          $"euler={discard.eulerAngles} scale={discard.lossyScale} " +
          $"cardUpDotWorldUp={Vector3.Dot(go.transform.up, Vector3.up):F2}", go);
    }

    // ---------- Cleanup ----------

    public void RemoveCard(int slotIndex)
    {
        if (cards.TryGetValue(slotIndex, out GameObject existing))
        {
            if (existing != null) Destroy(existing);
            cards.Remove(slotIndex);
        }
    }

    public void Clear()
    {
        foreach (GameObject go in cards.Values)
            if (go != null) Destroy(go);
        cards.Clear();
        slotOfCard.Clear();

        ClearDiscard(playerDiscard);
        ClearDiscard(aiDiscard);
    }

    private static void ClearDiscard(Transform discard)
    {
        if (discard == null) return;
        for (int i = discard.childCount - 1; i >= 0; i--)
            Destroy(discard.GetChild(i).gameObject);
    }

    // ---------- Layout ----------

    [ContextMenu("Arrange Slots")]
    private void ArrangeSlots()
    {
        if (slots == null || slots.Length == 0) return;

        int rows = Mathf.CeilToInt(slots.Length / (float)columns);
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            int row = i / columns, col = i % columns;
            float x = (col - (columns - 1) * 0.5f) * columnSpacing;
            float z = (row - (rows - 1) * 0.5f) * rowSpacing;
            slots[i].localPosition = new Vector3(x, surfaceHeight, z);
            slots[i].localRotation = Quaternion.identity;
        }
    }
}