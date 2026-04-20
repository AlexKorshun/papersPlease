using UnityEngine;

[CreateAssetMenu(menuName = "AetherGate/DaySystem/Document Type", fileName = "DocType_")]
public class DocumentTypeSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id = "doc_id";
    [SerializeField] private string displayName = "Document";

    public string Id => id;
    public string DisplayName => displayName;
}

