using TMPro;
using UnityEngine;

/// <summary>
/// Attach to a document root. Allows handing the document to the delivery zone by dropping it there.
/// </summary>
public class DocumentDeliverable : MonoBehaviour
{
    [Header("Rules")]
    [Tooltip("If true and the document implements IStampable, it must have Decision != None to be deliverable.")]
    [SerializeField] private bool requireStampIfStampable = true;

    [Tooltip("If true, document is hidden after delivery.")]
    [SerializeField] private bool hideAfterDelivery = true;

    [Tooltip("Optional: if hiding, delay in seconds (lets you play a small animation).")]
    [Min(0f)]
    [SerializeField] private float hideDelay = 0f;

    [Header("Hint (TMP)")]
    [Tooltip("Optional TMP element that appears above the document when it can be delivered.")]
    [SerializeField] private TMP_Text deliverHintText;

    [Header("Detection")]
    [Tooltip("Optional: pick a specific collider to use for delivery zone overlap checks (recommended).")]
    [SerializeField] private Collider2D deliveryProbeCollider;

    [Tooltip("Overlap box size used to detect delivery zone at drop point.")]
    [SerializeField] private Vector2 deliveryZoneCheckSize = new Vector2(0.3f, 0.3f);

    [SerializeField] private LayerMask deliveryZoneLayers = ~0;

    public bool IsDelivered { get; private set; }
    public bool RequiresStampIfStampable => requireStampIfStampable;

    private IStampable stampable;
    private Collider2D[] cachedColliders;
    private Collider2D[] overlapResults;
    private bool hintVisible;

    private void Awake()
    {
        stampable = GetComponent<IStampable>();
        cachedColliders = GetComponentsInChildren<Collider2D>(true);
        overlapResults = new Collider2D[16];

        if (deliverHintText != null)
        {
            deliverHintText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        UpdateDeliverHint();
    }

    public void TryDeliverOnDrop()
    {
        if (IsDelivered) return;

        GameFlowController flow = GameFlowController.Instance;
        if (flow == null || flow.CurrentState != GameState.VisitorPresent)
            return;

        // Global gate: do not allow handing over anything until all required documents are stamped.
        if (!flow.AreAllRequiredStampsPresent())
            return;

        if (requireStampIfStampable && stampable != null && stampable.Decision == StampDecision.None)
            return;

        if (!TryFindDeliveryZone(out DocumentDeliveryZone zone, out string reason))
            return;

        DeliverToZone(zone);
    }

    private void UpdateDeliverHint()
    {
        if (deliverHintText == null) return;

        if (IsDelivered)
        {
            if (hintVisible) SetHintVisible(false);
            return;
        }

        GameFlowController flow = GameFlowController.Instance;
        bool visitorPresent = flow != null && flow.CurrentState == GameState.VisitorPresent;
        if (!visitorPresent)
        {
            if (hintVisible) SetHintVisible(false);
            return;
        }

        if (flow != null && !flow.AreAllRequiredStampsPresent())
        {
            if (hintVisible) SetHintVisible(false);
            return;
        }

        if (requireStampIfStampable && stampable != null && stampable.Decision == StampDecision.None)
        {
            if (hintVisible) SetHintVisible(false);
            return;
        }

        bool overZone = TryFindDeliveryZone(out _, out _);

        SetHintVisible(overZone);
    }

    private void SetHintVisible(bool visible)
    {
        if (hintVisible == visible) return;
        hintVisible = visible;
        deliverHintText.gameObject.SetActive(visible);
    }

    private bool TryFindDeliveryZone(out DocumentDeliveryZone zone, out string reason)
    {
        zone = null;
        reason = string.Empty;

        // Preferred: use a specific collider (shape-accurate).
        Collider2D probe = deliveryProbeCollider != null ? deliveryProbeCollider : null;

        if (probe != null)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useLayerMask = true;
            filter.layerMask = deliveryZoneLayers;
            filter.useTriggers = true;

            int count = probe.OverlapCollider(filter, overlapResults);
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = overlapResults[i];
                if (hit == null) continue;
                zone = hit.GetComponentInParent<DocumentDeliveryZone>();
                if (zone != null)
                    return true;
            }

            reason = $"probeCollider='{probe.name}' has no overlap with DocumentDeliveryZone (layers={deliveryZoneLayers.value})";
            return false;
        }

        // Fallback: overlap box around root position.
        Vector2 p = new Vector2(transform.position.x, transform.position.y);
        Collider2D hitBox = Physics2D.OverlapBox(p, deliveryZoneCheckSize, 0f, deliveryZoneLayers);
        if (hitBox == null)
        {
            reason = $"no zone hit (fallback OverlapBox) pos={p} box={deliveryZoneCheckSize} layers={deliveryZoneLayers.value}";
            return false;
        }

        zone = hitBox.GetComponentInParent<DocumentDeliveryZone>();
        if (zone == null)
        {
            reason = $"hit '{hitBox.name}' but no DocumentDeliveryZone in parents (fallback OverlapBox)";
            return false;
        }

        return true;
    }

    private void DeliverToZone(DocumentDeliveryZone zone)
    {
        IsDelivered = true;

        if (deliverHintText != null)
            deliverHintText.gameObject.SetActive(false);

        if (zone != null && zone.DeliveredPoint != null)
            transform.position = zone.DeliveredPoint.position;

        DisableColliders();

        GameFlowController flow = GameFlowController.Instance;
        if (flow != null)
            flow.NotifyDocumentDelivered(this);

        if (hideAfterDelivery)
        {
            if (hideDelay <= 1e-6f)
                gameObject.SetActive(false);
            else
                Invoke(nameof(HideNow), hideDelay);
        }
    }

    private void HideNow()
    {
        gameObject.SetActive(false);
    }

    private void DisableColliders()
    {
        if (cachedColliders == null) return;
        for (int i = 0; i < cachedColliders.Length; i++)
        {
            if (cachedColliders[i] != null)
                cachedColliders[i].enabled = false;
        }
    }
}

