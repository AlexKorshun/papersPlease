using System.Collections.Generic;
using UnityEngine;

public class VisitorController : MonoBehaviour
{
    [SerializeField] private PersonArchetypeSO archetype;
    [SerializeField] private List<MonoBehaviour> documents = new(); // components implementing IDocumentInstance
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

    public IReadOnlyList<MonoBehaviour> DocumentComponents => documents;
}

