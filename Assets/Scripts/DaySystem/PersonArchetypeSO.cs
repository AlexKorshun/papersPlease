using UnityEngine;

[CreateAssetMenu(menuName = "AetherGate/DaySystem/Person Archetype", fileName = "Person_")]
public class PersonArchetypeSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id = "person_id";
    [SerializeField] private string displayName = "Person";

    public string Id => id;
    public string DisplayName => displayName;
}

