
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Mixed_Reality_UI : MonoBehaviour
{
    [Header("Game References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameFlow gameFlow;

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
    [SerializeField] private float messageHorizontalPadding = 40f;
    [SerializeField] private float messageVerticalPadding = 20f;
    [SerializeField] private float messageMinWidth = 300f;
    [SerializeField] private float messageMaxWidth = 850f;
    [SerializeField] private float messageMinHeight = 80f;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverText;

    [Header("Game Settings")]
    [SerializeField] private int maxHealth = 20;

    [Header("Battle UI Cleanup")]
    [SerializeField] private GameObject battleUIRoot;

    private bool cardIsPresent = false;
    private Coroutine messageCoroutine;

    public bool IsMessageDisplaying => messageCoroutine != null;

    public float DefaultMessageDuration => defaultMessageDuration;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (gameFlow == null)
            gameFlow = FindFirstObjectByType<GameFlow>();

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

    // NEW GAME RESET
    public void PrepareForNewGame()
    {
        cardIsPresent = false;

        clearMessage();

        if (battleUIRoot != null)
            battleUIRoot.SetActive(true);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameOverText != null)
            gameOverText.text = "";

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

        showWelcomeMessage();
        keepMessagePanelVisible();
    }

    // RETURN TO START MENU
    public void PrepareForStartMenu()
    {
        cardIsPresent = false;

        clearMessage();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameOverText != null)
            gameOverText.text = "";

        if (turnText != null)
        {
            turnText.text = "";
            turnText.color = Color.white;
        }

        if (cardEffectPlayer != null)
        {
            cardEffectPlayer.ClearEffect();
            cardEffectPlayer.ClearAllShields();
        }
    }

    // HEALTH AND TURN UI
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
        }
        else
        {
            showTurnIndicator(playerTurn);
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

    // GAME MESSAGE PANEL
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

    // AUTOMATIC MESSAGE PANEL RESIZING
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

        gameMessageText.text = message;
        gameMessageText.enableAutoSizing = false;
        gameMessageText.fontSize = 24f;
        gameMessageText.alignment = TextAlignmentOptions.Center;
        gameMessageText.textWrappingMode = TextWrappingModes.Normal;
        gameMessageText.overflowMode = TextOverflowModes.Overflow;

        Canvas.ForceUpdateCanvases();

        Vector2 naturalSize = gameMessageText.GetPreferredValues(
            message,
            Mathf.Infinity,
            Mathf.Infinity
        );

        float panelWidth = Mathf.Clamp(
            naturalSize.x + messageHorizontalPadding,
            messageMinWidth,
            messageMaxWidth
        );

        panelRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            panelWidth
        );

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

    // WELCOME AND CARD RECOGNITION
    public void showWelcomeMessage()
    {
        showMessage("Place a card to start.", 0f);
    }

    public void showWaitingForCardMessage()
    {
        showMessage("Waiting for card...", 0f);
    }

    public void showCardDetectedMessage()
    {
        showMessage("Card ready!", -1f);
    }

    public void showCardRemovedMessage()
    {
        showMessage("Card removed.", 0f);
    }

    public void showInvalidCardMessage()
    {
        showMessage("Invalid card. Try again.", -1f);
    }

    // BATTLE MESSAGES
    public void showAttackMessage(int damage)
    {
        if (damage > 0)
            showMessage($"-{damage} HP", -1f);
        else
            showMessage("Attack blocked!", -1f);
    }

    public void showHealMessage(int amount)
    {
        showMessage($"+{amount} HP", -1f);
    }

    public void showShieldMessage(int amount)
    {
        showMessage($"+{amount} Shield", -1f);
    }

    public void showDrainMessage(int damage, int healed)
    {
        showMessage(
            $"Drain: -{damage} HP, +{healed} HP",
            -1f
        );
    }

    public void showDrainMessage(int amount)
    {
        showDrainMessage(amount, amount);
    }

    public void showBlockedAttackMessage()
    {
        showMessage("Attack blocked!", -1f);
    }

    public void showAbilityMessage(
        string message,
        float duration)
    {
        showMessage(message, duration);
    }

    public void showAbilityMessage(string message)
    {
        showMessage(message, -1f);
    }

    // CARD EFFECT TEXT COMPATIBILITY
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
    }

    // CARD RECOGNITION STATE
    public void onCardRecognized(string ability, int amount)
    {
        if (!cardIsPresent)
        {
            cardIsPresent = true;
            showCardDetectedMessage();
        }
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

    // TURN INDICATOR
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

    // COMPATIBILITY
    public void showPlayerTurnMessage()
    {
        showTurnIndicator(true);
    }

    public void showOpponentTurnMessage()
    {
        showTurnIndicator(false);
    }

    // GAME OVER
    public void showGameOver(
        string result,
        string finalMessage)
    {
        if (turnText != null)
        {
            turnText.text = "GAME OVER";
            turnText.color = Color.white;
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameOverText != null)
            gameOverText.text = "";

        showMessage(finalMessage, 0f);
    }

    public void showGameOver(bool playerWon)
    {
        showGameOver(
            playerWon ? "VICTORY!" : "DEFEAT!",
            playerWon ? "You won!" : "You lost!"
        );
    }

    public void ShowFinalGameOverPanel(string result)
    {
        clearMessage();

        if (cardEffectPlayer != null)
        {
            cardEffectPlayer.ClearEffect();
            cardEffectPlayer.ClearAllShields();
        }

        if (battleUIRoot != null)
            battleUIRoot.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (gameOverText != null)
        {
            gameOverText.text = "GAME OVER\n\n" + result;

            if (result.Contains("VICTORY"))
            {
                gameOverText.color =
                    new Color(0.25f, 1f, 0.65f);
            }
            else if (result.Contains("DEFEAT"))
            {
                gameOverText.color =
                    new Color(1f, 0.25f, 0.25f);
            }
            else if (result.Contains("DRAW"))
            {
                gameOverText.color =
                    new Color(1f, 0.85f, 0.25f);
            }
            else
            {
                gameOverText.color = Color.white;
            }
        }
    }

    // START BUTTON
    public void startGame()
    {
        if (gameFlow == null)
        {
            Debug.LogError("GameFlow reference is missing.");
            return;
        }

        gameFlow.BeginPlacement();
    }

    private void LateUpdate()
    {
        keepMessagePanelVisible();
    }

    // GAME OVER BUTTONS
    public void RestartGame()
    {
        if (gameFlow == null)
        {
            Debug.LogError("GameFlow reference is missing.");
            return;
        }

        gameFlow.RestartMatch();
    }

    public void ExitGame()
    {
        if (gameFlow == null)
        {
            Debug.LogError("GameFlow reference is missing.");
            return;
        }

        gameFlow.ReturnToStartMenu();
    }
}
