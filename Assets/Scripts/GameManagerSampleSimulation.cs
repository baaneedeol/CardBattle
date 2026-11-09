using System.Collections;
using System.Text;
using UnityEngine;

public class FullGameSimulationTestRunner : MonoBehaviour
{
    [Header("Existing Game References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameState gameState;
    [SerializeField] private GameFlow gameFlow;

    [Header("Simulation Settings")]
    [SerializeField] private float delayBetweenRounds = 1f;
    [SerializeField] private int maximumRounds = 200;
    [SerializeField] private bool useAutomaticPlayerStrategy = true;

    [Header("Turn Sequence Settings")]
    [SerializeField] private float turnProcessingTimeout = 90f;

    private Coroutine simulationCoroutine;
    private bool isRunning;

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

        if (gameFlow == null)
            gameFlow = FindFirstObjectByType<GameFlow>();
    }

    // True when the board was removed or reset (B pressed) mid-simulation.
    // A normal game over also clears IsMatchReady, so it is excluded here.
    private bool BoardWasCleared()
    {
        return gameFlow != null &&
               !gameFlow.IsMatchReady &&
               !gameState.gameOver;
    }

    [ContextMenu("Run Full Game Simulation")]
    public void RunFullGameSimulation()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode before running the simulation.");
            return;
        }

        if (isRunning)
        {
            Debug.LogWarning("A simulation is already running.");
            return;
        }

        FindReferences();

        if (gameManager == null || gameState == null || gameFlow == null)
        {
            Debug.LogError("Simulation references are missing.");
            return;
        }

