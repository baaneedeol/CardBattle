using UnityEngine;
using System.Collections.Generic;
using Meta.XR.BuildingBlocks.AIBlocks;

public class CardDetectionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ObjectDetectionAgent detectionAgent;

    [Tooltip("Receives every detection result while a hand scan is running.")]
    [SerializeField]
    private CardHandScanner scanner;

    [Header("Scan loop")]
    [Tooltip("Minimum seconds between inference requests while scanning.")]
    [SerializeField] private float scanInterval = 0.2f;

    [Tooltip("If a response never arrives, request again after this many seconds.")]
    [SerializeField] private float responseTimeout = 3f;

    [Header("Detection power")]
    [Tooltip("Seconds to wait after switching the agent on before the first request.")]
    [SerializeField] private float startupDelay = 1f;

    private List<BoxData> detectedCards = new List<BoxData>();

    private bool inferencePending;
    private bool detectionActive;
    private float readyTime;
    private float lastRequestTime = float.NegativeInfinity;

    private void OnEnable()
    {
        detectionAgent.OnDetectionResponseReceived
            .AddListener(OnCardsDetected);
    }

    private void OnDisable()
    {
        detectionAgent.OnDetectionResponseReceived
            .RemoveListener(OnCardsDetected);

        inferencePending = false;
    }

    // Detection is disabled at startup. It is switched on when the hand scan
    // begins (after the table is placed) and off again when the scan ends.
    // While on, inference is requested one request at a time.
    private void Update()
    {
        bool wantActive = scanner != null && scanner.IsScanning;

        if (wantActive != detectionActive)
            SetDetectionActive(wantActive);

        if (!wantActive)
        {
            inferencePending = false;
            return;
        }

        if (Time.time < readyTime)
            return;

        float sinceLast = Time.time - lastRequestTime;

        if (inferencePending && sinceLast < responseTimeout)
            return;

        if (sinceLast < scanInterval)
            return;

        ScanCards();
    }

    private void OnCardsDetected(List<BoxData> detections)
    {
        inferencePending = false;

        detectedCards = new List<BoxData>(detections);

        // Feed the hand scanner. BoxData's label is used as the class name;
        // no confidence is read from it, so every box is passed as 1.0.
        if (scanner != null && scanner.IsScanning)
        {
            var results = new List<(string label, float confidence)>(
                detectedCards.Count
            );

            foreach (var card in detectedCards)
                results.Add((card.label, 1f));

            scanner.ReportDetections(results);
        }
    }

    public List<BoxData> GetDetectedCards()
    {
        return new List<BoxData>(detectedCards);
    }

    public List<string> GetDetectedCardLabels()
    {
        List<string> labels = new List<string>();

        foreach (var card in detectedCards)
        {
            labels.Add(card.label);
        }

        return labels;
    }

    public void SetDetectionActive(bool active)
    {
        detectionActive = active;
        inferencePending = false;

        if (active)
            readyTime = Time.time + startupDelay;

        if (detectionAgent == null)
            return;

        // If this manager shares a GameObject with the agent, deactivating
        // the object would stop this script, so only toggle the component.
        if (detectionAgent.gameObject != gameObject)
            detectionAgent.gameObject.SetActive(active);
        else
            Debug.LogWarning(
                "CardDetectionManager shares a GameObject with the detection agent. " +
                "Move it to an always-active object so detection can be switched off."
            );

        detectionAgent.enabled = active;
    }

    public void ScanCards()
    {
        inferencePending = true;
        lastRequestTime = Time.time;

        detectionAgent.CallInference();
    }
}