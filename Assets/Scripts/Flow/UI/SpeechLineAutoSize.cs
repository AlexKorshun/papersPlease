using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the ROOT of a speech line prefab (the object that is a direct child of a LayoutGroup).
/// It updates the LayoutElement preferred size to match the TMP text preferred size,
/// so the LayoutGroup can size the row correctly (without using ContentSizeFitter on children).
/// </summary>
public class SpeechLineAutoSize : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private LayoutElement layoutElement;
    [SerializeField] private Vector2 padding = new Vector2(24f, 14f); // (x,y)

    private void Reset()
    {
        text = GetComponentInChildren<TMP_Text>(true);
        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();
    }

    private void Awake()
    {
        if (text == null)
            text = GetComponentInChildren<TMP_Text>(true);
        if (layoutElement == null)
            layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();
    }

    public void Refresh()
    {
        if (text == null || layoutElement == null) return;

        // Ensure layout values are up to date.
        text.ForceMeshUpdate();
        Vector2 pref = text.GetPreferredValues(text.text);

        layoutElement.preferredWidth = pref.x + Mathf.Max(0f, padding.x);
        layoutElement.preferredHeight = pref.y + Mathf.Max(0f, padding.y);
        // Prevent LayoutGroup from distributing extra space to this line.
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
    }
}

