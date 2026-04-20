using UnityEngine;

public enum StampDecision
{
    None = 0,
    Approved = 1,
    Rejected = 2,
}

public interface IStampable
{
    StampDecision Decision { get; }
    void ApplyDecision(StampDecision decision, Vector2 worldPoint);
}

/// <summary>
/// Attach to the stamp head. Call Apply(int) from an Animation Event on the impact frame.
/// decision: 1 = Approved, 2 = Rejected.
/// </summary>
public class StampApply2D : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private Transform stampPoint;
    [Tooltip("World overlap box size. Set to something like (0.2, 0.2).")]
    [SerializeField] private Vector2 overlapBoxSize = new Vector2(0.2f, 0.2f);
    [Tooltip("If set, only colliders on these layers can be stamped.")]
    [SerializeField] private LayerMask stampableLayers = ~0;

    private IStampable currentStampable;

    private void Reset()
    {
        stampPoint = transform;
    }

    public void Apply(int decision)
    {
        StampDecision d = decision == 1 ? StampDecision.Approved :
                          decision == 2 ? StampDecision.Rejected :
                          StampDecision.None;
        if (d == StampDecision.None) return;

        Vector3 p3 = stampPoint != null ? stampPoint.position : transform.position;
        Vector2 p = new Vector2(p3.x, p3.y);

        IStampable target = ResolveStampableUnderPoint(p);
        if (target == null) return;

        target.ApplyDecision(d, p);
    }

    private IStampable ResolveStampableUnderPoint(Vector2 worldPoint)
    {
        Collider2D hit = Physics2D.OverlapBox(worldPoint, overlapBoxSize, 0f, stampableLayers);
        if (hit != null)
        {
            IStampable s = hit.GetComponentInParent<IStampable>();
            if (s != null) return s;
        }

        return currentStampable;
    }

    private void OnTriggerEnter2D(Collider2D other) => CacheStampable(other);
    private void OnTriggerStay2D(Collider2D other) => CacheStampable(other);

    private void OnTriggerExit2D(Collider2D other)
    {
        IStampable s = other.GetComponentInParent<IStampable>();
        if (s != null && s == currentStampable)
            currentStampable = null;
    }

    private void CacheStampable(Collider2D other)
    {
        if (other == null) return;
        IStampable s = other.GetComponentInParent<IStampable>();
        if (s != null)
            currentStampable = s;
    }
}

