using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds PassportData to visuals on the passport prefab.
/// Works with TMP_Text plus optional UI Image / SpriteRenderer for the photo.
/// </summary>
public class PassportView : MonoBehaviour
{
    [Header("Text targets (TMP_Text)")]
    [SerializeField] private TMP_Text firstNameText;
    [SerializeField] private TMP_Text lastNameText;
    [SerializeField] private TMP_Text birthDateText;
    [SerializeField] private TMP_Text birthPlaceText;

    [Header("Photo targets (UI Image or SpriteRenderer)")]
    [SerializeField] private Image photoImage;
    [SerializeField] private SpriteRenderer photoSpriteRenderer;

    [Header("Optional forged indicator")]
    [SerializeField] private GameObject forgedIndicator;

    public void Apply(in PassportData data, bool isForged)
    {
        if (firstNameText != null) firstNameText.text = data.FirstName ?? string.Empty;
        if (lastNameText != null) lastNameText.text = data.LastName ?? string.Empty;
        if (birthDateText != null) birthDateText.text = data.BirthDate ?? string.Empty;
        if (birthPlaceText != null) birthPlaceText.text = data.BirthPlace ?? string.Empty;

        if (photoImage != null)
            photoImage.sprite = data.Photo;
        if (photoSpriteRenderer != null)
            photoSpriteRenderer.sprite = data.Photo;

        if (forgedIndicator != null)
            forgedIndicator.SetActive(isForged);
    }
}

