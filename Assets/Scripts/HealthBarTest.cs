
using UnityEngine;
using UnityEngine.InputSystem;

public class HealthBarAnchorTest : MonoBehaviour
{
    [Header("Player Health Bar")]
    [SerializeField] private RectTransform playerHealthBarFill;

    [Header("Opponent Health Bar")]
    [SerializeField] private RectTransform opponentHealthBarFill;

    [SerializeField] private float maxHealth = 20f;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        // Player health controls: 1-5
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            SetHealth(playerHealthBarFill, 20, "Player");

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            SetHealth(playerHealthBarFill, 15, "Player");

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            SetHealth(playerHealthBarFill, 10, "Player");

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            SetHealth(playerHealthBarFill, 5, "Player");

        if (Keyboard.current.digit5Key.wasPressedThisFrame)
            SetHealth(playerHealthBarFill, 0, "Player");

        // Opponent health controls: 6-0
        if (Keyboard.current.digit6Key.wasPressedThisFrame)
            SetHealth(opponentHealthBarFill, 20, "Opponent");

        if (Keyboard.current.digit7Key.wasPressedThisFrame)
            SetHealth(opponentHealthBarFill, 15, "Opponent");

        if (Keyboard.current.digit8Key.wasPressedThisFrame)
            SetHealth(opponentHealthBarFill, 10, "Opponent");

        if (Keyboard.current.digit9Key.wasPressedThisFrame)
            SetHealth(opponentHealthBarFill, 5, "Opponent");

        if (Keyboard.current.digit0Key.wasPressedThisFrame)
            SetHealth(opponentHealthBarFill, 0, "Opponent");
    }

    private void SetHealth(
        RectTransform healthBarFill,
        float health,
        string playerName)
    {
        if (healthBarFill == null)
        {
            Debug.LogWarning($"{playerName} health bar is not assigned!");
            return;
        }

        float ratio = maxHealth > 0f
            ? Mathf.Clamp01(health / maxHealth)
            : 0f;

        healthBarFill.anchorMin = new Vector2(0f, 0f);
        healthBarFill.anchorMax = new Vector2(ratio, 1f);
        healthBarFill.offsetMin = Vector2.zero;
        healthBarFill.offsetMax = Vector2.zero;

        Debug.Log($"{playerName} test health: {health}/{maxHealth}, ratio: {ratio}");
    }
}
