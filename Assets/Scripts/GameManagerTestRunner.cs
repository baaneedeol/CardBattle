
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GameManagerTestRunner : MonoBehaviour
{
    [Header("Game References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameState gameState;
    [SerializeField] private AIController aiController;
    [SerializeField] private CardRecognizer cardRecognizer;
    [SerializeField] private CardEffectPlayer cardEffectPlayer;

    [Header("Test Settings")]
    [SerializeField] private float secondsBetweenTests = 2f;
    [SerializeField] private float effectTestTimeout = 60f;

    [Header("Visual Test Settings")]
    [SerializeField] private float secondsToViewTest = 4f;
    [SerializeField] private bool pauseAfterEachTest = true;

    private int passed;
    private int failed;
    private bool isRunning;

    private StringBuilder report = new StringBuilder();

    // ----------------------------------
    // INITIALIZATION
    // ----------------------------------

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

        if (aiController == null)
            aiController = FindFirstObjectByType<AIController>();

        if (cardRecognizer == null)
            cardRecognizer = FindFirstObjectByType<CardRecognizer>();

        if (cardEffectPlayer == null)
            cardEffectPlayer = FindFirstObjectByType<CardEffectPlayer>();
    }

    // ----------------------------------
    // RUN ALL TESTS
    // ----------------------------------

    [ContextMenu("Run All Game Tests")]
    public void RunAllTests()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "Enter Play Mode before running the game tests."
            );
            return;
        }

        if (isRunning)
        {
            Debug.LogWarning("Tests are already running.");
            return;
        }

        FindReferences();

        if (gameManager == null ||
            gameState == null ||
            aiController == null ||
            cardRecognizer == null ||
            cardEffectPlayer == null)
        {
            Debug.LogError(
                "Test Runner: One or more references are missing. " +
                "Assign GameManager, GameState, AIController, " +
                "CardRecognizer, and CardEffectPlayer."
            );
            return;
        }

        StartCoroutine(RunAllTestsCoroutine());
    }

    private IEnumerator RunAllTestsCoroutine()
    {
        isRunning = true;

        passed = 0;
        failed = 0;
        report.Clear();

        report.AppendLine(
            "========== MR CARD BATTLE TESTS =========="
        );

        Debug.Log(
            "========== MR CARD BATTLE TESTS: START =========="
        );

        // ----------------------------------
        // GAMEPLAY TESTS 1-19
        // ----------------------------------

        yield return RunTestWithPause(
            "1. Game initialization",
            TestGameInitialization
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "2. Player creation and health",
            TestPlayerInitialization
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "3. Starting hands and deck",
            TestStartingHands
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "4. Card recognition",
            TestCardRecognition
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "5. Reject invalid card",
            TestInvalidCard
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "6. Attack ability",
            TestAttack
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "7. Heal ability",
            TestHeal
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "8. Drain ability",
            TestDrain
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "9. Shield ability",
            TestShield
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "10. Shield absorption",
            TestShieldAbsorption
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "11. Shield expiration",
            TestShieldExpiration
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "12. Player card management",
            TestPlayerCardManagement
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "13. AI card selection",
            TestAISelection
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "14. Full player and AI turn",
            TestFullTurn
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "15. AI immediate winning move",
            TestAIWinningMove
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "16. Player victory",
            TestPlayerVictory
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "17. AI victory",
            TestAIVictory
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "18. Game over prevents moves",
            TestGameOverPreventsMoves
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "19. Shield state synchronization",
            TestShieldSynchronization
        );

        yield return TestGap();

        // ----------------------------------
        // VISUAL TESTS 20-23
        // ----------------------------------

        yield return RunTestWithPause(
            "20. Player shield visual",
            TestPlayerShieldVisual
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "21. Opponent shield visual",
            TestOpponentShieldVisual
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "22. Shield visual removal",
            TestShieldVisualRemoval
        );

        yield return TestGap();

        yield return RunTestWithPause(
            "23. Shield particle systems loop",
            TestShieldParticleLoop
        );

        yield return TestGap();

        // ----------------------------------
        // QUEUE TESTS 24-25
        // ----------------------------------

        yield return RunCoroutineTest(
            "24. Temporary effects play sequentially",
            TestEffectQueue()
        );

        yield return new WaitForSeconds(secondsToViewTest);

        yield return TestGap();

        yield return RunCoroutineTest(
            "25. Effect queue cleanup",
            TestEffectQueueCleanup()
        );

        yield return new WaitForSeconds(secondsToViewTest);

        // ----------------------------------
        // FINAL REPORT
        // ----------------------------------

        report.AppendLine("------------------------------------------");
        report.AppendLine($"PASSED: {passed}");
        report.AppendLine($"FAILED: {failed}");
        report.AppendLine($"TOTAL:  {passed + failed}");
        report.AppendLine("==========================================");

        Debug.Log(report.ToString());

        if (failed == 0)
        {
            Debug.Log(
                "<color=green>ALL GAME TESTS PASSED!</color>"
            );
        }
        else
        {
            Debug.LogError(
                $"GAME TESTS FINISHED WITH {failed} FAILURE(S)."
            );
        }

        // Clean up temporary effects.
        if (cardEffectPlayer != null)
        {
            cardEffectPlayer.ClearEffect();
            cardEffectPlayer.ClearAllShields();
        }

        // Restore a fresh playable match.
        gameManager.StartGame();

        isRunning = false;
    }

    // ----------------------------------
    // TEST PAUSE HELPERS
    // ----------------------------------

    private IEnumerator TestGap()
    {
        if (secondsBetweenTests > 0f)
            yield return new WaitForSeconds(secondsBetweenTests);
    }

    private IEnumerator RunTestWithPause(
        string testName,
        Action test)
    {
        RunTest(testName, test);

        if (pauseAfterEachTest && secondsToViewTest > 0f)
        {
            Debug.Log(
                $"<color=cyan>Viewing test: {testName}</color>"
            );

            yield return new WaitForSeconds(secondsToViewTest);
        }
    }

    // ----------------------------------
    // TEST EXECUTION
    // ----------------------------------

    private void RunTest(string testName, Action test)
    {
        Debug.Log(
            $"<color=yellow>RUNNING: {testName}</color>"
        );

        try
        {
            test();

            passed++;

            report.AppendLine($"[PASS] {testName}");

            Debug.Log(
                $"<color=green>[PASS] {testName}</color>"
            );
        }
        catch (Exception exception)
        {
            failed++;

            report.AppendLine(
                $"[FAIL] {testName}: {exception.Message}"
            );

            Debug.LogError(
                $"[FAIL] {testName}: {exception.Message}\n" +
                exception.StackTrace
            );
        }
    }

    private IEnumerator RunCoroutineTest(
        string testName,
        IEnumerator test)
    {
        Debug.Log(
            $"<color=yellow>RUNNING: {testName}</color>"
        );

        yield return null;

        Exception testException = null;

        while (true)
        {
            bool hasNext = false;
            object current = null;

            try
            {
                hasNext = test.MoveNext();

                if (hasNext)
                    current = test.Current;
            }
            catch (Exception exception)
            {
                testException = exception;
            }

            if (testException != null || !hasNext)
                break;

            yield return current;
        }

        if (testException == null)
        {
            passed++;

            report.AppendLine($"[PASS] {testName}");

            Debug.Log(
                $"<color=green>[PASS] {testName}</color>"
            );
        }
        else
        {
            failed++;

            report.AppendLine(
                $"[FAIL] {testName}: {testException.Message}"
            );

            Debug.LogException(testException);
        }
    }

    // ----------------------------------
    // ASSERTIONS
    // ----------------------------------

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
        if (!EqualityComparer<T>.Default.Equals(
            expected,
            actual))
        {
            throw new Exception(
                $"{message}. Expected: {expected}, Actual: {actual}"
            );
        }
    }

    // ----------------------------------
    // TEST HELPERS
    // ----------------------------------

    private Card MakeCard(Suit suit, int rank)
    {
        return new Card(
            suit,
            rank,
            Card.GetAbilityForSuit(suit)
        );
    }

    private void SetupMatch()
    {
        // Stop any previous visual effects.
        cardEffectPlayer.ClearEffect();
        cardEffectPlayer.ClearAllShields();

        gameState.players.Clear();
        gameState.drawPile.Clear();
        gameState.discardPile.Clear();
        gameState.playerDiscardPile.Clear();
        gameState.aiDiscardPile.Clear();
        gameState.recentPlayerActions.Clear();

        Player player = new Player
        {
            health = 20,
            maxHealth = 20,
            shield = 0,
            hand = new List<Card>()
        };

        Player ai = new Player
        {
            health = 20,
            maxHealth = 20,
            shield = 0,
            hand = new List<Card>()
        };

        gameState.players.Add(player);
        gameState.players.Add(ai);

        gameState.currentTurn = 0;
        gameState.gameOver = false;

        gameState.UpdateState();
    }

    // ==================================
    // 1. GAME INITIALIZATION
    // ==================================

    private void TestGameInitialization()
    {
        gameManager.StartGame();

        AssertEqual(
            2,
            gameState.players.Count,
            "There should be exactly two players"
        );

        AssertEqual(
            0,
            gameState.currentTurn,
            "Player should start first"
        );

        Assert(
            !gameState.gameOver,
            "New game should not be over"
        );
    }

    // ==================================
    // 2. PLAYER INITIALIZATION
    // ==================================

    private void TestPlayerInitialization()
    {
        gameManager.StartGame();

        foreach (Player player in gameState.players)
        {
            AssertEqual(20, player.health, "Incorrect health");
            AssertEqual(20, player.maxHealth, "Incorrect max health");
            AssertEqual(0, player.shield, "Shield should start at zero");
        }
    }

    // ==================================
    // 3. STARTING HANDS AND DECK
    // ==================================

    private void TestStartingHands()
    {
        gameManager.StartGame();

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        AssertEqual(
            8,
            player.hand.Count,
            "Player should start with 8 cards"
        );

        AssertEqual(
            8,
            ai.hand.Count,
            "AI should start with 8 cards"
        );

        AssertEqual(
            0,
            gameState.drawPile.Count,
            "All 16 cards should have been dealt"
        );

        HashSet<Card> allCards = new HashSet<Card>();

        foreach (Card card in player.hand)
            allCards.Add(card);

        foreach (Card card in ai.hand)
            allCards.Add(card);

        AssertEqual(
            16,
            allCards.Count,
            "Cards should be unique between hands"
        );
    }

    // ==================================
    // 4. CARD RECOGNITION
    // ==================================

    private void TestCardRecognition()
    {
        Assert(
            cardRecognizer != null,
            "CardRecognizer is missing"
        );

        SetupMatch();

        Card playerCard = MakeCard(Suit.Spades, 2);

        gameState.players[0].hand.Add(playerCard);

        // Give AI a card it can play.
        gameState.players[1].hand.Add(
            MakeCard(Suit.Spades, 2)
        );

        // Provide replacement cards for both players.
        gameState.drawPile.Add(MakeCard(Suit.Clubs, 4));
        gameState.drawPile.Add(MakeCard(Suit.Diamonds, 4));

        cardRecognizer.ResetDetection();

        cardRecognizer.OnCardDetected(Suit.Spades, 2);

        Assert(
            gameState.playerDiscardPile.Contains(playerCard),
            "Recognized card did not reach GameManager"
        );

        AssertEqual(
            0,
            gameState.currentTurn,
            "Player and AI turns should complete"
        );

        AssertEqual(
            1,
            gameState.playerDiscardPile.Count,
            "Player card should be discarded"
        );

        AssertEqual(
            1,
            gameState.aiDiscardPile.Count,
            "AI should complete its turn"
        );

        cardRecognizer.ResetDetection();
    }

    // ==================================
    // 5. INVALID CARD REJECTION
    // ==================================

    private void TestInvalidCard()
    {
        SetupMatch();

        gameState.players[0].hand.Add(
            MakeCard(Suit.Hearts, 2)
        );

        bool accepted =
            gameManager.HandleRecognizedCard(Suit.Spades, 8);

        Assert(
            !accepted,
            "Card not in player's hand should be rejected"
        );

        AssertEqual(
            1,
            gameState.players[0].hand.Count,
            "Invalid card must not change hand size"
        );

        AssertEqual(
            0,
            gameState.playerDiscardPile.Count,
            "Invalid card must not be discarded"
        );
    }

    // ==================================
    // 6. ATTACK ABILITY
    // ==================================

    private void TestAttack()
    {
        SetupMatch();

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
            "Attack should deal 6 damage"
        );
    }

    // ==================================
    // 7. HEAL ABILITY
    // ==================================

    private void TestHeal()
    {
        SetupMatch();

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        player.health = 10;

        gameManager.ApplyAbility(
            MakeCard(Suit.Hearts, 6),
            player,
            ai
        );

        AssertEqual(
            16,
            player.health,
            "Heal should restore 6 health"
        );

        player.health = 18;

        gameManager.ApplyAbility(
            MakeCard(Suit.Hearts, 6),
            player,
            ai
        );

        AssertEqual(
            20,
            player.health,
            "Healing should not exceed max health"
        );
    }

    // ==================================
    // 8. DRAIN ABILITY
    // ==================================

    private void TestDrain()
    {
        SetupMatch();

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        player.health = 10;

        gameManager.ApplyAbility(
            MakeCard(Suit.Diamonds, 4),
            player,
            ai
        );

        AssertEqual(
            14,
            player.health,
            "Drain should restore 4 HP"
        );

        AssertEqual(
            16,
            ai.health,
            "Drain should deal 4 damage"
        );
    }

    // ==================================
    // 9. SHIELD ABILITY
    // ==================================

    private void TestShield()
    {
        SetupMatch();

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        gameManager.ApplyAbility(
            MakeCard(Suit.Clubs, 8),
            player,
            ai
        );

        AssertEqual(
            8,
            player.shield,
            "Shield should provide 8 protection"
        );
    }

    // ==================================
    // 10. SHIELD ABSORPTION
    // ==================================

    private void TestShieldAbsorption()
    {
        SetupMatch();

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        ai.shield = 4;

        gameManager.ApplyAbility(
            MakeCard(Suit.Spades, 6),
            player,
            ai
        );

        AssertEqual(
            18,
            ai.health,
            "Shield should absorb 4 of 6 damage"
        );

        AssertEqual(
            0,
            ai.shield,
            "Shield should be consumed"
        );
    }

    // ==================================
    // 11. SHIELD EXPIRATION
    // ==================================

    private void TestShieldExpiration()
    {
        SetupMatch();

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        player.shield = 8;
        ai.shield = 4;

        // AI begins its turn.
        gameManager.EndTurn();

        AssertEqual(
            8,
            player.shield,
            "Player shield should persist during AI turn"
        );

        AssertEqual(
            0,
            ai.shield,
            "AI shield should expire at start of AI turn"
        );

        // Player begins their next turn.
        gameManager.EndTurn();

        AssertEqual(
            0,
            player.shield,
            "Player shield should expire at start of next turn"
        );
    }

    // ==================================
    // 12. PLAYER CARD MANAGEMENT
    // ==================================

    private void TestPlayerCardManagement()
    {
        SetupMatch();

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        Card playedCard = MakeCard(Suit.Spades, 2);

        player.hand.Add(playedCard);
        ai.hand.Add(MakeCard(Suit.Hearts, 2));

        gameState.drawPile.Add(MakeCard(Suit.Clubs, 4));
        gameState.drawPile.Add(MakeCard(Suit.Diamonds, 4));

        gameManager.ProcessPlayerCard(playedCard);

        Assert(
            !player.hand.Contains(playedCard),
            "Played card should leave player's hand"
        );

        Assert(
            gameState.playerDiscardPile.Contains(playedCard),
            "Played card should enter player's discard pile"
        );

        AssertEqual(
            1,
            player.hand.Count,
            "Player should draw a replacement card"
        );

        AssertEqual(
            1,
            ai.hand.Count,
            "AI should retain its hand size after its turn"
        );
    }

    // ==================================
    // 13. AI CARD SELECTION
    // ==================================

    private void TestAISelection()
    {
        SetupMatch();

        Player ai = gameState.players[1];

        ai.hand.Add(MakeCard(Suit.Spades, 2));
        ai.hand.Add(MakeCard(Suit.Spades, 8));
        ai.hand.Add(MakeCard(Suit.Hearts, 6));

        Card selected = aiController.SelectCard(gameState);

        Assert(
            selected != null,
            "AI should select a card"
        );

        AssertEqual(
            8,
            selected.rank,
            "AI should select its strongest attack"
        );

        Assert(
            ai.hand.Contains(selected),
            "Selected card must belong to AI"
        );
    }

    // ==================================
    // 14. FULL PLAYER AND AI TURN
    // ==================================

    private void TestFullTurn()
    {
        SetupMatch();

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        Card playerCard = MakeCard(Suit.Hearts, 2);
        Card aiCard = MakeCard(Suit.Spades, 2);

        player.hand.Add(playerCard);
        ai.hand.Add(aiCard);

        gameState.drawPile.Add(MakeCard(Suit.Clubs, 4));
        gameState.drawPile.Add(MakeCard(Suit.Diamonds, 4));

        gameManager.ProcessPlayerCard(playerCard);

        AssertEqual(
            18,
            player.health,
            "AI attack should damage player"
        );

        AssertEqual(
            20,
            ai.health,
            "Player heal should restore health"
        );

        AssertEqual(
            0,
            gameState.currentTurn,
            "Turn should return to player after AI acts"
        );

        AssertEqual(
            1,
            gameState.playerDiscardPile.Count,
            "Player card should be discarded"
        );

        AssertEqual(
            1,
            gameState.aiDiscardPile.Count,
            "AI card should be discarded"
        );

        Assert(
            !gameState.gameOver,
            "Game should still be active"
        );
    }

    // ==================================
    // 15. AI IMMEDIATE WINNING MOVE
    // ==================================

    private void TestAIWinningMove()
    {
        SetupMatch();

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        player.health = 8;

        Card winningCard = MakeCard(Suit.Spades, 8);

        ai.hand.Add(MakeCard(Suit.Spades, 4));
        ai.hand.Add(winningCard);

        Card selected = aiController.SelectCard(gameState);

        Assert(
            selected == winningCard,
            "AI should choose its immediate winning attack"
        );
    }

    // ==================================
    // 16. PLAYER VICTORY
    // ==================================

    private void TestPlayerVictory()
    {
        SetupMatch();

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        ai.health = 2;

        gameManager.ApplyAbility(
            MakeCard(Suit.Spades, 4),
            player,
            ai
        );

        gameManager.CheckWinner();

        Assert(
            gameState.gameOver,
            "Game should end when AI reaches zero HP"
        );

        AssertEqual(
            0,
            ai.health,
            "AI health should be zero"
        );
    }

    // ==================================
    // 17. AI VICTORY
    // ==================================

    private void TestAIVictory()
    {
        SetupMatch();

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        player.health = 2;

        gameManager.ApplyAbility(
            MakeCard(Suit.Spades, 4),
            ai,
            player
        );

        gameManager.CheckWinner();

        Assert(
            gameState.gameOver,
            "Game should end when player reaches zero HP"
        );

        AssertEqual(
            0,
            player.health,
            "Player health should be zero"
        );
    }

    // ==================================
    // 18. GAME OVER PREVENTS MOVES
    // ==================================

    private void TestGameOverPreventsMoves()
    {
        SetupMatch();

        Player player = gameState.players[0];

        Card card = MakeCard(Suit.Spades, 2);
        player.hand.Add(card);

        gameState.gameOver = true;

        bool accepted =
            gameManager.HandleRecognizedCard(Suit.Spades, 2);

        Assert(
            !accepted,
            "Game should reject cards after game over"
        );

        AssertEqual(
            1,
            player.hand.Count,
            "Hand should not change after game over"
        );
    }

    // ==================================
    // 19. SHIELD STATE SYNCHRONIZATION
    // ==================================

    private void TestShieldSynchronization()
    {
        SetupMatch();

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        gameManager.ApplyAbility(
            MakeCard(Suit.Clubs, 6),
            player,
            ai
        );

        AssertEqual(
            6,
            player.shield,
            "Player shield state should update"
        );

        ai.shield = 8;

        gameManager.EndTurn();
        gameManager.EndTurn();

        AssertEqual(
            0,
            player.shield,
            "Player shield should expire"
        );

        AssertEqual(
            0,
            ai.shield,
            "AI shield should expire at start of its next turn"
        );
    }

    // ==================================
    // 20. PLAYER SHIELD VISUAL
    // ==================================

    private void TestPlayerShieldVisual()
    {
        SetupMatch();

        cardEffectPlayer.SetShield(true, true);

        GameObject playerShield =
            cardEffectPlayer.GetPlayerShield();

        Assert(
            playerShield != null,
            "Player shield visual was not created"
        );

        Assert(
            playerShield.activeInHierarchy,
            "Player shield should be active"
        );

        Vector3 expectedPosition =
            new Vector3(0f, 0f, -0.05f);

        Assert(
            Vector3.Distance(
                playerShield.transform.localPosition,
                expectedPosition
            ) < 0.001f,
            "Player shield should use the standard shield offset"
        );
    }

    // ==================================
    // 21. OPPONENT SHIELD VISUAL
    // ==================================

    private void TestOpponentShieldVisual()
    {
        SetupMatch();

        cardEffectPlayer.SetShield(false, true);

        GameObject opponentShield =
            cardEffectPlayer.GetOpponentShield();

        Assert(
            opponentShield != null,
            "Opponent shield visual was not created"
        );

        Assert(
            opponentShield.activeInHierarchy,
            "Opponent shield should be active"
        );

        Vector3 expectedPosition =
            new Vector3(0f, 0f, -0.05f);

        Assert(
            Vector3.Distance(
                opponentShield.transform.localPosition,
                expectedPosition
            ) < 0.001f,
            "Opponent shield should use the standard shield offset"
        );
    }

    // ==================================
    // 22. SHIELD VISUAL REMOVAL
    // ==================================

    private void TestShieldVisualRemoval()
    {
        SetupMatch();

        cardEffectPlayer.SetShield(true, true);
        cardEffectPlayer.SetShield(false, true);

        Assert(
            cardEffectPlayer.GetPlayerShield() != null,
            "Player shield should exist before removal"
        );

        Assert(
            cardEffectPlayer.GetOpponentShield() != null,
            "Opponent shield should exist before removal"
        );

        cardEffectPlayer.SetShield(true, false);
        cardEffectPlayer.SetShield(false, false);

        Assert(
            cardEffectPlayer.GetPlayerShield() == null,
            "Player shield reference should be cleared"
        );

        Assert(
            cardEffectPlayer.GetOpponentShield() == null,
            "Opponent shield reference should be cleared"
        );
    }

    // ==================================
    // 23. SHIELD PARTICLE LOOP
    // ==================================

    private void TestShieldParticleLoop()
    {
        SetupMatch();

        cardEffectPlayer.SetShield(true, true);

        GameObject playerShield =
            cardEffectPlayer.GetPlayerShield();

        Assert(
            playerShield != null,
            "Player shield prefab was not created"
        );

        ParticleSystem[] systems =
            playerShield.GetComponentsInChildren<ParticleSystem>(true);

        Assert(
            systems.Length > 0,
            "Shield prefab has no ParticleSystem components"
        );

        foreach (ParticleSystem system in systems)
        {
            Assert(
                system.main.loop,
                "Shield particle system should have looping enabled"
            );

            Assert(
                system.isPlaying,
                "Shield particle system should be playing"
            );
        }
    }

    // ==================================
    // 24. TEMPORARY EFFECT QUEUE
    // ==================================

    private IEnumerator TestEffectQueue()
    {
        SetupMatch();

        int startingCount =
            cardEffectPlayer.TotalEffectsPlayed;

        cardEffectPlayer.PlayAttack(true);
        cardEffectPlayer.PlayHeal(true);

        Assert(
            cardEffectPlayer.IsPlayingQueuedEffect,
            "Effect queue should start playing"
        );

        AssertEqual(
            1,
            cardEffectPlayer.QueuedEffectCount,
            "Second effect should wait in the queue"
        );

        AssertEqual(
            1,
            cardEffectPlayer.ActiveTemporaryEffectCount,
            "Only one temporary effect should be active initially"
        );

        float elapsed = 0f;

        while (cardEffectPlayer.IsPlayingQueuedEffect &&
               elapsed < effectTestTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        Assert(
            !cardEffectPlayer.IsPlayingQueuedEffect,
            "Effect queue did not finish before timeout"
        );

        AssertEqual(
            startingCount + 2,
            cardEffectPlayer.TotalEffectsPlayed,
            "Both queued effects should have played"
        );

        AssertEqual(
            0,
            cardEffectPlayer.QueuedEffectCount,
            "Effect queue should be empty after playback"
        );
    }

    // ==================================
    // 25. EFFECT QUEUE CLEANUP
    // ==================================

    private IEnumerator TestEffectQueueCleanup()
    {
        SetupMatch();

        cardEffectPlayer.PlayAttack(true);
        cardEffectPlayer.PlayHeal(true);
        cardEffectPlayer.PlayDrain(true);

        Assert(
            cardEffectPlayer.IsPlayingQueuedEffect,
            "Queue should be active before cleanup"
        );

        cardEffectPlayer.ClearEffect();

        AssertEqual(
            0,
            cardEffectPlayer.QueuedEffectCount,
            "Queue should be empty after cleanup"
        );

        AssertEqual(
            0,
            cardEffectPlayer.ActiveTemporaryEffectCount,
            "Active temporary effects should be cleared"
        );

        Assert(
            !cardEffectPlayer.IsPlayingQueuedEffect,
            "Queue coroutine should stop after cleanup"
        );

        AssertEqual(
            0,
            cardEffectPlayer.TotalEffectsPlayed,
            "Effect counter should reset after cleanup"
        );

        yield return null;
    }
}
