using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisitorSpawner : MonoBehaviour
{
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

    private Coroutine running;

    public void StartDay()
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(RunDay());
    }

    private IEnumerator RunDay()
    {
        if (dayManager == null || visitorPrefab == null)
            yield break;

        var docsBuffer = new List<DayManager.RolledDocument>(8);

        for (int i = 0; i < visitorsPerDay; i++)
        {
            PersonArchetypeSO archetype = dayManager.RollArchetypeForVisitor(i);
            VisitorController visitor = SpawnVisitor(archetype);

            dayManager.RollDocumentsForVisitor(archetype, i, docsBuffer);
            SpawnDocumentsForVisitor(visitor, docsBuffer);

            if (secondsBetweenVisitors > 0f)
                yield return new WaitForSeconds(secondsBetweenVisitors);
            else
                yield return null;
        }

        running = null;
    }

    private VisitorController SpawnVisitor(PersonArchetypeSO archetype)
    {
        Vector3 pos = visitorSpawnPoint != null ? visitorSpawnPoint.position : transform.position;
        VisitorController v = Instantiate(visitorPrefab, pos, Quaternion.identity);
        v.Initialize(archetype);
        return v;
    }

    private void SpawnDocumentsForVisitor(VisitorController visitor, List<DayManager.RolledDocument> docs)
    {
        if (visitor == null || docs == null) return;

        Vector3 basePos = documentSpawnPoint != null ? documentSpawnPoint.position : transform.position;
        for (int i = 0; i < docs.Count; i++)
        {
            DocumentTypeSO type = docs[i].DocumentType;
            GameObject prefab = documentPrefabs != null ? documentPrefabs.GetPrefab(type) : null;
            if (prefab == null) continue;

            Vector3 p = basePos + new Vector3(0.12f * i, -0.06f * i, 0f);
            GameObject go = Instantiate(prefab, p, Quaternion.identity);

            // If it is a passport document, pass forged flag (extend with more doc types later).
            PassportDocument passport = go.GetComponent<PassportDocument>();
            if (passport != null)
                passport.Initialize(type, docs[i].IsForged, GeneratePassportData(visitor, i, docs[i].IsForged));

            MonoBehaviour mb = go.GetComponent<MonoBehaviour>();
            if (mb != null)
                visitor.AddDocumentComponent(mb);
        }
    }

    private static PassportData GeneratePassportData(VisitorController visitor, int docIndex, bool forged)
    {
        // Minimal placeholder generator so you can see fields populate.
        // Later we'll swap this to a proper data generator + forgery rules per-day.
        string[] first = { "Igor", "Nina", "Viktor", "Alina", "Sergey", "Mila" };
        string[] last = { "Volkov", "Petrova", "Sokolov", "Morozov", "Kuznetsova", "Smirnov" };
        string[] nat = { "Arstotzka", "Kolechia", "Impor", "Antegria" };

        int seed = (visitor != null && visitor.Archetype != null ? visitor.Archetype.Id.GetHashCode() : 12345);
        int x = seed ^ (docIndex * 1103515245);
        unchecked { x = x * 1664525 + 1013904223; }

        string fullName = $"{first[Mathf.Abs(x) % first.Length]} {last[Mathf.Abs(x / 7) % last.Length]}";
        string passportNumber = $"AG-{Mathf.Abs(x / 13) % 900000 + 100000}";
        string nationality = nat[Mathf.Abs(x / 29) % nat.Length];
        string expiry = $"19{(Mathf.Abs(x / 31) % 10) + 80}-0{(Mathf.Abs(x / 37) % 9) + 1}-1{Mathf.Abs(x / 41) % 9}";

        if (forged)
        {
            // Simple forgery: mismatch nationality or number.
            nationality = nat[(Mathf.Abs(x / 3) + 1) % nat.Length];
            passportNumber = $"AG-{Mathf.Abs(x / 5) % 900000 + 100000}";
        }

        return new PassportData
        {
            FullName = fullName,
            PassportNumber = passportNumber,
            Nationality = nationality,
            ExpiryDate = expiry,
            Photo = null,
        };
    }
}

