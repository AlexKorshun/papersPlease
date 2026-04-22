using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AetherGate/DaySystem/Person Archetype", fileName = "Person_")]
public class PersonArchetypeSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id = "person_id";
    [SerializeField] private string displayName = "Person";

    [Header("Visuals")]
    [SerializeField] private VisitorController prefab;

    [Header("Dialogues")]
    [Tooltip("Replicas when visitor has valid documents. One variant picked at random.")]
    [SerializeField] private List<DialogueVariant> enterDialoguesLegit = new();
    [Tooltip("Replicas when visitor has forged documents. Falls back to Legit list if empty.")]
    [SerializeField] private List<DialogueVariant> enterDialoguesForged = new();

    public string Id => id;
    public string DisplayName => displayName;
    public VisitorController Prefab => prefab;
    public IReadOnlyList<DialogueVariant> EnterDialoguesLegit => enterDialoguesLegit;
    public IReadOnlyList<DialogueVariant> EnterDialoguesForged => enterDialoguesForged;
}

