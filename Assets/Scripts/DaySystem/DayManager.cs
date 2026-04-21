using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime facade for the day rules (who appears, required docs, and forgery odds).
/// Hook your visitor spawner / document generator to this.
/// </summary>
public class DayManager : MonoBehaviour
{
    [Header("Day selection")]
    [Min(1)]
    [SerializeField] private int currentDayNumber = 1;

    [Header("Calendar")]
    [Tooltip("Base date for day 1 (dd.MM.yyyy). Default: 21.10.2077")]
    [SerializeField] private int baseDay = 21;
    [SerializeField] private int baseMonth = 10;
    [SerializeField] private int baseYear = 2077;

    [SerializeField] private List<DayConfigSO> dayConfigs = new();

    [Header("Randomness")]
    [Tooltip("If true, uses DayConfig.Seed to get reproducible rolls. If false, uses Unity's Random per call.")]
    [SerializeField] private bool deterministic = true;

    private DayConfigSO activeConfig;

    public int CurrentDayNumber => currentDayNumber;
    public DayConfigSO ActiveConfig => activeConfig;

    public DateTime CurrentDate
    {
        get
        {
            DateTime baseDate = new DateTime(baseYear, baseMonth, baseDay);
            return baseDate.AddDays(Mathf.Max(0, currentDayNumber - 1));
        }
    }

    public string CurrentDateString => CurrentDate.ToString("dd.MM.yyyy");

    private void Awake()
    {
        activeConfig = FindConfig(currentDayNumber);
        if (activeConfig == null && dayConfigs.Count > 0)
            activeConfig = dayConfigs[0];
    }

    public void SetDay(int dayNumber)
    {
        currentDayNumber = Mathf.Max(1, dayNumber);
        activeConfig = FindConfig(currentDayNumber);
    }

    public PersonArchetypeSO RollArchetypeForVisitor(int visitorIndex)
    {
        if (activeConfig == null || activeConfig.Archetypes.Count == 0)
            return null;

        float total = 0f;
        for (int i = 0; i < activeConfig.Archetypes.Count; i++)
            total += Mathf.Max(0f, activeConfig.Archetypes[i].SpawnWeight);

        if (total <= 1e-6f)
            return activeConfig.Archetypes[0].Archetype;

        float r = Next01(visitorIndex) * total;
        float acc = 0f;
        for (int i = 0; i < activeConfig.Archetypes.Count; i++)
        {
            acc += Mathf.Max(0f, activeConfig.Archetypes[i].SpawnWeight);
            if (r <= acc)
                return activeConfig.Archetypes[i].Archetype;
        }

        return activeConfig.Archetypes[activeConfig.Archetypes.Count - 1].Archetype;
    }

    public IReadOnlyList<DayConfigSO.DocumentRequirement> GetRequirements(PersonArchetypeSO archetype)
    {
        if (activeConfig == null || archetype == null)
            return Array.Empty<DayConfigSO.DocumentRequirement>();

        for (int i = 0; i < activeConfig.Archetypes.Count; i++)
        {
            var r = activeConfig.Archetypes[i];
            if (r.Archetype == archetype)
                return r.RequiredDocuments;
        }

        return Array.Empty<DayConfigSO.DocumentRequirement>();
    }

    public List<RolledDocument> RollDocumentsForVisitor(PersonArchetypeSO archetype, int visitorIndex, List<RolledDocument> buffer = null)
    {
        buffer ??= new List<RolledDocument>();
        buffer.Clear();

        var reqs = GetRequirements(archetype);
        for (int i = 0; i < reqs.Count; i++)
        {
            var req = reqs[i];
            if (req == null || req.DocumentType == null) continue;

            bool forged = Next01(visitorIndex * 997 + i * 101) < Mathf.Clamp01(req.ForgeryChance);
            buffer.Add(new RolledDocument(req.DocumentType, forged));
        }

        return buffer;
    }

    [Serializable]
    public struct RolledDocument
    {
        public DocumentTypeSO DocumentType;
        public bool IsForged;

        public RolledDocument(DocumentTypeSO doc, bool forged)
        {
            DocumentType = doc;
            IsForged = forged;
        }
    }

    private DayConfigSO FindConfig(int dayNumber)
    {
        for (int i = 0; i < dayConfigs.Count; i++)
        {
            if (dayConfigs[i] != null && dayConfigs[i].DayNumber == dayNumber)
                return dayConfigs[i];
        }
        return null;
    }

    private float Next01(int salt)
    {
        if (!deterministic || activeConfig == null)
            return UnityEngine.Random.value;

        // Stable per-day, per-visitor random without touching global UnityEngine.Random state.
        unchecked
        {
            int seed = activeConfig.Seed;
            int x = seed;
            x = (x * 73856093) ^ (currentDayNumber * 19349663);
            x = (x * 83492791) ^ salt;
            x ^= (x << 13);
            x ^= (x >> 17);
            x ^= (x << 5);
            uint u = (uint)x;
            return (u & 0x00FFFFFFu) / 16777216f;
        }
    }
}

