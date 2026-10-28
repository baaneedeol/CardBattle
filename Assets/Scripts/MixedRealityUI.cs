using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Mixed_Reality_UI : MonoBehaviour
{
    [Header("Game Reference")]
    [SerializeField] private GameManager gameManager;

    [Header("Player Health UI")]
    [SerializeField] private Image playerHealthFill;
    [SerializeField] private TMP_Text playerHealthText;
    [SerializeField] private TMP_Text playerShieldText;
    [SerializeField] private RectTransform playerHealthFillRect;

    [Header("Opponent Health UI")]
    [SerializeField] private Image opponentHealthFill;
    [SerializeField] private TMP_Text opponentHealthText;
    [SerializeField] private TMP_Text opponentShieldText;
    [SerializeField] private RectTransform opponentHealthFillRect;

    [Header("Turn UI")]
    [SerializeField] private TMP_Text turnText;

    [Header("Card Effect UI")]
    [SerializeField] private GameObject cardEffectPanel;
    [SerializeField] private TMP_Text cardEffectText;

    [Header("Game Message UI")]
    [SerializeField] private GameObject gameMessagePanel;
    [SerializeField] private TMP_Text gameMessageText;
    [SerializeField] private float defaultMessageDuration = 3f;

    [Header("Game Message Auto Resize")]
    [SerializeField] private float messageHorizontalPadding = 40f;
    [SerializeField] private float messageVerticalPadding = 24f;
    [SerializeField] private float messageMinWidth = 200f;
    [SerializeField] private float messageMaxWidth = 850f;
    [SerializeField] private float messageMinHeight = 80f;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverText;

    [Header("Game Settings")]
    [SerializeField] private int maxHealth = 20;

    private bool cardIsPresent = false;
    private Coroutine messageCoroutine;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<GameManager>();
        }
    }

    private void Start()
    {
        cardIsPresent = false;

        if (cardEffectPanel != null)
            cardEffectPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        keepMessagePanelVisible();

        updateUI(
            maxHealth, 0,
            maxHealth, 0,
            true, false
        );

        showWelcomeMessage();
    }

    // Resets UI for a newly initialized match.
    // Called by GameManager.StartGame().
    public void PrepareForNewGame()
    {
        cardIsPresent = false;

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameOverText != null)
            gameOverText.text = "";

        hideCardEffect();

        if (cardEffectText != null)
            cardEffectText.gameObject.SetActive(true);

        updateUI(
            maxHealth, 0,
            maxHealth, 0,
            true, false
        );

        showWelcomeMessage();
        keepMessagePanelVisible();
    }

    public void updateUI(
        int playerHealth,
        int playerShield,
        int opponentHealth,
        int opponentShield,
        bool playerTurn,
        bool gameOver)
    {
        updateHealth(true, playerHealth, playerShield);
        updateHealth(false, opponentHealth, opponentShield);

        if (gameOver)
        {
            if (turnText != null)
                turnText.text = "GAME OVER";

            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
        }
        else
        {
            showTurnIndicator(playerTurn);

            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
        }

        keepMessagePanelVisible();
    }

    public void updateHealth(
        bool isPlayer,
        int health,
        int shield)
    {
        health = Mathf.Clamp(health, 0, maxHealth);
        shield = Mathf.Max(0, shield);

        TMP_Text healthText = isPlayer
            ? playerHealthText
            : opponentHealthText;

        TMP_Text shieldText = isPlayer
            ? playerShieldText
            : opponentShieldText;

        Image healthFill = isPlayer
            ? playerHealthFill
            : opponentHealthFill;

        float ratio = maxHealth > 0
            ? (float)health / maxHealth
            : 0f;

        RectTransform fillRect = isPlayer
            ? playerHealthFillRect
            : opponentHealthFillRect;

        // Preserve the existing anchor-based health bar setup.
        if (fillRect != null)
        {
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(ratio, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }
        else if (healthFill != null)
        {
            // Alternative for an Image configured as Filled.
            healthFill.fillAmount = ratio;
        }

        if (healthText != null)
            healthText.text = $"{health} / {maxHealth} HP";

        if (shieldText != null)
            shieldText.text = $"Shield: {shield}";
    }

    private void keepMessagePanelVisible()
    {
        if (gameMessagePanel != null &&
            !gameMessagePanel.activeSelf)
        {
            gameMessagePanel.SetActive(true);
        }

        if (gameMessageText != null &&
            !gameMessageText.gameObject.activeSelf)
        {
            gameMessageText.gameObject.SetActive(true);
        }
    }

    public void showMessage(
        string message,
        float duration = 0f)
    {
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }

        keepMessagePanelVisible();
        ResizeGameMessagePanel(message);

        if (duration < 0f)
            duration = defaultMessageDuration;

        if (duration > 0f)
        {
            messageCoroutine =
                StartCoroutine(
                    clearMessageAfterDelay(duration)
                );
        }
    }

    private void ResizeGameMessagePanel(string message)
    {
        if (gameMessagePanel == null ||
            gameMessageText == null)
        {
            return;
        }

        RectTransform panelRect =
            gameMessagePanel.GetComponent<RectTransform>();

        RectTransform textRect =
            gameMessageText.GetComponent<RectTransform>();

        if (panelRect == null || textRect == null)
            return;

        gameMessageText.text = message;

        // Measure the unwrapped message first.
        gameMessageText.textWrappingMode =
            TextWrappingModes.NoWrap;

        gameMessageText.ForceMeshUpdate();

        float naturalWidth =
            gameMessageText.preferredWidth;

        float panelWidth = Mathf.Clamp(
            naturalWidth + messageHorizontalPadding,
            messageMinWidth,
            messageMaxWidth
        );

        float textWidth = Mathf.Max(
            1f,
            panelWidth - messageHorizontalPadding
        );

        textRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            textWidth
        );

        // Enable wrapping for longer messages.
        gameMessageText.textWrappingMode =
            TextWrappingModes.Normal;

        gameMessageText.ForceMeshUpdate();

        float textHeight =
            gameMessageText.preferredHeight;

        float panelHeight = Mathf.Max(
            messageMinHeight,
            textHeight + messageVerticalPadding
        );

        panelRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            panelWidth
        );

        panelRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            panelHeight
        );
    }

    private IEnumerator clearMessageAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (gameMessageText != null)
            gameMessageText.text = "";

        messageCoroutine = null;
    }

    public void clearMessage()
    {
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }

        if (gameMessageText != null)
            gameMessageText.text = "";
    }

    // Welcome and recognition messages.

    public void showWelcomeMessage()
    {
        showMessage("Welcome! Place a card to begin!", 0f);
    }

    public void showWaitingForCardMessage()
    {
        showMessage("Waiting for a card...", 0f);
    }

    public void showCardDetectedMessage()
    {
        showMessage("Card detected! Preparing ability!", -1f);
    }

    public void showCardRemovedMessage()
    {
        showMessage("Card removed. Place another card!", 0f);
    }

    public void showInvalidCardMessage()
    {
        showMessage("Card not in your hand. Try another card!", -1f);
    }

    // Battle messages.

    public void showAttackMessage(int damage)
    {
        if (damage > 0)
        {
            showMessage(
                $"Attack successful! {damage} HP damage dealt!",
                -1f
            );
        }
        else
        {
            showMessage(
                "Attack blocked! No HP damage dealt.",
                -1f
            );
        }
    }

    public void showHealMessage(int amount)
    {
        showMessage(
            $"Healing activated! +{amount} HP!",
            -1f
        );
    }

    public void showShieldMessage(int amount)
    {
        showMessage(
            $"Shield activated! +{amount} shield!",
            -1f
        );
    }

    // Two-argument version for accurate drain feedback.
    public void showDrainMessage(int damage, int healed)
    {
        showMessage(
            $"Drain: {damage} HP damage dealt, {healed} HP restored.",
            -1f
        );
    }

    // Preserves compatibility with earlier callers.
    public void showDrainMessage(int amount)
    {
        showDrainMessage(amount, amount);
    }

    public void showBlockedAttackMessage()
    {
        showMessage("Attack blocked by shield!", -1f);
    }

    public void showAbilityMessage(string message)
    {
        showMessage(message, -1f);
    }

    public void showPlayerTurnMessage()
    {
        showMessage("Your turn! Place a card to play.", 0f);
    }

    public void showOpponentTurnMessage()
    {
        showMessage("Opponent's turn!", 0f);
    }

    // MR card-effect panel.

    public void showCardEffect(string ability, int amount)
    {
        showCardEffect(ability, amount, amount);
    }

    public void showCardEffect(
        string ability,
        int amount,
        int secondaryAmount)
    {
        if (!cardIsPresent)
        {
            hideCardEffect();
            return;
        }

        if (cardEffectPanel != null)
            cardEffectPanel.SetActive(true);

        if (cardEffectText == null)
            return;

        switch (ability.ToLowerInvariant())
        {
            case "attack":
                cardEffectText.text =
                    $"-{amount} DAMAGE!";

                cardEffectText.color =
                    new Color(1f, 0.25f, 0.25f);
                break;

            case "heal":
                cardEffectText.text =
                    $"+{amount} HP!";

                cardEffectText.color =
                    new Color(0.25f, 1f, 0.45f);
                break;

            case "drain":
                cardEffectText.text =
                    $"DRAIN: -{amount} HP / " +
                    $"+{secondaryAmount} HP";

                cardEffectText.color =
                    new Color(0.25f, 0.85f, 1f);
                break;

            case "shield":
                cardEffectText.text =
                    $"+{amount} SHIELD!";

                cardEffectText.color =
                    new Color(0.75f, 0.45f, 1f);
                break;

            default:
                cardEffectText.text = ability;
                cardEffectText.color = Color.white;
                break;
        }
    }

    public void onCardRecognized(string ability, int amount)
    {
        cardIsPresent = true;
        showCardEffect(ability, amount);
        showCardDetectedMessage();
    }

    public void setCardPresent(bool present)
    {
        bool wasPresent = cardIsPresent;
        cardIsPresent = present;

        if (!present)
        {
            hideCardEffect();

            if (wasPresent)
                showCardRemovedMessage();
        }
        else if (!wasPresent)
        {
            showCardDetectedMessage();
        }
    }

    public void showTurnIndicator(bool playerTurn)
    {
        if (turnText == null)
            return;

        turnText.text = playerTurn
            ? "PLAYER'S TURN"
            : "OPPONENT'S TURN";

        turnText.color = playerTurn
            ? new Color(0.25f, 1f, 0.85f)
            : new Color(1f, 0.65f, 0.3f);
    }

    public void showGameOver(bool playerWon)
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (gameOverText != null)
        {
            gameOverText.text = playerWon
                ? "VICTORY!"
                : "DEFEAT!";

            gameOverText.color = playerWon
                ? new Color(0.25f, 1f, 0.65f)
                : new Color(1f, 0.25f, 0.25f);
        }

        if (turnText != null)
            turnText.text = "GAME OVER";

        showMessage(
            playerWon
                ? "Victory! You won the battle!"
                : "Defeat! Better luck next time!",
            0f
        );

        keepMessagePanelVisible();
    }

    public void hideCardEffect()
    {
        if (cardEffectPanel != null)
        {
            cardEffectPanel.SetActive(false);
        }
        else if (cardEffectText != null)
        {
            cardEffectText.gameObject.SetActive(false);
        }
    }

    // Keep this method so existing Start Button OnClick
    // assignments do not have to change.
    public void startGame()
    {
        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null)
        {
            gameManager.StartGame();
        }
        else
        {
            Debug.LogError(
                "Mixed_Reality_UI: GameManager is not assigned."
            );
        }
    }

    private void LateUpdate()
    {
        keepMessagePanelVisible();
    }
}