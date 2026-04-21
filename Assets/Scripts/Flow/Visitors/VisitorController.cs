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

