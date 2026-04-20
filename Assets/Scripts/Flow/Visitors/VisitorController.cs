using System.Collections.Generic;
using UnityEngine;

public class VisitorController : MonoBehaviour
{
    [SerializeField] private PersonArchetypeSO archetype;
    [SerializeField] private List<MonoBehaviour> documents = new(); // components implementing IDocumentInstance

    public PersonArchetypeSO Archetype => archetype;

    public void Initialize(PersonArchetypeSO personArchetype)
    {
        archetype = personArchetype;
        documents.Clear();
    }

    public void AddDocumentComponent(MonoBehaviour docComponent)
    {
        if (docComponent == null) return;
        documents.Add(docComponent);
    }

    public IReadOnlyList<MonoBehaviour> DocumentComponents => documents;
}