        simulationCoroutine = StartCoroutine(RunSimulation());
    }

    [ContextMenu("Stop Full Game Simulation")]
    public void StopFullGameSimulation()
    {
        if (simulationCoroutine != null)
        {
            StopCoroutine(simulationCoroutine);
            simulationCoroutine = null;
        }

        isRunning = false;
        Debug.Log("Simulation stopped manually.");
    }

    private IEnumerator RunSimulation()
    {
        isRunning = true;

        Debug.Log("========== FULL GAME SIMULATION START ==========");

        // ----------------------------------
        // TABLE PLACEMENT
        // ----------------------------------
        // GameFlow owns setup: placement -> StartGame -> DealHands.
        // Wait for the wearer to aim, rotate and pull the trigger.
        // (If the Start button or TablePlacer.beginOnStart already
        // began placement, BeginPlacement is harmless to call again.)
        if (!gameFlow.IsMatchReady)
        {
            Debug.Log("Waiting for table placement...");
            gameFlow.BeginPlacement();

            while (!gameFlow.IsMatchReady)
                yield return null;

            Debug.Log("Board placed. Starting simulation.");
        }

        // Let DealHands run its first frame.
        yield return null;

        if (gameState.players == null || gameState.players.Count < 2)
        {
            Debug.LogError("Two players were not initialized.");
            FinishSimulation();
            yield break;
        }

        Player player = gameState.players[0];
        Player ai = gameState.players[1];

        Debug.Log(
            $"Initial state | Player: {player.health} HP, " +
            $"{player.hand.Count} cards | " +
            $"AI: {ai.health} HP, {ai.hand.Count} cards"
        );

        StringBuilder matchLog = new StringBuilder();

        matchLog.AppendLine("========== FULL MATCH SUMMARY ==========");

        int completedRounds = 0;
        bool boardCleared = false;

        while (!gameState.gameOver &&
               completedRounds < maximumRounds)
        {
            if (BoardWasCleared())
            {
                Debug.Log("Board was cleared. Stopping simulation.");
                boardCleared = true;
                break;
            }

            bool playerHasCards =
                player.hand != null && player.hand.Count > 0;

            bool aiHasCards =
                ai.hand != null && ai.hand.Count > 0;

            // Both hands are empty. GameManager should declare
            // the winner through its end-of-deck check.
            if (!playerHasCards && !aiHasCards)
            {
                Debug.Log("Both players have used all their cards.");
                break;
            }

            // The player has no cards, but the AI still has cards.
            if (!playerHasCards && aiHasCards)
            {
                Debug.Log("Player has no cards remaining. AI continues.");

                // Record the state before the AI attempts to play.
                int previousPlayerHealth = player.health;
                int previousAIHealth = ai.health;

                int previousPlayerShield = player.shield;
                int previousAIShield = ai.shield;

                int previousPlayerHand = player.hand.Count;
                int previousAIHand = ai.hand.Count;

                int previousPlayerDiscard = gameState.playerDiscardPile.Count;
                int previousAIDiscard = gameState.aiDiscardPile.Count;

                // Switch to AI turn if necessary.
                if (gameState.currentTurn == 0)
                    gameManager.EndTurn();

                gameManager.ProcessAITurn();

                float aiElapsed = 0f;

                while (gameManager.IsProcessingTurn &&
                       aiElapsed < turnProcessingTimeout &&
                       !BoardWasCleared())
                {
                    aiElapsed += Time.deltaTime;
                    yield return null;
                }

                if (BoardWasCleared())
                {
                    Debug.Log("Board was cleared. Stopping simulation.");
                    boardCleared = true;
                    break;
                }

                if (gameManager.IsProcessingTurn)
                {
                    Debug.LogError("AI-only turn timed out.");
                    break;
                }

                // Check whether the AI actually did anything.
                bool stateChanged =
                    player.health != previousPlayerHealth ||
                    ai.health != previousAIHealth ||
                    player.shield != previousPlayerShield ||
                    ai.shield != previousAIShield ||
                    player.hand.Count != previousPlayerHand ||
                    ai.hand.Count != previousAIHand ||
                    gameState.playerDiscardPile.Count != previousPlayerDiscard ||
                    gameState.aiDiscardPile.Count != previousAIDiscard;

                if (!stateChanged)
                {
                    Debug.LogWarning(
                        "AI has no legal actions remaining. " +
                        "Ending match based on remaining health."
                    );

                    gameManager.EndGameByRemainingHealth();

                    break;
                }
                
                completedRounds++;

                LogRoundSummary(
                    completedRounds,
                    player,
                    ai,
                    matchLog
                );

                if (gameState.gameOver)
                    break;

                if (delayBetweenRounds > 0f)
                    yield return new WaitForSeconds(delayBetweenRounds);

                continue;
            }

            // Normal player + AI round.
            if (gameState.currentTurn != 0)
            {
                Debug.LogError(
                    $"Unexpected player turn state: {gameState.currentTurn}"
                );
                break;
            }

            Card chosenCard = useAutomaticPlayerStrategy
                ? ChoosePlayerCard(player, ai)
                : player.hand[Random.Range(0, player.hand.Count)];

            if (chosenCard == null ||
                !player.hand.Contains(chosenCard))
            {
                Debug.LogError("Could not select a valid player card.");
                break;
            }

            Debug.Log(
                $"<color=yellow>--- ROUND {completedRounds + 1} ---</color>"
            );

            Debug.Log($"Player plays: {chosenCard}");

            // GameManager fires CardPlayed -> GameFlow -> board.PlayCard,
            // so the card is shown in MR without any extra call here.
            gameManager.ProcessPlayerCard(chosenCard);

            float elapsed = 0f;

            while (gameManager.IsProcessingTurn &&
                   elapsed < turnProcessingTimeout &&
                   !BoardWasCleared())
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (BoardWasCleared())
            {
                Debug.Log("Board was cleared. Stopping simulation.");
                boardCleared = true;
                break;
            }

            if (gameManager.IsProcessingTurn)
            {
                Debug.LogError("Player/AI turn sequence timed out.");
                break;
            }

            completedRounds++;

            LogRoundSummary(
                completedRounds,
                player,
                ai,
                matchLog
            );

            if (gameState.gameOver)
                break;

            if (delayBetweenRounds > 0f)
                yield return new WaitForSeconds(delayBetweenRounds);
        }

        // Final end-of-deck check.
        if (!gameState.gameOver &&
            !boardCleared &&
            player.hand.Count == 0 &&
            ai.hand.Count == 0)
        {
            Debug.Log("Checking final match result.");
        }

        matchLog.AppendLine("----------------------------------------");
        matchLog.AppendLine($"Rounds completed: {completedRounds}");
        matchLog.AppendLine($"Final player HP: {player.health}");
        matchLog.AppendLine($"Final AI HP: {ai.health}");
        matchLog.AppendLine($"Player cards remaining: {player.hand.Count}");
        matchLog.AppendLine($"AI cards remaining: {ai.hand.Count}");
        matchLog.AppendLine(
            $"Player cards discarded: {gameState.playerDiscardPile.Count}"
        );
        matchLog.AppendLine(
            $"AI cards discarded: {gameState.aiDiscardPile.Count}"
        );

        if (gameState.gameOver)
        {
            string result;

            if (player.health > ai.health)
                result = "PLAYER WINS";
            else if (ai.health > player.health)
                result = "AI WINS";
            else
                result = "DRAW";

            matchLog.AppendLine($"Result: {result}");

            Debug.Log(
                $"<color=green>SIMULATION FINISHED: {result}</color>"
            );
        }
        else if (boardCleared)
        {
            matchLog.AppendLine("Result: Board was cleared before the game ended.");
            Debug.LogWarning("Simulation stopped because the board was cleared.");
        }
        else if (completedRounds >= maximumRounds)
        {
            matchLog.AppendLine("Result: Maximum-round limit reached.");
            Debug.LogWarning("Simulation reached the maximum-round limit.");
        }
        else
        {
            matchLog.AppendLine("Result: Simulation stopped unexpectedly.");
            Debug.LogWarning("Simulation stopped before the game ended.");
        }

        matchLog.AppendLine("========================================");

        Debug.Log(matchLog.ToString());

        FinishSimulation();
    }

    private void LogRoundSummary(
        int round,
        Player player,
        Player ai,
        StringBuilder matchLog)
    {
        string summary =
            $"Round {round}: " +
            $"Player {player.health} HP / {player.shield} shield; " +
            $"AI {ai.health} HP / {ai.shield} shield; " +
            $"Player hand {player.hand.Count}; " +
            $"AI hand {ai.hand.Count}.";

        Debug.Log($"<color=cyan>{summary}</color>");
        matchLog.AppendLine(summary);
    }

    private Card ChoosePlayerCard(Player player, Player opponent)
    {
        if (player == null ||
            opponent == null ||
            player.hand == null ||
            player.hand.Count == 0)
        {
            return null;
        }

        Card card = FindBestCard(
            player,
            AbilityType.Attack,
            opponent.health + opponent.shield
        );

        if (card != null)
            return card;

        card = FindBestCard(
            player,
            AbilityType.Drain,
            opponent.health + opponent.shield
        );

        if (card != null)
            return card;

        if (player.health <= 10)
        {
            card = FindBestCard(player, AbilityType.Heal, 0);

            if (card != null)
                return card;
        }

        card = FindBestCard(player, AbilityType.Attack, 0);

        if (card != null)
            return card;

        card = FindBestCard(player, AbilityType.Drain, 0);

        if (card != null)
            return card;

        if (player.shield == 0)
        {
            card = FindBestCard(player, AbilityType.Shield, 0);

            if (card != null)
                return card;
        }

        card = FindBestCard(player, AbilityType.Heal, 0);

        if (card != null)
            return card;

        return player.hand[Random.Range(0, player.hand.Count)];
    }

    private Card FindBestCard(
        Player player,
        AbilityType ability,
        int minimumRank)
    {
        Card best = null;

        foreach (Card card in player.hand)
        {
            if (card == null ||
                !card.IsValid() ||
                card.ability != ability ||
                card.rank < minimumRank)
            {
                continue;
            }

            if (best == null || card.rank > best.rank)
                best = card;
        }

        return best;
    }

    private void FinishSimulation()
    {
        isRunning = false;
        simulationCoroutine = null;
    }
}