using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Passive hand-scanning step. Knows nothing about the game: GameFlow starts a scan
/// after the table is placed, your YOLO script feeds it detections every inference
/// frame, and it reports back the set of physical cards the player is holding.
///
/// A card is "locked in" once it has been detected with enough confidence for
/// requiredStableFrames consecutive frames. The scan completes when the requested
/// number of distinct cards is locked.
///
/// Expected YOLO class labels (case-insensitive, separators '_', '-', ' '):
///   "Hearts_6", "6_Hearts", "6 of Hearts", "hearts6" ...
/// Edit ParseLabel if your class names differ.
/// </summary>
public class CardHandScanner : MonoBehaviour
{
    [Header("Filtering")]
    [Range(0f, 1f)]
    [SerializeField] private float confidenceThreshold = 0.6f;

    [Tooltip("Ranks that exist in this game's deck. Other detected cards are ignored.")]
    [SerializeField] private int[] validRanks = { 2, 4, 6, 8 };

    [Tooltip("Consecutive inference frames a card must be seen before it is locked in.")]
    [SerializeField] private int requiredStableFrames = 8;

    [Tooltip("Frames a card may go undetected before its stability counter resets.")]
    [SerializeField] private int missedFrameTolerance = 2;

    [Header("Debug")]
    [SerializeField] private bool logDetections = false;

    private class Track
    {
        public int stableFrames;
        public int missedFrames;
    }

    private readonly Dictionary<(Suit suit, int rank), Track> tracks = new();
    private readonly List<(Suit suit, int rank)> locked = new();
    private readonly List<(Suit suit, int rank)> toRemove = new();
    private readonly HashSet<string> warnedLabels = new();

    private int requiredCards;

    public bool IsScanning { get; private set; }
    public int LockedCount => locked.Count;
    public int RequiredCards => requiredCards;

    /// <summary>Fired whenever a new card locks in: (found, required).</summary>
    public event Action<int, int> ScanProgress;

    /// <summary>Fired the moment each card locks in: (suit, rank, index in scan order).</summary>
    public event Action<Suit, int, int> CardLocked;

    /// <summary>Fired once with the full set of scanned cards.</summary>
    public event Action<List<(Suit suit, int rank)>> HandScanned;

    public void BeginScan(int cardCount)
    {
        tracks.Clear();
        locked.Clear();
        requiredCards = Mathf.Max(1, cardCount);
        IsScanning = true;

        ScanProgress?.Invoke(0, requiredCards);
    }

    public void CancelScan()
    {
        IsScanning = false;
        tracks.Clear();
        locked.Clear();
    }

    /// <summary>
    /// Call once per inference frame with every box the model returned.
    /// Ignored when no scan is running.
    /// </summary>
    public void ReportDetections(IEnumerable<(string label, float confidence)> detections)
    {
        if (!IsScanning) return;

        var seen = new HashSet<(Suit, int)>();

        foreach (var (label, confidence) in detections)
        {
            if (confidence < confidenceThreshold) continue;

            if (!ParseLabel(label, out Suit suit, out int rank))
            {
                if (warnedLabels.Add(label))
                    Debug.LogWarning(
                        $"[CardHandScanner] Could not parse label '{label}' into a suit and rank. " +
                        "Edit ParseLabel to match your class names.");
                continue;
            }

            if (System.Array.IndexOf(validRanks, rank) < 0)
            {
                if (warnedLabels.Add(label))
                    Debug.LogWarning(
                        $"[CardHandScanner] '{label}' is detected but rank {rank} " +
                        "is not part of this game's deck; ignoring it.");
                continue;
            }

            seen.Add((suit, rank));
        }

        if (logDetections && seen.Count > 0)
            Debug.Log($"[CardHandScanner] Frame saw {seen.Count} card(s)");

        foreach (var key in seen)
        {
            if (locked.Contains(key)) continue;

            if (!tracks.TryGetValue(key, out Track t))
            {
                t = new Track();
                tracks[key] = t;
            }

            t.stableFrames++;
            t.missedFrames = 0;

            if (t.stableFrames >= requiredStableFrames)
            {
                locked.Add(key);
                tracks.Remove(key);

                Debug.Log($"[CardHandScanner] Locked {key.Item1} {key.Item2} " +
                          $"({locked.Count}/{requiredCards})");

                CardLocked?.Invoke(key.Item1, key.Item2, locked.Count - 1);
                ScanProgress?.Invoke(locked.Count, requiredCards);

                if (locked.Count >= requiredCards)
                {
                    Complete();
                    return;
                }
            }
        }

        toRemove.Clear();

        foreach (var kv in tracks)
        {
            if (seen.Contains(kv.Key)) continue;

            kv.Value.missedFrames++;

            if (kv.Value.missedFrames > missedFrameTolerance)
                toRemove.Add(kv.Key);
        }

        foreach (var key in toRemove)
            tracks.Remove(key);
    }

