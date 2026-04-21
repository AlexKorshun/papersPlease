using UnityEngine;

public class PassportDocument : MonoBehaviour, IStampable
{
    [Header("Identity")]
    [SerializeField] private DocumentTypeSO documentType;

    [Header("Runtime")]
    [SerializeField] private bool isForged;
    [SerializeField] private StampDecision decision = StampDecision.None;
    [SerializeField] private PassportData data;

    [Header("View")]
    [SerializeField] private PassportView view;

    [Header("Stamp visual")]
    [SerializeField] private StampMark stampMarkPrefab;
    [SerializeField] private Transform stampMarksRoot;
    [SerializeField] private Transform stampAnchor;
    [Tooltip("Random offset around the stamp anchor (world units).")]
    [SerializeField] private Vector2 stampAnchorRandomOffset = new Vector2(0.02f, 0.02f);
    [Tooltip("If true, replaces the previous stamp mark instead of creating many.")]
    [SerializeField] private bool singleStampOnly = true;

    private StampMark currentStamp;

    public DocumentTypeSO DocumentType => documentType;
    public bool IsForged => isForged;

    public StampDecision Decision => decision;

    public void Initialize(DocumentTypeSO type, bool forged, PassportData passportData)
    {
        documentType = type;
        isForged = forged;
        decision = StampDecision.None;
        data = passportData;

        if (view == null)
            view = GetComponentInChildren<PassportView>(true);
        if (view != null)
            view.Apply(data, isForged);
    }

    public void ApplyDecision(StampDecision d, Vector2 worldPoint)
    {
        decision = d;

        if (stampMarkPrefab == null)
            return;

        Transform parent = stampMarksRoot != null ? stampMarksRoot : transform;

        if (singleStampOnly && currentStamp != null)
            Destroy(currentStamp.gameObject);

        StampMark mark = Instantiate(stampMarkPrefab, parent);
        Vector2 p = stampAnchor != null ? (Vector2)stampAnchor.position : worldPoint;
        p += new Vector2(
            Random.Range(-stampAnchorRandomOffset.x, stampAnchorRandomOffset.x),
            Random.Range(-stampAnchorRandomOffset.y, stampAnchorRandomOffset.y));
        mark.transform.position = new Vector3(p.x, p.y, mark.transform.position.z);
        mark.Configure(d == StampDecision.Approved);

        currentStamp = mark;

        DocumentController.RebuildAllDocumentStacks();
    }
}

