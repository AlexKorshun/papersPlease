using TMPro;
using UnityEngine;

/// <summary>
/// Displays today's date from DayManager (dd.MM.yyyy).
/// </summary>
public class DayDateUI : MonoBehaviour
{
    [SerializeField] private DayManager dayManager;
    [SerializeField] private TMP_Text text;
    [SerializeField] private string prefix = "";

    private void Reset()
    {
        text = GetComponent<TMP_Text>();
    }

    private void Awake()
    {
        if (text == null)
            text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (text == null) return;
        if (dayManager == null)
            dayManager = FindFirstObjectByType<DayManager>();

        string date = dayManager != null ? dayManager.CurrentDateString : "--.--.----";
        text.text = string.IsNullOrEmpty(prefix) ? date : (prefix + date);
    }
}

