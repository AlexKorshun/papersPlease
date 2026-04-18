using UnityEngine;

public class DocumentController : MonoBehaviour
{
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
    private int sortingOrderInspectDesk = 5;

    [SerializeField]
    private int sortingOrderClosedDesk = 100;

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
        ApplySortingOrderForZone(currentZone);
    }

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
        if (!closedState.activeSelf && !openedState.activeSelf) return;

        Collider2D currentCollider = closedState.activeSelf ? closedCollider : openedCollider;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;

            if (currentCollider != null && currentCollider.OverlapPoint(mouseWorldPos))
                OnMouseDownInternal(mouseWorldPos);
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
        isDragging = true;
        SetRigidbodyForDrag(true);

        grabLocalPoint = transform.InverseTransformPoint(mouseWorldPos);
        if (currentZone == TableTrigger.TableZone.ClosedDesk)
            savedGrabLocalPointFromClosedDesk = grabLocalPoint;

        if (currentZone == TableTrigger.TableZone.InspectDesk)
            SetState(isOpened: true);
        else
            SetState(isOpened: false);
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
        RefreshDeskZoneFlagsFromPhysics(useCursorProbe: false);

        SetRigidbodyForDrag(false);

        if (!dropClosed && !dropInspect)
        {
            SetState(isOpened: false);
            currentZone = TableTrigger.TableZone.ClosedDesk;
            ApplySortingOrderForZone(currentZone);
            ClampDocumentToScreenBounds();
            return;
        }

        if (dropInspect)
        {
            currentZone = TableTrigger.TableZone.InspectDesk;
            SetState(isOpened: true);
            ApplySortingOrderForZone(currentZone);
        }
        else
        {
            currentZone = TableTrigger.TableZone.ClosedDesk;
            SetState(isOpened: false);
            ApplySortingOrderForZone(currentZone);
        }

        ClampDocumentToScreenBounds();
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
        ApplySortingOrderForZone(currentZone);
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
            ApplySortingOrderForZone(currentZone);
            return;
        }

        if (target == TableTrigger.TableZone.ClosedDesk && currentZone == TableTrigger.TableZone.InspectDesk)
        {
            grabLocalPoint = savedGrabLocalPointFromClosedDesk;
            AlignLocalGrabPointToCursor(mouseWorldPos);
            SetState(isOpened: false);
            currentZone = TableTrigger.TableZone.ClosedDesk;
            ApplySortingOrderForZone(currentZone);
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
        if (cachedSpriteRenderers == null || cachedSpriteRenderers.Length == 0) return;

        int order = zone == TableTrigger.TableZone.InspectDesk ? sortingOrderInspectDesk : sortingOrderClosedDesk;
        for (int i = 0; i < cachedSpriteRenderers.Length; i++)
        {
            if (cachedSpriteRenderers[i] != null)
                cachedSpriteRenderers[i].sortingOrder = order;
        }
    }
}
