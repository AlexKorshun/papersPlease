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
    /// Spawns one visitor; documents appear after the visitor's enter animation (see VisitorController).
    /// </summary>
    public VisitorController SpawnVisitorForSession()
    {
        if (sessionVisitorIndex >= visitorsPerDay)
        {
            OnDayEnded?.Invoke();
            return null;
        }

        if (dayManager == null || visitorPrefab == null) return null;

        var docsBuffer = new List<DayManager.RolledDocument>(8);
        int i = sessionVisitorIndex++;

        PersonArchetypeSO archetype = dayManager.RollArchetypeForVisitor(i);
        VisitorController visitor = SpawnVisitor(archetype, i);
        if (visitor == null) return null;

        dayManager.RollDocumentsForVisitor(archetype, i, docsBuffer);
        EnsureFallbackDocuments(docsBuffer);

        bool hasForgedDoc = docsBuffer.Exists(d => d.IsForged);
        visitor.SetShouldBeAllowed(!hasForgedDoc);
        visitor.SetEmotionalState(VisitorProfileGenerator.GenerateEmotionalState(visitor.Profile, hasForgedDoc, i));

        visitor.BeginSpawnDocumentsAfterEnter(dayManager, documentPrefabs, documentSpawnPoint, CopyRolledDocuments(docsBuffer));

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
            if (visitor == null)
                continue;

            dayManager.RollDocumentsForVisitor(archetype, i, docsBuffer);
            EnsureFallbackDocuments(docsBuffer);

            bool hasForgedDoc = docsBuffer.Exists(d => d.IsForged);
            visitor.SetShouldBeAllowed(!hasForgedDoc);
            visitor.SetEmotionalState(VisitorProfileGenerator.GenerateEmotionalState(visitor.Profile, hasForgedDoc, i));

            GameFlowController.Instance?.SetActiveVisitor(visitor);

            visitor.BeginSpawnDocumentsAfterEnter(dayManager, documentPrefabs, documentSpawnPoint, CopyRolledDocuments(docsBuffer));

            // Ждём пока посетитель не уйдёт (уничтожится)
            yield return new WaitUntil(() => visitor == null);

            if (secondsBetweenVisitors > 0f)
                yield return new WaitForSeconds(secondsBetweenVisitors);
        }

        running = null;
        OnDayEnded?.Invoke();
    }

    private static List<DayManager.RolledDocument> CopyRolledDocuments(List<DayManager.RolledDocument> src)
    {
        var dst = new List<DayManager.RolledDocument>(src != null ? src.Count : 0);
        if (src == null) return dst;
        for (int i = 0; i < src.Count; i++)
            dst.Add(src[i]);
        return dst;
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

        // Never Instantiate(..., parent) with a Transform that can be "persistent" (prefab asset / invalid scene) — Unity warns and drops parent.
        VisitorController v = Instantiate(prefabToUse);

        Transform spawn = visitorSpawnPoint != null ? visitorSpawnPoint : transform;
        if (IsTransformInLoadedScene(spawn))
        {
            v.transform.SetParent(spawn, false);
            v.transform.localPosition = Vector3.zero;
            v.transform.localRotation = Quaternion.identity;
        }
        else
        {
            v.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        }

        VisitorProfile profile = VisitorProfileGenerator.Generate(archetype, visitorIndex);
        v.Initialize(archetype, profile);
        return v;
    }

    private static bool IsTransformInLoadedScene(Transform t)
    {
        if (t == null) return false;
        GameObject go = t.gameObject;
        return go.scene.IsValid() && go.scene.isLoaded;
    }
}

