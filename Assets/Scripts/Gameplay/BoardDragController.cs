using DG.Tweening.Core.Easing;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class BoardDragController : MonoBehaviour
{
    [Header("Refs")]
    public Transform blockRoot;

    [Header("Picking (2D)")]
    public LayerMask blockLayer;

    [Header("Collision (2D)")]
    [Tooltip("Layer mask của tất cả vật cản: blocks + walls + obstacles.")]
    public LayerMask obstacleLayer;

    [Tooltip("Khoảng hở nhỏ để tránh dính sát collider.")]
    public float skin = 0.01f;

    [Header("Camera/Plane")]
    public Camera cam;
    public float boardPlaneZ = 0f;

    [Header("Drag Feel")]
    public float followSharpness = 12f;
    public float dragStartThresholdPx = 4f;

    [Header("Snap")]
    public bool snapOnRelease = true;

    [Header("Snap Smooth (SmoothDamp)")]
    public bool smoothSnapOnRelease = true;

    [Tooltip("Thời gian để SmoothDamp gần tới đích. Nhỏ = nhanh, lớn = chậm.")]
    public float snapSmoothTime = 0.08f;

    [Tooltip("Vận tốc tối đa của SmoothDamp (units/sec).")]
    public float snapMaxSpeed = 40f;

    [Tooltip("Khoảng cách coi như đã tới đích.")]
    public float snapArriveEps = 0.001f;

    private Plane boardPlane;

    // fingerId -> session
    private readonly Dictionary<int, DragSession> sessions = new(8);

    // blockIndex -> fingerId owner
    private readonly Dictionary<int, int> blockOwnerByIndex = new(64);

    private const int MOUSE_ID = -1;

    private void Awake()
    {
        if (!cam) cam = Camera.main;
        boardPlane = new Plane(Vector3.forward, new Vector3(0, 0, boardPlaneZ));

        GameController.onSetupCamera += OnSetupCamera;
    }
    public void OnSetupCamera(int width)
    {
        cam.orthographicSize = width + 4f;
    }    
    private void Update()
    {

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseAsTouch();
#endif
        HandleTouches();
    }

    private void FixedUpdate()
    {
        if (sessions.Count == 0) return;

        // iterate on a copy of keys so we can remove sessions safely
        var keys = ListPool<int>.Get();
        foreach (var k in sessions.Keys) keys.Add(k);

        for (int ii = 0; ii < keys.Count; ii++)
        {
            int id = keys[ii];
            if (!sessions.TryGetValue(id, out var s)) continue;

            if (s.blockView == null || s.rb == null)
            {
                ForceFinalizeSession(id);
                continue;
            }

            // 1) While dragging: follow desiredWorld
            if (s.startedDragging && s.hasDesiredWorld && !s.isSnapping)
            {
                s.rb.MovePosition(s.desiredWorld);
                continue;
            }

            // 2) After release: SmoothDamp snap
            if (s.isSnapping)
            {
                Vector2 cur = s.rb.position;

                Vector2 next = Vector2.SmoothDamp(
                    cur,
                    s.snapTargetWorld,
                    ref s.snapVelocity,
                    Mathf.Max(0.0001f, snapSmoothTime),
                    snapMaxSpeed,
                    Time.fixedDeltaTime
                );

                s.rb.MovePosition(next);

                if ((s.snapTargetWorld - next).sqrMagnitude <= snapArriveEps * snapArriveEps)
                {
                    s.rb.position = s.snapTargetWorld;
                    // Freeze
                    s.rb.velocity = Vector2.zero;
                    s.rb.angularVelocity = 0f;
                    s.rb.bodyType = RigidbodyType2D.Static;

                    // Commit board model
                    //CommitBlockToBoard(s);

                    // Finalize
                    FinalizeSession(s);
                }
            }
        }

        ListPool<int>.Release(keys);
    }

    private void HandleTouches()
    {
        if(GameController.GameState != EnumGameState.Playing) return;
        int count = Input.touchCount;
        for (int i = 0; i < count; i++)
        {
            Touch t = Input.GetTouch(i);
            int id = t.fingerId;

            switch (t.phase)
            {
                case TouchPhase.Began:
                    TryBegin(id, t.position);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (sessions.TryGetValue(id, out var sMove))
                        UpdateDrag(sMove, t.position);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (sessions.TryGetValue(id, out var sEnd))
                        EndDrag(sEnd);
                    break;
            }
        }
    }

    private void HandleMouseAsTouch()
    {
        if (GameController.GameState != EnumGameState.Playing) return;
        Vector2 pos = Input.mousePosition;

        if (Input.GetMouseButtonDown(0)) TryBegin(MOUSE_ID, pos);
        else if (Input.GetMouseButton(0))
        {
            if (sessions.TryGetValue(MOUSE_ID, out var sMove))
                UpdateDrag(sMove, pos);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (sessions.TryGetValue(MOUSE_ID, out var sEnd))
                EndDrag(sEnd);
        }
    }

    private void TryBegin(int fingerId, Vector2 screenPos)
    {
        Vector3 world = ScreenToWorldOnPlane(screenPos);
        var col = Physics2D.OverlapPoint(world, blockLayer);
        if (col == null) return;

        var bv = col.GetComponentInParent<BlockView>();
        if (!bv) return;
        if (bv.pouring) return;
        if (bv.iceCount > 0) return;


        int idx = bv.block.index;
        if (idx < 0) return;
        if (!GameController.board.blocks.TryGetValue(idx, out var block)) return;


        GameController.Instance.topUI.StartCountTime();

        // If this block is already owned by another session, finalize that session
        if (blockOwnerByIndex.TryGetValue(idx, out int ownerFingerId) && ownerFingerId != fingerId)
            ForceFinalizeSession(ownerFingerId);

        // If this finger already has a session, finalize it first
        if (sessions.ContainsKey(fingerId))
            ForceFinalizeSession(fingerId);

        var rb = bv.GetComponent<Rigidbody2D>();
        if (!rb)
        {
            return;
        }

        // MUST be Dynamic so collisions can happen
        rb.bodyType = RigidbodyType2D.Dynamic;

        

        // Stop any residual motion
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        

        // Grab offset in LOCAL grid space
        Vector3 pointerLocal = blockRoot.InverseTransformPoint(world);
        Vector3 blockLocal = blockRoot.InverseTransformPoint(rb.position);
        Vector3 grabOffsetLocal = pointerLocal - blockLocal;

        var s = new DragSession
        {
            fingerId = fingerId,
            blockIndex = idx,
            blockView = bv,
            rb = rb,

            axis = block.data.direction,

            startedDragging = false,
            startPointerScreen = screenPos,
            grabOffsetLocal = grabOffsetLocal,

            hasDesiredWorld = false,
            isSnapping = false,
            snapVelocity = Vector2.zero
        };

        sessions[fingerId] = s;
        blockOwnerByIndex[idx] = fingerId;
    }

    private void UpdateDrag(DragSession s, Vector2 screenPos)
    {

        if (!s.startedDragging)
        {
            if ((screenPos - s.startPointerScreen).sqrMagnitude < dragStartThresholdPx * dragStartThresholdPx)
                return;

            s.startedDragging = true;
        }

        Vector3 pointerWorld = ScreenToWorldOnPlane(screenPos);
        Vector3 pointerLocal = blockRoot.InverseTransformPoint(pointerWorld);

        // desired in local grid
        Vector3 desiredLocal = pointerLocal - s.grabOffsetLocal;
        desiredLocal.z = 0f;

        // Current RB local
        Vector3 curLocal = blockRoot.InverseTransformPoint(s.rb.position);
        curLocal.z = 0f;

        // Axis constraint
        if (s.axis == MoveAxis.Horizontal) desiredLocal.y = curLocal.y;
        else if (s.axis == MoveAxis.Vertical) desiredLocal.x = curLocal.x;

        // smoothing in LOCAL, then convert to world
        Vector3 targetLocal;
        if (followSharpness <= 0f) targetLocal = desiredLocal;
        else
        {
            float k = 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime);
            targetLocal = Vector3.Lerp(curLocal, desiredLocal, k);
        }

        s.desiredWorld = blockRoot.TransformPoint(targetLocal);
        s.hasDesiredWorld = true;

        // If user drags again while snapping, cancel snap
        s.isSnapping = false;
        s.snapVelocity = Vector2.zero;
    }
    public float cellSize = 1f;
    
    private void EndDrag(DragSession s)
    {

        if (!snapOnRelease)
        {
            s.rb.velocity = Vector2.zero;
            s.rb.angularVelocity = 0f;
            s.rb.bodyType = RigidbodyType2D.Static;

            CommitBlockToBoard(s);
            FinalizeSession(s);
            return;
        }

        // stop drag-follow
        s.hasDesiredWorld = false;

        // 1) Current pivot position in LOCAL grid space
        Vector3 pivotLocal3 = blockRoot.InverseTransformPoint(s.rb.position);
        pivotLocal3.z = 0f;
        Vector2 pivotLocal = pivotLocal3;

        // 2) Determine pivot->center offset in LOCAL grid units
        //    Square 2x2 has center offset (0.5, 0.5) from bottom-left pivot.
        //    If your prefab pivot is already centered, set this = Vector2.zero.
        Vector2 pivotToCenterLocal = ShapeLibrary.GetBlockOffset(s.blockView.block.data.shapeType, s.blockView.block.data.rotation);


        // 3) Compute center position (local)
        Vector2 centerLocal = pivotLocal + pivotToCenterLocal;

        // 4) Snap center to nearest integer cell
        int cx = Mathf.RoundToInt(centerLocal.x);
        int cy = Mathf.RoundToInt(centerLocal.y);

        // 5) Axis constraint: only snap on the allowed axis
        if (s.axis == MoveAxis.Horizontal) cy = Mathf.RoundToInt(centerLocal.y);
        else if (s.axis == MoveAxis.Vertical) cx = Mathf.RoundToInt(centerLocal.x);

        Vector2 snappedCenterLocal = new Vector2(cx, cy);

        // 6) Convert snapped center back to pivot position
        Vector2 snappedPivotLocal = snappedCenterLocal - pivotToCenterLocal;

        // 7) Convert to world
        Vector2 snapWorld = blockRoot.TransformPoint((Vector3)snappedPivotLocal);

        CommitBlockToBoard(s);
        //FinalizeSession(s);

        if (!smoothSnapOnRelease)
        {
            s.rb.position = snapWorld;
            s.rb.velocity = Vector2.zero;
            s.rb.angularVelocity = 0f;
            s.rb.bodyType = RigidbodyType2D.Static;

            //CommitBlockToBoard(s);
            FinalizeSession(s);
            return;
        }

        // keep Dynamic during snap
        s.rb.bodyType = RigidbodyType2D.Dynamic;

        // Start snapping state
        s.isSnapping = true;
        s.snapTargetWorld = snapWorld;
        s.snapVelocity = Vector2.zero;
    }


    private void CommitBlockToBoard(DragSession s)
    {

        Vector2Int oldPos = GameController.board.blocks[s.blockIndex].data.position;

        Vector3 finalLocal = blockRoot.InverseTransformPoint(s.rb.position);
        Vector2 offset = ShapeLibrary.GetBlockOffset(s.blockView.block.data.shapeType, s.blockView.block.data.rotation);
        Vector2Int targetCell = new Vector2Int(Mathf.RoundToInt(finalLocal.x - offset.x), Mathf.RoundToInt(finalLocal.y - offset.y));

        if (targetCell != oldPos)
        {
            GameController.board.UpdateBlockOccupancy(s.blockIndex, oldPos, targetCell);
            GameController.board.CheckBlocksOnPipes();
        }    

    }

    private void FinalizeSession(DragSession s)
    {
        blockOwnerByIndex.Remove(s.blockIndex);
        sessions.Remove(s.fingerId);
    }

    private void ForceFinalizeSession(int fingerId)
    {
        if (!sessions.TryGetValue(fingerId, out var s)) return;

        if (s.rb != null)
        {
            s.rb.velocity = Vector2.zero;
            s.rb.angularVelocity = 0f;
            s.rb.bodyType = RigidbodyType2D.Static;
        }

        blockOwnerByIndex.Remove(s.blockIndex);
        sessions.Remove(fingerId);
    }

    private Vector3 ScreenToWorldOnPlane(Vector2 screenPos)
    {
        if (!cam) cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (boardPlane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);
        return Vector3.zero;
    }

    private class DragSession
    {
        public int fingerId;
        public int blockIndex;
        public BlockView blockView;
        public Rigidbody2D rb;

        public MoveAxis axis;

        public bool startedDragging;
        public Vector2 startPointerScreen;
        public Vector3 grabOffsetLocal;

        public bool hasDesiredWorld;
        public Vector2 desiredWorld;

        public bool isSnapping;
        public Vector2 snapTargetWorld;
        public Vector2 snapVelocity;
    }

    private static class ListPool<T>
    {
        private static readonly Stack<List<T>> Pool = new();
        public static List<T> Get() => Pool.Count > 0 ? Pool.Pop() : new List<T>(16);
        public static void Release(List<T> list) { list.Clear(); Pool.Push(list); }
    }
}
