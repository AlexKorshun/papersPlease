using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds PassportData to visuals on the passport prefab.
/// Works with Unity UI Text / Image, legacy TextMesh, and TextMeshPro (via reflection).
/// </summary>
public class PassportView : MonoBehaviour
{
    [Header("Text targets (UI Text, TextMesh, TMP_Text)")]
    [SerializeField] private Component fullNameText;
    [SerializeField] private Component passportNumberText;
    [SerializeField] private Component nationalityText;
    [SerializeField] private Component expiryDateText;

    [Header("Photo targets (UI Image or SpriteRenderer)")]
    [SerializeField] private Image photoImage;
    [SerializeField] private SpriteRenderer photoSpriteRenderer;

    [Header("Optional forged indicator")]
    [SerializeField] private GameObject forgedIndicator;

    public void Apply(in PassportData data, bool isForged)
    {
        SetText(fullNameText, data.FullName);
        SetText(passportNumberText, data.PassportNumber);
        SetText(nationalityText, data.Nationality);
        SetText(expiryDateText, data.ExpiryDate);

        if (photoImage != null)
            photoImage.sprite = data.Photo;
        if (photoSpriteRenderer != null)
            photoSpriteRenderer.sprite = data.Photo;

        if (forgedIndicator != null)
            forgedIndicator.SetActive(isForged);
    }

    private static void SetText(Component target, string value)
    {
        if (target == null) return;
        value ??= string.Empty;

        // UnityEngine.UI.Text
        if (target is Text uiText)
        {
            uiText.text = value;
            return;
        }

        // Legacy TextMesh
        if (target is TextMesh textMesh)
        {
            textMesh.text = value;
            return;
        }

        // TextMeshPro (TMP_Text) via reflection: property "text"
        Type t = target.GetType();
        PropertyInfo p = t.GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
        if (p != null && p.PropertyType == typeof(string) && p.CanWrite)
        {
            p.SetValue(target, value);
        }
    }
}

