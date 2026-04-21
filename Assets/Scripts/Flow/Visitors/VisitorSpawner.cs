using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisitorSpawner : MonoBehaviour
{
    public event Action OnDayEnded;
    [Header("Dependencies")]
    [SerializeField] private DayManager dayManager;
    [SerializeField] private DocumentPrefabRegistrySO documentPrefabs;

    [Header("Prefabs")]
    [SerializeField] private VisitorController visitorPrefab;

    [Header("Spawn points")]
    [SerializeField] private Transform visitorSpawnPoint;
    [SerializeField] private Transform documentSpawnPoint;

    [Header("Flow")]
    [Min(0)]
    [SerializeField] private int visitorsPerDay = 10;

    [Min(0f)]
    [SerializeField] private float secondsBetweenVisitors = 2f;

    [Header("Fallback (safety)")]
    [Tooltip("If DayConfig has no RequiredDocuments for the rolled archetype, we can still spawn at least one document.")]
    [SerializeField] private bool ensureAtLeastOneDocument = true;

    [Tooltip("Used when ensureAtLeastOneDocument is enabled and the day rules roll 0 documents. Typically set this to Passport document type.")]
    [SerializeField] private DocumentTypeSO fallbackDocumentType;

    private Coroutine running;
    private int sessionVisitorIndex;

    /// <summary>
    /// Spawns exactly one visitor + its documents immediately (no coroutine).
    /// Use this when GameFlowController drives the state machine (VisitorPresent / NextVisitor),
    /// but you still want all documents to come from the same DayManager rules.
    /// </summary>
    public VisitorController SpawnVisitorForSession()
    {
        if (dayManager == null || visitorPrefab == null) return null;

        var docsBuffer = new List<DayManager.RolledDocument>(8);
        int i = sessionVisitorIndex++;

        PersonArchetypeSO archetype = dayManager.RollArchetypeForVisitor(i);
        VisitorController visitor = SpawnVisitor(archetype, i);

        dayManager.RollDocumentsForVisitor(archetype, i, docsBuffer);
        EnsureFallbackDocuments(docsBuffer);
        visitor.SpawnDocuments(dayManager, documentPrefabs, documentSpawnPoint, docsBuffer);

        bool hasForgedDoc = docsBuffer.Exists(d => d.IsForged);
        visitor.SetShouldBeAllowed(!hasForgedDoc);

        return visitor;
    }

    public void StartDay()
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(RunDay());
    }

    public void StopDay()
    {
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }
        OnDayEnded?.Invoke();
    }

    private IEnumerator RunDay()
    {
        if (dayManager == null || visitorPrefab == null)
            yield break;

        var docsBuffer = new List<DayManager.RolledDocument>(8);

        for (int i = 0; i < visitorsPerDay; i++)
        {
            PersonArchetypeSO archetype = dayManager.RollArchetypeForVisitor(i);
            VisitorController visitor = SpawnVisitor(archetype, i);

            dayManager.RollDocumentsForVisitor(archetype, i, docsBuffer);
            EnsureFallbackDocuments(docsBuffer);
            visitor.SpawnDocuments(dayManager, documentPrefabs, documentSpawnPoint, docsBuffer);

            bool hasForgedDoc = docsBuffer.Exists(d => d.IsForged);
            visitor.SetShouldBeAllowed(!hasForgedDoc);

            // Ждём пока посетитель не уйдёт (уничтожится)
            yield return new WaitUntil(() => visitor == null);

            if (secondsBetweenVisitors > 0f)
                yield return new WaitForSeconds(secondsBetweenVisitors);
        }

        running = null;
        OnDayEnded?.Invoke();
    }

    private void EnsureFallbackDocuments(List<DayManager.RolledDocument> docsBuffer)
    {
        if (!ensureAtLeastOneDocument) return;
        if (docsBuffer == null) return;
        if (docsBuffer.Count > 0) return;
        if (fallbackDocumentType == null) return;

        docsBuffer.Add(new DayManager.RolledDocument(fallbackDocumentType, forged: false));
    }

    private VisitorController SpawnVisitor(PersonArchetypeSO archetype, int visitorIndex)
    {
        VisitorController prefabToUse = (archetype != null && archetype.Prefab != null)
            ? archetype.Prefab
            : visitorPrefab;

        if (prefabToUse == null) return null;

        Vector3 pos = visitorSpawnPoint != null ? visitorSpawnPoint.position : transform.position;
        VisitorController v = Instantiate(prefabToUse, pos, Quaternion.identity);
        VisitorProfile profile = VisitorProfileGenerator.Generate(archetype, visitorIndex);
        v.Initialize(archetype, profile);
        return v;
    }
}

