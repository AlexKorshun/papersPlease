using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AetherGate/Flow/Document Prefab Registry", fileName = "DocumentPrefabRegistry")]
public class DocumentPrefabRegistrySO : ScriptableObject
{
    [SerializeField] private List<Entry> entries = new();

    [Serializable]
    public class Entry
    {
        public DocumentTypeSO documentType;
        public GameObject prefab;
    }

    public GameObject GetPrefab(DocumentTypeSO type)
    {
        if (type == null) return null;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].documentType == type)
                return entries[i].prefab;
        }
        return null;
    }
}

