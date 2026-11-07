using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class TablePlacer : MonoBehaviour
{
    private enum State { Idle, Aiming, Placed }
    private const OVRInput.Controller Hand = OVRInput.Controller.RTouch;

    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARAnchorManager anchorManager;
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private Transform head;
    [SerializeField] private GameObject boardPrefab;   // pivot = bottom-center of footprint
    [SerializeField] private LayerMask planeMask = ~0; // set to the ARPlane layer only
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private float yawSpeed = 90f;     // deg/sec, thumbstick X
    [SerializeField] private bool beginOnStart = false; // testing without the Start button

    public event Action<GameObject> Placed;
    public event Action Cleared;   // fired when a placed board is reset

    private State state = State.Idle;
    private GameObject board;
    private ARAnchor anchor;
    private float yaw;

    private void Awake()
    {
        if (head == null && Camera.main != null)
            head = Camera.main.transform;
    }

    private void Start()
    {
        planeManager.enabled = false;
        if (beginOnStart) Begin();
    }

    private void OnEnable()  => planeManager.trackablesChanged.AddListener(OnPlanesChanged);
    private void OnDisable() => planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);

    // Start button -> GameFlow.BeginPlacement -> this. B calls it again to start over.
    [ContextMenu("Begin")]
    public void Begin()
    {
        Clear();
        board = Instantiate(boardPrefab);
        board.SetActive(false);
        yaw = 0f;
        state = State.Aiming;
        planeManager.enabled = true;
        foreach (ARPlane p in planeManager.trackables) Refresh(p);
    }

    private void Update()
    {
        if (state == State.Idle) return;

        if (OVRInput.GetDown(OVRInput.Button.Two, Hand)) { Begin(); return; }
        if (state != State.Aiming) return;

        if (!TryGetTableHit(out RaycastHit hit)) { board.SetActive(false); return; }

        yaw += OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, Hand).x * yawSpeed * Time.deltaTime;
        board.transform.SetPositionAndRotation(
            hit.point, FacePlayer(hit.point) * Quaternion.Euler(0f, yaw, 0f));
        board.SetActive(true);

        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, Hand)) Place();
    }

    private async void Place()
    {
        state = State.Placed;
        planeManager.enabled = false;
        planeManager.SetTrackablesActive(false);   // hides every plane
        GameObject placed = board;
        Placed?.Invoke(placed);

        if (anchorManager == null) return;

        var result = await anchorManager.TryAddAnchorAsync(
            new Pose(placed.transform.position, placed.transform.rotation));

        // The board may have been reset while awaiting.
        if (placed == null || board != placed || state != State.Placed)
        {
            if (result.status.IsSuccess() && result.value != null)
                Destroy(result.value.gameObject);
            return;
        }

        if (result.status.IsSuccess())
        {
            anchor = result.value;
            placed.transform.SetParent(anchor.transform, true);
        }
        else Debug.LogWarning("Anchor failed; board placed but not anchored.");
    }

    private void Clear()
    {
        if (state == State.Placed) Cleared?.Invoke();
        if (board != null) Destroy(board);
        if (anchor != null) Destroy(anchor.gameObject);
        board = null;
        anchor = null;
        state = State.Idle;
    }

    // ---------- helpers ----------

    private static bool IsTable(ARPlane p) =>
        (p.classifications & PlaneClassifications.Table) != 0;

    private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> c)
    {
        foreach (ARPlane p in c.added)   Refresh(p);
        foreach (ARPlane p in c.updated) Refresh(p);
    }
    

    private void Refresh(ARPlane p)
    {
        bool show = state == State.Aiming && IsTable(p);
        p.gameObject.SetActive(show);

        // Planes mutate their mesh in place, so force a collider rebake.
        if (show && p.TryGetComponent(out MeshCollider col) &&
            p.TryGetComponent(out MeshFilter mf))
        {
            col.sharedMesh = null;
            col.sharedMesh = mf.sharedMesh;
        }
    }

    private bool TryGetTableHit(out RaycastHit hit) =>
        Physics.Raycast(rayOrigin.position, rayOrigin.forward, out hit, maxDistance,
                        planeMask, QueryTriggerInteraction.Ignore)
        && hit.collider.GetComponentInParent<ARPlane>() is ARPlane p
        && IsTable(p);

    private Quaternion FacePlayer(Vector3 point)
    {
        Vector3 away = Vector3.ProjectOnPlane(point - head.position, Vector3.up);
        return away.sqrMagnitude > 1e-4f
            ? Quaternion.LookRotation(away, Vector3.up)
            : Quaternion.identity;
    }
}