using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AetherGate/DaySystem/Day Config", fileName = "Day_")]
public class DayConfigSO : ScriptableObject
{
    [Header("Day identity")]
    [Min(1)]
    [SerializeField] private int dayNumber = 1;

    [Tooltip("Optional: used for deterministic rolls if you want reproducible days.")]
    [SerializeField] private int seed = 0;

    [Header("Who can appear today")]
    [SerializeField] private List<ArchetypeRule> archetypes = new();

    public int DayNumber => dayNumber;
    public int Seed => seed;
    public IReadOnlyList<ArchetypeRule> Archetypes => archetypes;

    [Serializable]
    public class ArchetypeRule
    {
        [SerializeField] private PersonArchetypeSO archetype;

        [Tooltip("Relative weight for selecting this archetype among others available today.")]
        [Min(0f)]
        [SerializeField] private float spawnWeight = 1f;

        [SerializeField] private List<DocumentRequirement> requiredDocuments = new();

        public PersonArchetypeSO Archetype => archetype;
        public float SpawnWeight => spawnWeight;
        public IReadOnlyList<DocumentRequirement> RequiredDocuments => requiredDocuments;
    }

    [Serializable]
    public class DocumentRequirement
    {
        [SerializeField] private DocumentTypeSO documentType;

        [Header("Forgery")]
        [Range(0f, 1f)]
        [SerializeField] private float forgeryChance = 0f;

        public DocumentTypeSO DocumentType => documentType;
        public float ForgeryChance => forgeryChance;
    }
}

