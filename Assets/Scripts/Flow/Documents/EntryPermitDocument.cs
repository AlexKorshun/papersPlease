using UnityEngine;

/// <summary>
/// Entry permit: valid only if its ValidDate equals today's DayManager date.
/// </summary>
public class EntryPermitDocument : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private DocumentTypeSO documentType;

    [Header("Runtime")]
    [SerializeField] private bool isForged;
    [SerializeField] private PermitData data;

    [Header("View")]
    [SerializeField] private PermitView view;

    public DocumentTypeSO DocumentType => documentType;
    public bool IsForged => isForged;
    public PermitData Data => data;

    public void Initialize(DocumentTypeSO type, bool forged, PermitData permitData)
    {
        documentType = type;
        isForged = forged;
        data = permitData;

        if (view == null)
            view = GetComponentInChildren<PermitView>(true);
        if (view != null)
            view.Apply(data);
    }

    public bool IsValidForDate(string todayDate)
    {
        return !string.IsNullOrEmpty(todayDate) && data.ValidDate == todayDate;
    }
}

