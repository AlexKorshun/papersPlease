using System.Collections.Generic;
using UnityEngine;

public class VisitorController : MonoBehaviour
{
    [SerializeField] private PersonArchetypeSO archetype;
    [SerializeField] private List<MonoBehaviour> documents = new();
    [SerializeField] private VisitorProfile profile;

    public PersonArchetypeSO Archetype => archetype;
    public VisitorProfile Profile => profile;

    public void Initialize(PersonArchetypeSO personArchetype, VisitorProfile visitorProfile)
    {
        archetype = personArchetype;
        profile = visitorProfile;
        documents.Clear();
    }

    public void AddDocumentComponent(MonoBehaviour docComponent)
    {
        if (docComponent == null) return;
        documents.Add(docComponent);
    }

    /// <summary>
    /// Spawns and initializes rolled documents for this visitor, using the shared VisitorProfile
    /// so all documents stay consistent (name, birth date, photo, etc).
    /// </summary>
    public void SpawnDocuments(
        DayManager dayManager,
        DocumentPrefabRegistrySO documentPrefabs,
        Transform documentSpawnPoint,
        List<DayManager.RolledDocument> rolledDocuments)
    {
        if (rolledDocuments == null || rolledDocuments.Count == 0) return;
        if (documentPrefabs == null) return;

        string today = dayManager != null ? dayManager.CurrentDateString : "21.10.2077";
        Vector3 basePos = documentSpawnPoint != null ? documentSpawnPoint.position : transform.position;

        for (int i = 0; i < rolledDocuments.Count; i++)
        {
            DayManager.RolledDocument rolled = rolledDocuments[i];
            DocumentTypeSO type = rolled.DocumentType;
            GameObject prefab = documentPrefabs.GetPrefab(type);
            if (prefab == null) continue;

            Vector3 p = basePos + new Vector3(0.12f * i, -0.06f * i, 0f);
            GameObject go = Instantiate(prefab, p, Quaternion.identity);

            PassportDocument passport = go.GetComponent<PassportDocument>();
            if (passport != null)
                passport.Initialize(type, rolled.IsForged, VisitorProfileGenerator.BuildPassportData(Profile, i, rolled.IsForged));

            EntryPermitDocument permit = go.GetComponent<EntryPermitDocument>();
            if (permit != null)
                permit.Initialize(type, rolled.IsForged, VisitorProfileGenerator.BuildPermitData(Profile, today, i, rolled.IsForged));

            MonoBehaviour mb = go.GetComponent<MonoBehaviour>();
            if (mb != null)
                AddDocumentComponent(mb);

            if (GameFlowController.Instance != null)
                GameFlowController.Instance.RegisterVisitorSessionDocument(go);
        }
    }

    public void SetShouldBeAllowed(bool value)
    {
        profile.ShouldBeAllowed = value;
    }

    public IReadOnlyList<MonoBehaviour> DocumentComponents => documents;

    private void OnEnable()
    {
        GameFlowController.OnStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameFlowController.OnStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState prev, GameState next)
    {
        if (next == GameState.Decision)
        {
            bool playerApproved = GameFlowController.Instance.LastDecisionApproved;
            bool correct = playerApproved == profile.ShouldBeAllowed;

            if (ScoreManager.Instance != null)
            {
                if (correct) ScoreManager.Instance.AddScore();
                else         ScoreManager.Instance.AddPenalty();
            }
        }

        if (next == GameState.NextVisitor)
            Destroy(gameObject);
    }
}

