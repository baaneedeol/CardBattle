
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MRVisualTestRunner : MonoBehaviour
{
    [Header("Game References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameState gameState;
    [SerializeField] private Mixed_Reality_UI mixedRealityUI;
    [SerializeField] private CardEffectPlayer cardEffectPlayer;

    [Header("Visual Test Display")]
    [SerializeField] private TMP_Text testStatusText;

    [SerializeField] private float secondsPerTest = 2f;

    private int passed;
    private int failed;

    private bool isRunning;

    private System.Text.StringBuilder report =
        new System.Text.StringBuilder();

    // ==========================================
    // INITIALIZATION
    // ==========================================

    private void Awake()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (gameState == null)
            gameState = FindFirstObjectByType<GameState>();

        if (mixedRealityUI == null)
            mixedRealityUI = FindFirstObjectByType<Mixed_Reality_UI>();

        if (cardEffectPlayer == null)
            cardEffectPlayer = FindFirstObjectByType<CardEffectPlayer>();
    }

    // ==========================================
    // RUN ALL TESTS
    // ==========================================

    [ContextMenu("Run All MR Visual Tests")]
    public void RunAllVisualTests()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "Enter Play Mode before running visual tests."
            );
            return;
        }

        if (isRunning)
        {
            Debug.LogWarning("Visual tests are already running.");
            return;
        }

        FindReferences();

        if (gameManager == null ||
            gameState == null ||
            mixedRealityUI == null ||
            cardEffectPlayer == null)
        {
            Debug.LogError(
                "MR Visual Test Runner: Missing references. " +
                "Assign GameManager, GameState, Mixed_Reality_UI, " +
                "and CardEffectPlayer."
            );
            return;
        }

        StartCoroutine(RunTests());
    }

    private IEnumerator RunTests()
    {
        isRunning = true;

        passed = 0;
        failed = 0;
        report.Clear();

        report.AppendLine("========== MR VISUAL TESTS ==========");

        if (testStatusText != null)
        {
            testStatusText.gameObject.SetActive(true);
            testStatusText.text = "STARTING MR VISUAL TESTS...";
        }

        // ------------------------------------------
        // PARTICLE EFFECT TESTS
        // ------------------------------------------

        yield return RunTestWithPause(
            "Player attack effect",
            TestPlayerAttackEffect
        );

        yield return RunTestWithPause(
            "AI attack effect",
            TestAIAttackEffect
        );

        yield return RunTestWithPause(
            "Player heal effect",
            TestPlayerHealEffect
        );

        yield return RunTestWithPause(
            "AI heal effect",
            TestAIHealEffect
        );

        yield return RunTestWithPause(
            "Player drain effect",
            TestPlayerDrainEffect
        );

        yield return RunTestWithPause(
            "AI drain effect",
            TestAIDrainEffect
        );

        yield return RunTestWithPause(
            "Player shield effect",
            TestPlayerShieldEffect
        );

        yield return RunTestWithPause(
            "AI shield effect",
            TestAIShieldEffect
        );

        yield return RunTestWithPause(
            "Shield effect removal",
            TestShieldRemoval
        );

        yield return RunTestWithPause(
            "Temporary effect cleanup",
            TestEffectCleanup
        );

        // ------------------------------------------
        // UI TESTS
        // ------------------------------------------

        yield return RunTestWithPause(
            "Player health bar",
            TestPlayerHealthUI
        );

        yield return RunTestWithPause(
            "Opponent health bar",
            TestOpponentHealthUI
        );

        yield return RunTestWithPause(
            "Shield text",
            TestShieldUI
        );

        yield return RunTestWithPause(
            "Player turn indicator",
            TestPlayerTurnUI
        );

        yield return RunTestWithPause(
            "Opponent turn indicator",
            TestOpponentTurnUI
        );

        yield return RunTestWithPause(
            "Attack message",
            TestAttackMessage
        );

        yield return RunTestWithPause(
            "Heal message",
            TestHealMessage
        );

        yield return RunTestWithPause(
            "Drain message",
            TestDrainMessage
        );

        yield return RunTestWithPause(
            "Shield message",
            TestShieldMessage
        );

        // ------------------------------------------
        // GAME OVER TESTS
        // ------------------------------------------

        yield return RunTestWithPause(
            "Player victory screen",
            TestPlayerVictoryUI
        );

        yield return RunTestWithPause(
            "AI victory screen",
            TestAIVictoryUI
        );

        yield return RunTestWithPause(
            "New game UI reset",
            TestNewGameUIReset
        );

        // ------------------------------------------
        // GAMEMANAGER INTEGRATION TESTS
        // ------------------------------------------

        yield return RunTestWithPause(
            "GameManager updates health UI",
            TestGameManagerHealthIntegration
        );

        yield return RunTestWithPause(
            "GameManager triggers particle effects",
            TestGameManagerParticleIntegration
        );

        // ------------------------------------------
        // FINAL REPORT
        // ------------------------------------------

        report.AppendLine("-------------------------------------");
        report.AppendLine($"PASSED: {passed}");
        report.AppendLine($"FAILED: {failed}");
        report.AppendLine($"TOTAL:  {passed + failed}");
        report.AppendLine("=====================================");

        Debug.Log(report.ToString());

        if (testStatusText != null)
        {
            testStatusText.text =
                "MR VISUAL TESTS COMPLETE\n\n" +
                "PASSED: " + passed + "\n" +
                "FAILED: " + failed + "\n" +
                "TOTAL: " + (passed + failed);
        }

        // Leave the final result visible before resetting the game.
        yield return new WaitForSeconds(4f);

        // Restore a fresh playable game.
        gameManager.StartGame();

        isRunning = false;
    }

    // ==========================================
    // TEST EXECUTION
    // ==========================================

    private void RunTest(string name, Action test)
    {
        try
        {
            test();

            passed++;
            report.AppendLine($"[PASS] {name}");
        }
        catch (Exception exception)
        {
            failed++;

            report.AppendLine(
                $"[FAIL] {name}: {exception.Message}"
            );

            Debug.LogException(exception);
        }
    }

    private IEnumerator RunTestWithPause(
        string testName,
        Action test)
    {
        if (testStatusText != null)
        {
            testStatusText.text = "RUNNING TEST:\n" + testName;
        }

        Debug.Log("RUNNING TEST: " + testName);

        yield return null;

        try
        {
            test();

            passed++;
            report.AppendLine("[PASS] " + testName);

            if (testStatusText != null)
            {
                testStatusText.text =
                    "PASS\n" + testName;
            }

            Debug.Log("[PASS] " + testName);
        }
        catch (Exception exception)
        {
            failed++;

            report.AppendLine(
                "[FAIL] " + testName + ": " + exception.Message
            );

            Debug.LogException(exception);

            if (testStatusText != null)
            {
                testStatusText.text =
                    "FAIL\n" + testName +
                    "\n" + exception.Message;
            }
        }

        yield return new WaitForSeconds(secondsPerTest);
    }

    // ==========================================
    // ASSERTIONS
    // ==========================================

    private void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    private void AssertEqual<T>(
        T expected,
        T actual,
        string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new Exception(
                $"{message}. Expected: {expected}, Actual: {actual}"
            );
        }
    }

    private void AssertNear(
        float expected,
        float actual,
        float tolerance,
        string message)
    {
        if (Mathf.Abs(expected - actual) > tolerance)
        {
            throw new Exception(
                $"{message}. Expected: {expected}, Actual: {actual}"
            );
        }
    }

    // ==========================================
    // PRIVATE FIELD ACCESS
    // ==========================================

    private T GetField<T>(object target, string fieldName)
    {
        Assert(target != null, "Target object is missing.");

        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public
        );

        Assert(
            field != null,
            $"Could not find field: {fieldName}"
        );

        object value = field.GetValue(target);

        Assert(
            value != null,
            $"Field '{fieldName}' is not assigned."
        );

        return (T)value;
    }

    private void SetField(
        object target,
        string fieldName,
        object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public
        );

        Assert(
            field != null,
            $"Could not find field: {fieldName}"
        );

        field.SetValue(target, value);
    }

    private List<GameObject> GetActiveEffects()
    {
        return GetField<List<GameObject>>(
            cardEffectPlayer,
            "activeEffects"
        );
    }

    private GameObject GetNewestEffect()
    {
        List<GameObject> effects = GetActiveEffects();

        Assert(
            effects.Count > 0,
            "No particle effect was spawned."
        );

        GameObject effect = effects[effects.Count - 1];

        Assert(effect != null, "Spawned effect is null.");
        Assert(effect.activeInHierarchy, "Spawned effect is inactive.");

        return effect;
    }

    // ==========================================
    // EFFECT POSITION ASSERTIONS
    // ==========================================

    // Temporary effects use their configured offsets.
    private void AssertTemporaryEffectPosition(
        GameObject effect,
        Transform expectedPoint,
        Vector3 expectedOffset)
    {
        Assert(
            expectedPoint != null,
            "Expected effect position is not assigned."
        );

        Vector3 expectedPosition =
            expectedPoint.position +
            expectedPoint.TransformDirection(expectedOffset);

        float distance = Vector3.Distance(
            effect.transform.position,
            expectedPosition
        );

        Assert(
            distance < 0.01f,
            $"Effect spawned at the wrong position. " +
            $"Expected: {expectedPosition}, " +
            $"Actual: {effect.transform.position}, " +
            $"Distance: {distance}"
        );
    }

    // Shields use their own exact local offset.
    private void AssertShieldPosition(
        GameObject shield,
        Transform expectedPoint)
    {
        Assert(
            shield.transform.parent == expectedPoint,
            "Shield is not parented to its expected effect point."
        );

        Vector3 expectedPosition =
            new Vector3(0f, 0f, -0.05f);

        float distance = Vector3.Distance(
            shield.transform.localPosition,
            expectedPosition
        );

        Assert(
            distance < 0.001f,
            $"Shield has the wrong local position. " +
            $"Expected: {expectedPosition}, " +
            $"Actual: {shield.transform.localPosition}"
        );
    }

    private void AssertParticlesPlaying(GameObject effect)
    {
        ParticleSystem[] systems =
            effect.GetComponentsInChildren<ParticleSystem>(true);

        Assert(
            systems.Length > 0,
            "Effect prefab has no ParticleSystem components."
        );

        bool anyPlaying = false;

        foreach (ParticleSystem system in systems)
        {
            if (system.isPlaying)
                anyPlaying = true;
        }

        Assert(
            anyPlaying,
            "Particle systems were not playing after spawning."
        );
    }

    // ==========================================
    // EFFECT POINT REFERENCES
    // ==========================================

    private Transform PlayerEffectPoint()
    {
        return GetField<Transform>(
            cardEffectPlayer,
            "playerEffectPoint"
        );
    }

    private Transform OpponentEffectPoint()
    {
        return GetField<Transform>(
            cardEffectPlayer,
            "opponentEffectPoint"
        );
    }

    // ==========================================
    // PARTICLE EFFECT TESTS
    // ==========================================

    private void TestPlayerAttackEffect()
    {
        ClearEffects();

        cardEffectPlayer.PlayAttack(true);

        GameObject effect = GetNewestEffect();

        AssertTemporaryEffectPosition(
            effect,
            OpponentEffectPoint(),
            new Vector3(0f, -0.2f, 0f)
        );

        AssertParticlesPlaying(effect);
    }

    private void TestAIAttackEffect()
    {
        ClearEffects();

        cardEffectPlayer.PlayAttack(false);

        GameObject effect = GetNewestEffect();

        AssertTemporaryEffectPosition(
            effect,
            PlayerEffectPoint(),
            new Vector3(0f, -0.2f, 0f)
        );

        AssertParticlesPlaying(effect);
    }

    private void TestPlayerHealEffect()
    {
        ClearEffects();

        cardEffectPlayer.PlayHeal(true);

        GameObject effect = GetNewestEffect();

        AssertTemporaryEffectPosition(
            effect,
            PlayerEffectPoint(),
            new Vector3(0f, -0.2f, 0f)
        );

        AssertParticlesPlaying(effect);
    }

    private void TestAIHealEffect()
    {
        ClearEffects();

        cardEffectPlayer.PlayHeal(false);

        GameObject effect = GetNewestEffect();

        AssertTemporaryEffectPosition(
            effect,
            OpponentEffectPoint(),
            new Vector3(0f, -0.2f, 0f)
        );

        AssertParticlesPlaying(effect);
    }

    private void TestPlayerDrainEffect()
    {
        ClearEffects();

        int previousCount = GetActiveEffects().Count;

        cardEffectPlayer.PlayDrain(true);

        List<GameObject> effects = GetActiveEffects();

        AssertEqual(
            previousCount + 1,
            effects.Count,
            "Drain should spawn exactly one effect"
        );

        GameObject effect = effects[effects.Count - 1];

        AssertTemporaryEffectPosition(
            effect,
            OpponentEffectPoint(),
            Vector3.zero
        );

        AssertParticlesPlaying(effect);
    }

    private void TestAIDrainEffect()
    {
        ClearEffects();

        int previousCount = GetActiveEffects().Count;

        cardEffectPlayer.PlayDrain(false);

        List<GameObject> effects = GetActiveEffects();

        AssertEqual(
            previousCount + 1,
            effects.Count,
            "AI drain should spawn exactly one effect"
        );

        GameObject effect = effects[effects.Count - 1];

        Assert(
            effect != null,
            "AI drain effect was not instantiated."
        );

        AssertTemporaryEffectPosition(
            effect,
            PlayerEffectPoint(),
            Vector3.zero
        );

        AssertParticlesPlaying(effect);
    }

    // ==========================================
    // SHIELD VISUAL TESTS
    // ==========================================

    private void TestPlayerShieldEffect()
    {
        cardEffectPlayer.SetShield(true, false);
        cardEffectPlayer.SetShield(true, true);

        GameObject instance = GetField<GameObject>(
            cardEffectPlayer,
            "playerShieldInstance"
        );

        Assert(
            instance != null,
            "Player shield instance was not created."
        );

        Assert(
            instance.activeSelf,
            "Player shield instance exists but is inactive."
        );

        Assert(
            instance.activeInHierarchy,
            "Player shield exists but its parent hierarchy is inactive."
        );

        AssertShieldPosition(
            instance,
            PlayerEffectPoint()
        );

        AssertParticlesPlaying(instance);
    }

    private void TestAIShieldEffect()
    {
        cardEffectPlayer.SetShield(false, false);
        cardEffectPlayer.SetShield(false, true);

        GameObject instance = GetField<GameObject>(
            cardEffectPlayer,
            "opponentShieldInstance"
        );

        Assert(
            instance != null,
            "AI shield instance was not created."
        );

        Assert(
            instance.activeSelf,
            "AI shield instance exists but is inactive."
        );

        Assert(
            instance.activeInHierarchy,
            "AI shield exists but its parent hierarchy is inactive."
        );

        AssertShieldPosition(
            instance,
            OpponentEffectPoint()
        );

        AssertParticlesPlaying(instance);
    }

    private void TestShieldRemoval()
    {
        cardEffectPlayer.SetShield(true, true);
        cardEffectPlayer.SetShield(false, true);

        cardEffectPlayer.SetShield(true, false);
        cardEffectPlayer.SetShield(false, false);

        GameObject playerShield =
            GetOptionalField<GameObject>(
                cardEffectPlayer,
                "playerShieldInstance"
            );

        GameObject opponentShield =
            GetOptionalField<GameObject>(
                cardEffectPlayer,
                "opponentShieldInstance"
            );

        Assert(
            playerShield == null,
            "Player shield reference should be cleared."
        );

        Assert(
            opponentShield == null,
            "Opponent shield reference should be cleared."
        );
    }

    private void TestEffectCleanup()
    {
        cardEffectPlayer.PlayAttack(true);

        Assert(
            GetActiveEffects().Count > 0,
            "An effect should exist before cleanup."
        );

        cardEffectPlayer.ClearEffect();

        AssertEqual(
            0,
            GetActiveEffects().Count,
            "Temporary effect list should be empty after cleanup"
        );
    }

    private void ClearEffects()
    {
        cardEffectPlayer.ClearEffect();
    }

    // ==========================================
    // OPTIONAL FIELD ACCESS
    // ==========================================

    private T GetOptionalField<T>(
        object target,
        string fieldName) where T : class
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public
        );

        Assert(
            field != null,
            $"Could not find field: {fieldName}"
        );

        return field.GetValue(target) as T;
    }

    // ==========================================
    // UI FIELD HELPERS
    // ==========================================

    private TMP_Text UIText(string fieldName)
    {
        return GetField<TMP_Text>(
            mixedRealityUI,
            fieldName
        );
    }

    private GameObject UIPanel(string fieldName)
    {
        return GetField<GameObject>(
            mixedRealityUI,
            fieldName
        );
    }

    private void AssertMessageContains(string expected)
    {
        TMP_Text message = UIText("gameMessageText");

        Assert(
            message.text.Contains(expected),
            $"Expected message to contain '{expected}', " +
            $"but received '{message.text}'."
        );

        Assert(
            UIPanel("gameMessagePanel").activeSelf,
            "Game message panel should be visible."
        );
    }

    // ==========================================
    // HEALTH BAR TESTS
    // ==========================================

    private void TestPlayerHealthUI()
    {
        mixedRealityUI.updateUI(
            10, 0,
            20, 0,
            true, false
        );

        TMP_Text healthText = UIText("playerHealthText");

        AssertEqual(
            "10 / 20 HP",
            healthText.text,
            "Player health text is incorrect"
        );

        RectTransform fill = GetOptionalField<RectTransform>(
            mixedRealityUI,
            "playerHealthFillRect"
        );

        if (fill != null)
        {
            AssertNear(
                0.5f,
                fill.anchorMax.x,
                0.01f,
                "Player health bar fill is incorrect"
            );
        }
        else
        {
            Image image = GetField<Image>(
                mixedRealityUI,
                "playerHealthFill"
            );

            AssertNear(
                0.5f,
                image.fillAmount,
                0.01f,
                "Player health image fill is incorrect"
            );
        }
    }

    private void TestOpponentHealthUI()
    {
        mixedRealityUI.updateUI(
            20, 0,
            5, 0,
            true, false
        );

        TMP_Text healthText = UIText("opponentHealthText");

        AssertEqual(
            "5 / 20 HP",
            healthText.text,
            "Opponent health text is incorrect"
        );

        RectTransform fill = GetOptionalField<RectTransform>(
            mixedRealityUI,
            "opponentHealthFillRect"
        );

        if (fill != null)
        {
            AssertNear(
                0.25f,
                fill.anchorMax.x,
                0.01f,
                "Opponent health bar fill is incorrect"
            );
        }
        else
        {
            Image image = GetField<Image>(
                mixedRealityUI,
                "opponentHealthFill"
            );

            AssertNear(
                0.25f,
                image.fillAmount,
                0.01f,
                "Opponent health image fill is incorrect"
            );
        }
    }

    private void TestShieldUI()
    {
        mixedRealityUI.updateUI(
            20, 6,
            20, 4,
            true, false
        );

        AssertEqual(
            "Shield: 6",
            UIText("playerShieldText").text,
            "Player shield text is incorrect"
        );

        AssertEqual(
            "Shield: 4",
            UIText("opponentShieldText").text,
            "Opponent shield text is incorrect"
        );
    }

    // ==========================================
    // TURN INDICATOR TESTS
    // ==========================================

    private void TestPlayerTurnUI()
    {
        mixedRealityUI.showPlayerTurnMessage();

        AssertEqual(
            "PLAYER'S TURN",
            UIText("turnText").text,
            "Player turn indicator is incorrect"
        );
    }

    private void TestOpponentTurnUI()
    {
        mixedRealityUI.showOpponentTurnMessage();

        AssertEqual(
            "OPPONENT'S TURN",
            UIText("turnText").text,
            "Opponent turn indicator is incorrect"
        );
    }

    // ==========================================
    // MESSAGE TESTS
    // ==========================================

    private void TestAttackMessage()
    {
        mixedRealityUI.showAttackMessage(6);

        AssertMessageContains("6 HP damage dealt");
    }

    private void TestHealMessage()
    {
        mixedRealityUI.showHealMessage(4);

        AssertMessageContains("+4 HP");
    }

    private void TestDrainMessage()
    {
        mixedRealityUI.showDrainMessage(6, 6);

        AssertMessageContains("6 HP damage dealt");
    }

    private void TestShieldMessage()
    {
        mixedRealityUI.showShieldMessage(8);

        AssertMessageContains("+8 shield");
    }

    // ==========================================
    // GAME OVER TESTS
    // ==========================================

    private void TestPlayerVictoryUI()
    {
        gameManager.StartGame();

        gameState.players[1].health = 0;

        gameManager.CheckWinner();

        Assert(
            UIPanel("gameOverPanel").activeSelf,
            "Game over panel should be visible."
        );

        string actualText = UIText("gameOverText").text;

        Assert(
            actualText.StartsWith(
                "Victory!",
                StringComparison.OrdinalIgnoreCase
            ),
            "Victory message is incorrect: " + actualText
        );

        AssertEqual(
            "GAME OVER",
            UIText("turnText").text,
            "Turn text should say GAME OVER"
        );
    }

    private void TestAIVictoryUI()
    {
        gameManager.StartGame();

        gameState.players[0].health = 0;

        gameManager.CheckWinner();

        Assert(
            UIPanel("gameOverPanel").activeSelf,
            "Game over panel should be visible."
        );

        string actualText = UIText("gameOverText").text;

        Assert(
            actualText.StartsWith(
                "Defeat!",
                StringComparison.OrdinalIgnoreCase
            ),
            "Defeat message is incorrect: " + actualText
        );

        AssertEqual(
            "GAME OVER",
            UIText("turnText").text,
            "Turn text should say GAME OVER"
        );
    }

    private void TestNewGameUIReset()
    {
        mixedRealityUI.showGameOver(true);

        Assert(
            UIPanel("gameOverPanel").activeSelf,
            "Game over panel should initially be active."
        );

        mixedRealityUI.PrepareForNewGame();

        Assert(
            !UIPanel("gameOverPanel").activeSelf,
            "Game over panel should be hidden after reset."
        );

        AssertEqual(
            "",
            UIText("gameOverText").text,
            "Game over text should be cleared."
        );

        AssertMessageContains("Welcome!");
    }

    // ==========================================
    // GAMEMANAGER + UI INTEGRATION
    // ==========================================

    private Card MakeCard(Suit suit, int rank)
    {
        return new Card(
            suit,
            rank,
            Card.GetAbilityForSuit(suit)
        );
    }

    private void TestGameManagerHealthIntegration()
    {
        gameManager.StartGame();

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        gameManager.ApplyAbility(
            MakeCard(Suit.Spades, 6),
            player,
            ai
        );

        AssertEqual(
            14,
            ai.health,
            "AI should have 14 HP after attack"
        );

        AssertEqual(
            "14 / 20 HP",
            UIText("opponentHealthText").text,
            "GameManager did not update opponent health UI"
        );
    }

    private void TestGameManagerParticleIntegration()
    {
        gameManager.StartGame();

        cardEffectPlayer.ClearEffect();

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        gameManager.ApplyAbility(
            MakeCard(Suit.Spades, 4),
            player,
            ai
        );

        GameObject effect = GetNewestEffect();

        AssertTemporaryEffectPosition(
            effect,
            OpponentEffectPoint(),
            new Vector3(0f, -0.2f, 0f)
        );

        AssertParticlesPlaying(effect);
    }
}