    /// <summary>Convenience overload for a single detection.</summary>
    public void ReportDetection(string label, float confidence)
    {
        ReportDetections(new[] { (label, confidence) });
    }

    private void Complete()
    {
        IsScanning = false;

        var result = new List<(Suit suit, int rank)>(locked);

        tracks.Clear();
        locked.Clear();

        HandScanned?.Invoke(result);
    }

    // Matches a full suit name ("Clubs") or a single-letter code ("C") to the
    // Suit enum. A single letter matches the enum value whose name starts with it.
    private static bool TryParseSuit(string text, out Suit suit)
    {
        suit = default;

        if (string.IsNullOrEmpty(text))
            return false;

        // Full enum name (ignore purely numeric strings, which Enum.TryParse accepts).
        if (!char.IsDigit(text[0]) &&
            Enum.TryParse(text, true, out Suit parsed) &&
            Enum.IsDefined(typeof(Suit), parsed))
        {
            suit = parsed;
            return true;
        }

        // Single-letter code, e.g. "C" -> Clubs.
        if (text.Length == 1)
        {
            bool found = false;

            foreach (Suit candidate in Enum.GetValues(typeof(Suit)))
            {
                string name = candidate.ToString();

                if (name.Length > 0 &&
                    char.ToUpperInvariant(name[0]) ==
                    char.ToUpperInvariant(text[0]))
                {
                    if (found)
                    {
                        Debug.LogWarning(
                            $"[CardHandScanner] Suit letter '{text}' matches more than one " +
                            "Suit value; edit TryParseSuit with an explicit mapping.");
                        return false;
                    }

                    suit = candidate;
                    found = true;
                }
            }

            return found;
        }

        return false;
    }

    // "10c" -> Clubs 10, "Ah" -> Hearts 1 (A=1, J=11, Q=12, K=13).
    private static bool TryParseCompact(string label, out Suit suit, out int rank)
    {
        suit = default;
        rank = 0;

        string s = label.Trim();

        if (s.Length < 2 || s.Length > 3)
            return false;

        if (!char.IsLetter(s[s.Length - 1]))
            return false;

        if (!TryParseSuit(s[s.Length - 1].ToString(), out suit))
            return false;

        string rankText = s.Substring(0, s.Length - 1);

        if (int.TryParse(rankText, out int numeric))
        {
            rank = numeric;
            return true;
        }

        if (rankText.Length == 1)
        {
            switch (char.ToUpperInvariant(rankText[0]))
            {
                case 'A': rank = 1; return true;
                case 'J': rank = 11; return true;
                case 'Q': rank = 12; return true;
                case 'K': rank = 13; return true;
            }
        }

        return false;
    }

    // LABEL PARSING - edit to match your YOLO class names.
    public static bool ParseLabel(string label, out Suit suit, out int rank)
    {
        suit = default;
        rank = 0;

        if (string.IsNullOrWhiteSpace(label)) return false;

        // Compact format: rank then suit letter, e.g. "10c", "8h", "Ac", "Ks".
        if (TryParseCompact(label, out suit, out rank))
            return true;

        string[] parts = label.Split(
            new[] { '_', '-', ' ' },
            StringSplitOptions.RemoveEmptyEntries
        );

        bool gotSuit = false, gotRank = false;

        foreach (string raw in parts)
        {
            string p = raw.Trim();

            if (string.Equals(p, "of", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!gotRank && int.TryParse(p, out int r))
            {
                rank = r;
                gotRank = true;
            }
            else if (!gotSuit && TryParseSuit(p, out Suit s))
            {
                suit = s;
                gotSuit = true;
            }
            else
            {
                // Fused names like "hearts6" or "6hearts".
                string digits = string.Empty;
                string letters = string.Empty;

                foreach (char ch in p)
                {
                    if (char.IsDigit(ch)) digits += ch;
                    else letters += ch;
                }

                if (!gotRank && digits.Length > 0 && int.TryParse(digits, out int fr))
                {
                    rank = fr;
                    gotRank = true;
                }

                if (!gotSuit && letters.Length > 0 && TryParseSuit(letters, out Suit fs))
                {
                    suit = fs;
                    gotSuit = true;
                }
            }
        }

        return gotSuit && gotRank;
    }
}