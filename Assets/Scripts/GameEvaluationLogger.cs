using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Writes one summary row per game and one action row per card played.
/// Files are saved under Application.persistentDataPath/ResearchLogs.
/// </summary>
public class GameEvaluationLogger
{
    private const string SummaryFileName = "GameSummaries.csv";
    private const string ActionFileName = "GameActionLogs.csv";

    // PlayerPrefs persists these values across app and headset restarts.
    private const string NextParticipantNumberKey = "Research_NextParticipantNumber";
    private const string CurrentParticipantIdKey = "Research_CurrentParticipantId";

    private string participantId;
    private string sessionId;
    private string startTime;
    private float startRealtime;
    private int playerTurnCount;
    private int actionCount;
    private bool gameActive;

    private readonly List<string> playerCards = new List<string>();
    private readonly List<string> aiCards = new List<string>();
    private readonly List<string> playerActions = new List<string>();
    private readonly List<string> aiActions = new List<string>();

    public bool IsGameActive => gameActive;

    /// <summary>
    /// Returns the current participant ID. Creates P01 (or the next available
    /// number) automatically if no participant has been assigned yet.
    /// </summary>
    public static string GetOrCreateParticipantId()
    {
        string currentId = PlayerPrefs.GetString(CurrentParticipantIdKey, string.Empty);

        if (!string.IsNullOrWhiteSpace(currentId))
            return currentId;

        return StartNewParticipant();
    }

    /// <summary>
    /// Assigns and persists the next participant ID: P01, P02, P03, ...
    /// Call once when a new person is about to begin.
    /// </summary>
    public static string StartNewParticipant()
    {
        int nextNumber = Mathf.Max(
            1,
            PlayerPrefs.GetInt(NextParticipantNumberKey, 1)
        );

        string newParticipantId = $"P{nextNumber:D2}";

        PlayerPrefs.SetInt(NextParticipantNumberKey, nextNumber + 1);
        PlayerPrefs.SetString(CurrentParticipantIdKey, newParticipantId);
        PlayerPrefs.Save();

        return newParticipantId;
    }

    private string LogDirectory =>
        Path.Combine(Application.persistentDataPath, "ResearchLogs");

    public void BeginGame(string participant, int startingHealth)
    {
        participantId = string.IsNullOrWhiteSpace(participant) ? "Unspecified" : participant.Trim();
        sessionId = Guid.NewGuid().ToString("N");
        startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        startRealtime = Time.realtimeSinceStartup;
        playerTurnCount = 0;
        actionCount = 0;
        gameActive = true;

        playerCards.Clear();
        aiCards.Clear();
        playerActions.Clear();
        aiActions.Clear();

        Debug.Log(
            $"[Research Logging] Started game for {participantId}. " +
            $"Logs will be saved to: {LogDirectory}"
        );
    }

    public void RecordCardAction(
        string actor,
        Card card,
        int playerHpAfter,
        int aiHpAfter)
    {
        if (!gameActive || card == null)
            return;

        actionCount++;

        bool isPlayer = string.Equals(actor, "Player", StringComparison.OrdinalIgnoreCase);
        if (isPlayer)
            playerTurnCount++;

        string cardName = $"{card.suit} {card.rank}";
        string abilityName = card.ability.ToString();

        if (isPlayer)
        {
            playerCards.Add(cardName);
            playerActions.Add(abilityName);
        }
        else
        {
            aiCards.Add(cardName);
            aiActions.Add(abilityName);
        }

        int turnNumber = playerTurnCount;
        string actionRow = string.Join(",",
            Csv(participantId),
            Csv(sessionId),
            actionCount.ToString(CultureInfo.InvariantCulture),
            turnNumber.ToString(CultureInfo.InvariantCulture),
            Csv(actor),
            Csv(card.suit.ToString()),
            card.rank.ToString(CultureInfo.InvariantCulture),
            Csv(abilityName),
            playerHpAfter.ToString(CultureInfo.InvariantCulture),
            aiHpAfter.ToString(CultureInfo.InvariantCulture)
        );

        AppendCsv(
            ActionFileName,
            "ParticipantID,SessionID,ActionNumber,TurnNumber,Actor,Suit,Rank,Ability,PlayerHPAfter,AIHPAfter",
            actionRow
        );
    }

    public void FinishGame(string outcome, GameState state, string endReason = "")
    {
        if (!gameActive)
            return;

        float durationSeconds = Mathf.Max(0f, Time.realtimeSinceStartup - startRealtime);

        int playerHp = 0;
        int aiHp = 0;

        if (state != null && state.players != null)
        {
            if (state.players.Count > 0 && state.players[0] != null)
                playerHp = state.players[0].health;

            if (state.players.Count > 1 && state.players[1] != null)
                aiHp = state.players[1].health;
        }

        string summaryRow = string.Join(",",
            Csv(participantId),
            Csv(sessionId),
            Csv(startTime),
            Csv(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
            durationSeconds.ToString("F2", CultureInfo.InvariantCulture),
            playerTurnCount.ToString(CultureInfo.InvariantCulture),
            actionCount.ToString(CultureInfo.InvariantCulture),
            playerCards.Count.ToString(CultureInfo.InvariantCulture),
            aiCards.Count.ToString(CultureInfo.InvariantCulture),
            playerHp.ToString(CultureInfo.InvariantCulture),
            aiHp.ToString(CultureInfo.InvariantCulture),
            Csv(string.Join("; ", playerCards)),
            Csv(string.Join("; ", aiCards)),
            Csv(string.Join("; ", playerActions)),
            Csv(string.Join("; ", aiActions)),
            Csv(outcome),
            Csv(endReason)
        );

        AppendCsv(
            SummaryFileName,
            "ParticipantID,SessionID,StartTime,EndTime,DurationSeconds,PlayerTurns,TotalActions,PlayerCardsPlayed,AICardsPlayed,FinalPlayerHP,FinalAIHP,PlayerCardList,AICardList,PlayerActions,AIActions,Outcome,EndReason",
            summaryRow
        );

        gameActive = false;

        Debug.Log(
            $"[Research Logging] Game saved. Participant: {participantId}, " +
            $"Turns: {playerTurnCount}, Duration: {durationSeconds:F1}s, " +
            $"Outcome: {outcome}. Files: {LogDirectory}"
        );
    }

    private void AppendCsv(string fileName, string header, string row)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            string path = Path.Combine(LogDirectory, fileName);
            bool needsHeader = !File.Exists(path) || new FileInfo(path).Length == 0;

            using (StreamWriter writer = new StreamWriter(path, true, new UTF8Encoding(true)))
            {
                if (needsHeader)
                    writer.WriteLine(header);

                writer.WriteLine(row);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[Research Logging] Could not write {fileName}: {exception.Message}"
            );
        }
    }

    private static string Csv(string value)
    {
        value = value ?? string.Empty;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
