using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PermitView : MonoBehaviour
{
    [Header("Text targets (TMP_Text)")]
    [SerializeField] private TMP_Text firstNameText;
    [SerializeField] private TMP_Text lastNameText;
    [SerializeField] private TMP_Text validDateText;

    public void Apply(in PermitData data)
    {
        if (firstNameText != null) firstNameText.text = data.FirstName ?? string.Empty;
        if (lastNameText != null) lastNameText.text = data.LastName ?? string.Empty;
        if (validDateText != null) validDateText.text = data.ValidDate ?? string.Empty;
    }
}

