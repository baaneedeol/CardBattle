
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

    [Header("Turn Indicator Panel")]
    [SerializeField] private TMP_Text turnText;

    [Header("Card Effect Visuals")]
    [SerializeField] private CardEffectPlayer cardEffectPlayer;

    [Header("Game Message UI")]
    [SerializeField] private GameObject gameMessagePanel;
    [SerializeField] private TMP_Text gameMessageText;
    [SerializeField] private float defaultMessageDuration = 3f;

    [Header("Game Message Auto Resize")]
    [SerializeField] private float messageHorizontalPadding = 50f;
    [SerializeField] private float messageVerticalPadding = 30f;
    [SerializeField] private float messageMinWidth = 300f;
    [SerializeField] private float messageMaxWidth = 850f;
    [SerializeField] private float messageMinHeight = 80f;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverText;

    [Header("Game Settings")]
    [SerializeField] private int maxHealth = 20;

    private bool cardIsPresent = false;
    private Coroutine messageCoroutine;

    public bool IsMessageDisplaying =>
        messageCoroutine != null;

    public float DefaultMessageDuration =>
        defaultMessageDuration;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (cardEffectPlayer == null)
            cardEffectPlayer = FindFirstObjectByType<CardEffectPlayer>();
    }

    private void Start()
    {
        cardIsPresent = false;

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

    // ----------------------------------
    // NEW GAME RESET
    // ----------------------------------

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

        if (turnText != null)
            showTurnIndicator(true);

        if (cardEffectPlayer != null)
        {
            cardEffectPlayer.ClearEffect();
            cardEffectPlayer.ClearAllShields();
        }

        updateUI(
            maxHealth, 0,
            maxHealth, 0,
            true, false
        );

        clearMessage();
        showWelcomeMessage();
        keepMessagePanelVisible();

        Debug.Log("New game UI reset.");
    }

    // ----------------------------------
    // HEALTH AND TURN UI
    // ----------------------------------

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

        RectTransform fillRect = isPlayer
            ? playerHealthFillRect
            : opponentHealthFillRect;

        float ratio = maxHealth > 0
            ? (float)health / maxHealth
            : 0f;

        if (fillRect != null)
        {
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(ratio, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }
        else if (healthFill != null)
        {
            healthFill.fillAmount = ratio;
        }

        if (healthText != null)
            healthText.text = $"{health} / {maxHealth} HP";

        if (shieldText != null)
            shieldText.text = $"Shield: {shield}";
    }

    // ----------------------------------
    // GAME MESSAGE PANEL
    // ----------------------------------

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
            messageCoroutine = StartCoroutine(
                clearMessageAfterDelay(duration)
            );
        }
    }


    private void ResizeGameMessagePanel(string message)
    {
        if (gameMessagePanel == null || gameMessageText == null)
            return;

        RectTransform panelRect =
            gameMessagePanel.GetComponent<RectTransform>();

        RectTransform textRect =
            gameMessageText.GetComponent<RectTransform>();

        if (panelRect == null || textRect == null)
            return;

        message = FormatGameMessage(message);
        gameMessageText.text = message;
        gameMessageText.enableAutoSizing = false;
        gameMessageText.fontSize = 30f;
        gameMessageText.alignment = TextAlignmentOptions.Center;
        gameMessageText.textWrappingMode = TextWrappingModes.Normal;
        gameMessageText.overflowMode = TextOverflowModes.Overflow;

        Canvas.ForceUpdateCanvases();

        // Measure the message without wrapping.
        Vector2 naturalSize = gameMessageText.GetPreferredValues(
            message,
            Mathf.Infinity,
            Mathf.Infinity
        );

        // Short messages fit naturally.
        // Long messages stop at the maximum panel width.
        float panelWidth = Mathf.Clamp(
            naturalSize.x + messageHorizontalPadding,
            messageMinWidth,
            messageMaxWidth
        );

        panelRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            panelWidth
        );

        // Use the full available panel width.
        // Long messages will wrap when they reach the panel limit.
        float availableWidth =
            panelWidth - messageHorizontalPadding;

        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);

        textRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            availableWidth
        );

        Vector2 wrappedSize = gameMessageText.GetPreferredValues(
            message,
            availableWidth,
            Mathf.Infinity
        );

        float panelHeight = Mathf.Max(
            messageMinHeight,
            wrappedSize.y + messageVerticalPadding
        );

        panelRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            panelHeight
        );

        textRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            wrappedSize.y
        );

        Canvas.ForceUpdateCanvases();
        gameMessageText.ForceMeshUpdate();

        Debug.Log(
            $"Message: {message} | " +
            $"Panel: {panelWidth} x {panelHeight} | " +
            $"Natural text width: {naturalSize.x}"
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

    // ----------------------------------
    // WELCOME AND CARD RECOGNITION
    // ----------------------------------

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
        showMessage(
            "Card not in your hand. Try another card!",
            -1f
        );
    }

    // ----------------------------------
    // BATTLE MESSAGES
    // ----------------------------------

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

    public void showDrainMessage(int damage, int healed)
    {
        showMessage(
            $"Drain: {damage} HP damage dealt, {healed} HP restored.",
            -1f
        );
    }

    public void showDrainMessage(int amount)
    {
        showDrainMessage(amount, amount);
    }

    public void showBlockedAttackMessage()
    {
        showMessage("Attack blocked by shield!", -1f);
    }

    // New overload allows GameManager to control message duration.
    public void showAbilityMessage(
        string message,
        float duration)
    {
        showMessage(message, duration);
    }

    // Compatibility with existing one-argument callers.
    public void showAbilityMessage(string message)
    {
        showMessage(message, -1f);
    }

    // ----------------------------------
    // CARD EFFECT TEXT COMPATIBILITY
    // ----------------------------------

    // Kept for existing scripts and tests.
    // GameManager no longer calls this after showing an ability message.
    public void showCardEffect(string ability, int amount)
    {
        showCardEffect(ability, amount, amount);
    }

    public void showCardEffect(
        string ability,
        int amount,
        int secondaryAmount)
    {
        if (string.IsNullOrEmpty(ability))
            return;

        switch (ability.ToLowerInvariant())
        {
            case "attack":
                showMessage($"-{amount} DAMAGE!", -1f);
                break;

            case "heal":
                showMessage($"+{amount} HP!", -1f);
                break;

            case "drain":
                showMessage(
                    $"DRAIN: -{amount} HP / +{secondaryAmount} HP",
                    -1f
                );
                break;

            case "shield":
                showMessage($"+{amount} SHIELD!", -1f);
                break;

            default:
                showMessage(ability, -1f);
                break;
        }
    }

    // ----------------------------------
    // CARD RECOGNITION STATE
    // ----------------------------------

    public void onCardRecognized(string ability, int amount)
    {
        cardIsPresent = true;
        showCardDetectedMessage();
    }

    public void setCardPresent(bool present)
    {
        bool wasPresent = cardIsPresent;
        cardIsPresent = present;

        if (!present)
        {
            if (cardEffectPlayer != null)
                cardEffectPlayer.ClearEffect();

            if (wasPresent)
                showCardRemovedMessage();
        }
        else if (!wasPresent)
        {
            showCardDetectedMessage();
        }
    }

    public void hideCardEffect()
    {
        if (cardEffectPlayer != null)
            cardEffectPlayer.ClearEffect();
    }

    // ----------------------------------
    // TURN INDICATOR
    // ----------------------------------

    public void showTurnIndicator(bool playerTurn)
    {
        if (turnText == null)
            return;

        turnText.text = playerTurn
            ? "Player's Turn"
            : "Opponent's Turn";

        turnText.color = playerTurn
            ? new Color(0.25f, 1f, 0.85f)
            : new Color(1f, 0.65f, 0.3f);
    }

    public void showPlayerTurnMessage()
    {
        showTurnIndicator(true);
    }

    public void showOpponentTurnMessage()
    {
        showTurnIndicator(false);
    }

    // ----------------------------------
    // GAME OVER
    // ----------------------------------

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

    // ----------------------------------
    // START BUTTON COMPATIBILITY
    // ----------------------------------

    public void startGame()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

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

    private string FormatGameMessage(string message)
    {
        if (message.StartsWith("Player drained ") ||
            message.StartsWith("Opponent drained "))
        {
            message = message.Replace(
                " and restored ",
                "\nand restored "
            );
        }

        return message;
    }
}
