using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DocumentController : MonoBehaviour
{
    // Global: prevent grabbing multiple overlapping documents in one click.
    private static DocumentController activeDragOwner;
    private static readonly Collider2D[] overlapPointHits = new Collider2D[32];

    private static readonly List<DocumentController> s_instances = new(16);
    private static ulong s_nextTouchOrder = 1;

    /// <summary>Higher = interacted with more recently; used to pick top document and assign sorting bands.</summary>
    private ulong touchOrder;

    [Header("States")]
    public GameObject closedState;
    public GameObject openedState;

    [Header("Drag / Offset rules")]
    [Tooltip("Local point used only when moving from closed desk to inspect while holding: cursor aligns to this point (often center).")]
    [SerializeField]
    private Vector3 inspectGrabLocalPoint = Vector3.zero;

    [Tooltip("If set, Y is snapped here when entering inspect (fallen/stacked Y is corrected). Leave empty to keep world Y.")]
    [SerializeField]
    private Transform inspectSurfaceAnchor;

    [Tooltip("While dragging: small circle at the cursor tests desk zones (crossing feels like the old behavior). Idle: used only if sprite bounds are empty.")]
    [SerializeField]
    private float zoneOverlapProbeRadius = 0.14f;

    [Header("Rendering")]
    [SerializeField]
    private int sortingOrderInspectDesk = 5; // legacy (kept for compatibility; overridden by dynamic orders below)

    [SerializeField]
    private int sortingOrderClosedDesk = 100; // legacy (kept for compatibility; overridden by dynamic orders below)

    [Header("Dynamic order-in-layer (stacking)")]
    [Tooltip("If enabled: when you grab a document, it moves to the global front (highest sorting order).")]
    [SerializeField] private bool raiseToFrontOnGrab = true;

    [Tooltip("View sprites use base+stride. TMP is expected at base+1, so stride should be >= 2.")]
    [SerializeField]
    [Min(2)]
    private int documentOrderStride = 2;

    [Tooltip("If enabled, sets world-space TMP renderers to base+1 so they stay between the document base and view sprites.")]
    [SerializeField]
    private bool syncWorldTmpSortingOrder = true;

    [Header("Sorting bands (per desk zone)")]
    [Tooltip("Inspect desk: base sorting order min..max (inclusive). TMP uses base+1, view sprites base+stride.")]
    [SerializeField] private int openDeskSortingMin = 1;
    [SerializeField] private int openDeskSortingMax = 14;

    [Tooltip("Closed desk: base sorting order min..max (inclusive). TMP uses base+1, view sprites base+stride.")]
    [SerializeField] private int closedDeskSortingMin = 16;
    [SerializeField] private int closedDeskSortingMax = 31;

    [Header("Screen bounds")]
    [SerializeField]
    private bool clampDocumentToScreen = true;

    [Tooltip("World-space margin from the camera edge (keeps the sprite from touching the border).")]
    [SerializeField]
    private float screenClampPaddingWorld = 0.05f;

    [Tooltip("On inspect desk only: at least this fraction of sprite width must stay inside the camera rect (rest may go past the edge).")]
    [SerializeField]
    [Range(0.05f, 1f)]
    private float inspectMinVisibleWidthFraction = 0.35f;

    [Tooltip("On inspect desk only: at least this fraction of sprite height must stay inside the camera rect.")]
    [SerializeField]
    [Range(0.05f, 1f)]
    private float inspectMinVisibleHeightFraction = 0.35f;

    private bool isDragging;
    private bool interactionEnabled = true;
    private Vector3 grabLocalPoint;
    private Vector3 savedGrabLocalPointFromClosedDesk;
    private Camera mainCamera;
    private Collider2D closedCollider;
    private Collider2D openedCollider;
    private Rigidbody2D documentRigidbody;
    private RigidbodyType2D savedBodyType = RigidbodyType2D.Dynamic;
    private float savedGravityScale = 1f;

    private bool isOverClosedDesk;
    private bool isOverInspectDesk;
    private bool physicsOverClosedDesk;
    private bool physicsOverInspectDesk;
    private TableTrigger.TableZone currentZone = TableTrigger.TableZone.ClosedDesk;
    private SpriteRenderer[] cachedSpriteRenderers;
    private int currentBaseSortingOrder;

    private void OnEnable()
    {
        if (!s_instances.Contains(this))
            s_instances.Add(this);
    }

    private void OnDisable()
    {
        s_instances.Remove(this);
    }

    void Start()
    {
        mainCamera = Camera.main;
        cachedSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        closedCollider = closedState != null ? closedState.GetComponent<Collider2D>() : null;
        openedCollider = openedState != null ? openedState.GetComponent<Collider2D>() : null;

        documentRigidbody = GetComponent<Rigidbody2D>();
        if (documentRigidbody != null)
        {
            savedBodyType = documentRigidbody.bodyType;
            savedGravityScale = documentRigidbody.gravityScale;
        }

        SetState(isOpened: false);
        RefreshDeskZoneFlagsFromPhysics();
        ApplyRigidbodyModeForPhysicsZone(GetPhysicsZone());
        ApplyIdleZoneFromFlags();
        touchOrder = ++s_nextTouchOrder;
        RebuildAllStacks();

        //GameFlowController.OnStateChanged += HandleGameStateChanged;
    }

    /*
    void OnDestroy()
    {
        GameFlowController.OnStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState prev, GameState next)
    {
        interactionEnabled = (next == GameState.VisitorPresent);
        if (!interactionEnabled && isDragging)
            OnMouseUpInternal();
    }
    */ 

    void FixedUpdate()
    {
        if (documentRigidbody == null || isDragging) return;
        RefreshDeskZoneFlagsFromPhysics();
        ApplyRigidbodyModeForPhysicsZone(GetPhysicsZone());
    }

    void LateUpdate()
    {
        if (isDragging) return;
        ApplyIdleZoneFromFlags();
    }

    void Update()
    {
        if (!interactionEnabled) return;
        if (!closedState.activeSelf && !openedState.activeSelf) return;

        Collider2D currentCollider = closedState.activeSelf ? closedCollider : openedCollider;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;

            if (currentCollider != null && currentCollider.OverlapPoint(mouseWorldPos))
            {
                // Only the topmost document under cursor can start dragging.
                if (CanStartDragFromPoint(mouseWorldPos))
                    OnMouseDownInternal(mouseWorldPos);
            }
        }

        RefreshDeskZoneFlagsFromPhysics();

        if (isDragging)
        {
            SyncZoneWhileDragging();
            OnMouseDragInternal();
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
            OnMouseUpInternal();
    }

    private void OnMouseDownInternal(Vector3 mouseWorldPos)
    {
        activeDragOwner = this;
        isDragging = true;
        SetRigidbodyForDrag(true);

        grabLocalPoint = transform.InverseTransformPoint(mouseWorldPos);
        if (currentZone == TableTrigger.TableZone.ClosedDesk)
            savedGrabLocalPointFromClosedDesk = grabLocalPoint;

        if (currentZone == TableTrigger.TableZone.InspectDesk)
            SetState(isOpened: true);
        else
            SetState(isOpened: false);

        if (raiseToFrontOnGrab)
            touchOrder = ++s_nextTouchOrder;
        RebuildAllStacks();
    }

    private void OnMouseDragInternal()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        AlignLocalGrabPointToCursor(mouseWorldPos);
    }

    private void OnMouseUpInternal()
    {
        RefreshDeskZoneFlagsFromPhysics(useCursorProbe: true);
        bool dropClosed = isOverClosedDesk;
        bool dropInspect = isOverInspectDesk;

        isDragging = false;
        if (activeDragOwner == this) activeDragOwner = null;
        RefreshDeskZoneFlagsFromPhysics(useCursorProbe: false);

        SetRigidbodyForDrag(false);

        if (!dropClosed && !dropInspect)
        {
            SetState(isOpened: false);
            currentZone = TableTrigger.TableZone.ClosedDesk;
            RebuildAllStacks();
            ClampDocumentToScreenBounds();
            return;
        }

        if (dropInspect)
        {
            currentZone = TableTrigger.TableZone.InspectDesk;
            SetState(isOpened: true);
            RebuildAllStacks();
        }
        else
        {
            currentZone = TableTrigger.TableZone.ClosedDesk;
            SetState(isOpened: false);
            RebuildAllStacks();
        }

        ClampDocumentToScreenBounds();

        // Hand over document if dropped into delivery zone.
        DocumentDeliverable deliverable = GetComponent<DocumentDeliverable>();
        if (deliverable != null)
            deliverable.TryDeliverOnDrop();
    }

    public void SetState(bool isOpened)
    {
        if (closedState != null) closedState.SetActive(!isOpened);
        if (openedState != null) openedState.SetActive(isOpened);
    }

    private void ApplyIdleZoneFromFlags()
    {
        TableTrigger.TableZone t = GetTargetZoneFromFlags();
        if (t == currentZone) return;

        currentZone = t;
        bool open = t == TableTrigger.TableZone.InspectDesk;
        SetState(open);
        if (open)
            SnapInspectSurfaceYIfConfigured();
        RebuildAllStacks();
    }

    private TableTrigger.TableZone GetTargetZoneFromFlags()
    {
        return DocumentDeskZoneProbe.ResolveTargetZone(isDragging, currentZone, isOverClosedDesk, isOverInspectDesk);
    }

    private TableTrigger.TableZone GetPhysicsZone()
    {
        return DocumentDeskZoneProbe.ResolvePhysicsZone(currentZone, physicsOverClosedDesk, physicsOverInspectDesk);
    }

    private void SyncZoneWhileDragging()
    {
        TableTrigger.TableZone target = GetTargetZoneFromFlags();
        if (target == currentZone)
            return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        if (target == TableTrigger.TableZone.InspectDesk && currentZone == TableTrigger.TableZone.ClosedDesk)
        {
            savedGrabLocalPointFromClosedDesk = grabLocalPoint;
            grabLocalPoint = inspectGrabLocalPoint;
            AlignLocalGrabPointToCursor(mouseWorldPos);
            SnapInspectSurfaceYIfConfigured();
            SetState(isOpened: true);
            currentZone = TableTrigger.TableZone.InspectDesk;
            RebuildAllStacks();
            return;
        }

        if (target == TableTrigger.TableZone.ClosedDesk && currentZone == TableTrigger.TableZone.InspectDesk)
        {
            grabLocalPoint = savedGrabLocalPointFromClosedDesk;
            AlignLocalGrabPointToCursor(mouseWorldPos);
            SetState(isOpened: false);
            currentZone = TableTrigger.TableZone.ClosedDesk;
            RebuildAllStacks();
        }
    }

    private void SnapInspectSurfaceYIfConfigured()
    {
        if (inspectSurfaceAnchor == null) return;
        Vector3 p = transform.position;
        p.y = inspectSurfaceAnchor.position.y;
        transform.position = p;
        ClampDocumentToScreenBounds();
    }

    private void AlignLocalGrabPointToCursor(Vector3 mouseWorldPos)
    {
        Vector3 grabWorld = transform.TransformPoint(grabLocalPoint);
        Vector3 delta = mouseWorldPos - grabWorld;
        delta.z = 0f;
        transform.position += delta;
        ClampDocumentToScreenBounds();
    }

    private Bounds ComputeActiveSpriteBoundsWorld()
    {
        Bounds b = new Bounds(transform.position, Vector3.zero);
        bool has = false;
        if (cachedSpriteRenderers != null)
        {
            for (int i = 0; i < cachedSpriteRenderers.Length; i++)
            {
                SpriteRenderer sr = cachedSpriteRenderers[i];
                if (sr == null || !sr.gameObject.activeInHierarchy) continue;
                if (!has)
                {
                    b = sr.bounds;
                    has = true;
                }
                else
                    b.Encapsulate(sr.bounds);
            }
        }

        if (!has)
            return new Bounds(transform.position, Vector3.zero);
        return b;
    }

    private void ClampDocumentToScreenBounds()
    {
        DocumentScreenClamp.ClampDocument(
            transform,
            mainCamera,
            clampDocumentToScreen,
            screenClampPaddingWorld,
            currentZone,
            inspectMinVisibleWidthFraction,
            inspectMinVisibleHeightFraction,
            ComputeActiveSpriteBoundsWorld);
    }

    private void RefreshDeskZoneFlagsFromPhysics()
    {
        RefreshDeskZoneFlagsFromPhysics(useCursorProbe: isDragging);
    }

    private void RefreshDeskZoneFlagsFromPhysics(bool useCursorProbe)
    {
        DeskZoneFlags f = DocumentDeskZoneProbe.Refresh(
            useCursorProbe,
            mainCamera,
            zoneOverlapProbeRadius,
            transform,
            ComputeActiveSpriteBoundsWorld);

        isOverClosedDesk = f.IsOverClosedDesk;
        isOverInspectDesk = f.IsOverInspectDesk;
        physicsOverClosedDesk = f.PhysicsOverClosedDesk;
        physicsOverInspectDesk = f.PhysicsOverInspectDesk;
    }

    private void SetRigidbodyForDrag(bool dragging)
    {
        if (documentRigidbody == null) return;

        if (dragging)
        {
            documentRigidbody.velocity = Vector2.zero;
            documentRigidbody.angularVelocity = 0f;
            documentRigidbody.simulated = false;
        }
        else
        {
            documentRigidbody.simulated = true;
            Transform t = documentRigidbody.transform;
            documentRigidbody.position = t.position;
            documentRigidbody.rotation = t.eulerAngles.z;
            documentRigidbody.velocity = Vector2.zero;
            documentRigidbody.angularVelocity = 0f;
            Physics2D.SyncTransforms();
            ApplyRigidbodyModeForPhysicsZone(GetPhysicsZone());
        }
    }

    private void ApplyRigidbodyModeForPhysicsZone(TableTrigger.TableZone physicsZone)
    {
        DocumentRigidbodyByZone.ApplyModeForZone(documentRigidbody, physicsZone, savedBodyType, savedGravityScale);
    }

    private void ApplySortingOrderForZone(TableTrigger.TableZone zone)
    {
        // Include dynamically spawned children (e.g. StampMark) — stale cache would leave stamps at default order.
        cachedSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (cachedSpriteRenderers == null || cachedSpriteRenderers.Length == 0) return;

        int baseOrder = currentBaseSortingOrder;
        int stride = Mathf.Max(2, documentOrderStride);
        int bandMax = zone == TableTrigger.TableZone.InspectDesk ? openDeskSortingMax : closedDeskSortingMax;
        int stampOrder = Mathf.Min(bandMax, baseOrder + stride + 1);

        for (int i = 0; i < cachedSpriteRenderers.Length; i++)
        {
            SpriteRenderer sr = cachedSpriteRenderers[i];
            if (sr == null) continue;

            if (sr.GetComponentInParent<StampMark>() != null)
            {
                sr.sortingOrder = stampOrder;
                continue;
            }

            // Convention: SpriteRenderer on the state root (closedState/openedState) is the "document base".
            // TMP is expected at base+1; other sprites are "view" and render above TMP at base+stride.
            bool isStateRoot =
                (closedState != null && sr.gameObject == closedState) ||
                (openedState != null && sr.gameObject == openedState);

            sr.sortingOrder = isStateRoot ? baseOrder : baseOrder + stride;
        }

        if (syncWorldTmpSortingOrder)
        {
            TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < tmps.Length; i++)
            {
                TMP_Text tmp = tmps[i];
                if (tmp == null) continue;
                if (tmp.GetComponentInParent<StampMark>() != null)
                    continue;
                Renderer r = tmp.GetComponent<Renderer>();
                if (r != null)
                    r.sortingOrder = baseOrder + 1;
            }
        }
    }

    private bool CanStartDragFromPoint(Vector2 worldPoint)
    {
        if (activeDragOwner != null) return false;

        // Find topmost DocumentController at cursor by its current base sorting order.
        int hitCount = Physics2D.OverlapPointNonAlloc(worldPoint, overlapPointHits);
        DocumentController best = null;
        int bestOrder = int.MinValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D c = overlapPointHits[i];
            if (c == null) continue;
            DocumentController dc = c.GetComponentInParent<DocumentController>();
            if (dc == null) continue;

            int order = dc.currentBaseSortingOrder;
            if (best == null || order > bestOrder ||
                (order == bestOrder && dc.touchOrder > best.touchOrder))
            {
                best = dc;
                bestOrder = order;
            }
        }

        return best == this;
    }

    /// <summary>Sorting orders reserved per document: base, TMP +1, view +stride, stamp +stride+1 (clamped to band max).</summary>
    private int PackWidth()
    {
        int stride = Mathf.Max(2, documentOrderStride);
        return stride + 2;
    }

    /// <summary>Call after adding/removing renderers under a document (e.g. StampMark) so sorting updates immediately.</summary>
    public static void RebuildAllDocumentStacks() => RebuildAllStacks();

    private static void RebuildAllStacks()
    {
        for (int i = s_instances.Count - 1; i >= 0; i--)
        {
            if (s_instances[i] == null)
                s_instances.RemoveAt(i);
        }

        var inspect = new List<DocumentController>();
        var closed = new List<DocumentController>();

        for (int i = 0; i < s_instances.Count; i++)
        {
            DocumentController d = s_instances[i];
            if (d == null || !d.isActiveAndEnabled) continue;
            if (d.currentZone == TableTrigger.TableZone.InspectDesk)
                inspect.Add(d);
            else
                closed.Add(d);
        }

        int oMin = 1, oMax = 14, cMin = 16, cMax = 31;
        for (int i = 0; i < s_instances.Count; i++)
        {
            DocumentController r = s_instances[i];
            if (r == null || !r.isActiveAndEnabled) continue;
            oMin = r.openDeskSortingMin;
            oMax = r.openDeskSortingMax;
            cMin = r.closedDeskSortingMin;
            cMax = r.closedDeskSortingMax;
            break;
        }

        AssignBasesForZone(inspect, oMin, oMax);
        AssignBasesForZone(closed, cMin, cMax);

        for (int i = 0; i < s_instances.Count; i++)
        {
            DocumentController d = s_instances[i];
            if (d == null || !d.isActiveAndEnabled) continue;
            d.ApplySortingOrderForZone(d.currentZone);
        }
    }

    private static void AssignBasesForZone(List<DocumentController> group, int rangeMin, int rangeMax)
    {
        if (group.Count == 0) return;

        int rMin = Mathf.Min(rangeMin, rangeMax);
        int rMax = Mathf.Max(rangeMin, rangeMax);

        group.Sort((a, b) => b.touchOrder.CompareTo(a.touchOrder));

        int stride0 = Mathf.Max(2, group[0].documentOrderStride);
        // Highest layer used by a doc is base + stride + 1 (stamp), must stay within rMax.
        int cursor = rMax - stride0 - 1;
        for (int i = 0; i < group.Count; i++)
        {
            DocumentController d = group[i];
            int stride = Mathf.Max(2, d.documentOrderStride);
            int maxBase = rMax - stride - 1;
            if (cursor > maxBase)
                cursor = maxBase;
            if (cursor < rMin)
                cursor = rMin;
            d.currentBaseSortingOrder = cursor;
            if (i < group.Count - 1)
                cursor -= d.PackWidth();
        }
    }
}
