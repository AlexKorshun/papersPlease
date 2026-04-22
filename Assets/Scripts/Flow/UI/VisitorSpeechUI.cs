using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Simple scene-level UI for visitor speech lines.
/// Place this on a UI object in the scene (not in the visitor prefab).
/// </summary>
public class VisitorSpeechUI : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Behavior")]
    [Min(0f)]
    [SerializeField] private float autoHideSeconds = 0f;
    [Min(0f)]
    [SerializeField] private float lineHoldSeconds = 1.6f;
    [Min(0f)]
    [SerializeField] private float gapSeconds = 0.15f;

    [Header("Prefab stack mode (Papers, Please style)")]
    [Tooltip("If set, each line spawns a new line ROOT prefab under Stack Root. Put TMP_Text (optionally with ContentSizeFitter) inside that prefab.")]
    [SerializeField] private GameObject lineRootPrefab;
    [SerializeField] private Transform stackRoot;
    [Min(0f)]
    [SerializeField] private float lineLifetimeSeconds = 2.0f;

    private Coroutine sequenceRoutine;

    private void Reset()
    {
        text = GetComponentInChildren<TMP_Text>(true);
        canvasGroup = GetComponentInChildren<CanvasGroup>(true);
        if (stackRoot == null)
            stackRoot = transform;
    }

    private void Awake()
    {
        if (text == null)
            text = GetComponentInChildren<TMP_Text>(true);
        if (canvasGroup == null)
            canvasGroup = GetComponentInChildren<CanvasGroup>(true);
        if (stackRoot == null)
            stackRoot = transform;
    }

    public void Show(string line)
    {
        StopSequence();
        if (text == null) return;

        text.text = line ?? string.Empty;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
        {
            text.gameObject.SetActive(true);
        }

        CancelInvoke(nameof(Hide));
        if (autoHideSeconds > 1e-4f)
            Invoke(nameof(Hide), autoHideSeconds);
    }

    public void Hide()
    {
        StopSequence();
        ClearSpawnedLines();
        CancelInvoke(nameof(Hide));

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else if (text != null)
        {
            text.gameObject.SetActive(false);
        }
    }

    public void PlaySequence(IReadOnlyList<string> lines)
    {
        StopSequence();
        ClearSpawnedLines();
        if (lines == null || lines.Count == 0) return;
        if (lineRootPrefab != null && stackRoot != null)
            sequenceRoutine = StartCoroutine(StackSequenceRoutine(lines));
        else
            sequenceRoutine = StartCoroutine(SequenceRoutine(lines));
    }

    private void StopSequence()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }
    }

    public void ClearSpawnedLines()
    {
        if (stackRoot == null) return;
        for (int i = stackRoot.childCount - 1; i >= 0; i--)
        {
            Transform c = stackRoot.GetChild(i);
            if (c != null)
                Destroy(c.gameObject);
        }
    }

    private IEnumerator SequenceRoutine(IReadOnlyList<string> lines)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            Show(lines[i]);
            float hold = Mathf.Max(0.01f, autoHideSeconds > 1e-4f ? autoHideSeconds : lineHoldSeconds);
            yield return new WaitForSeconds(hold);
            Hide();
            if (gapSeconds > 1e-4f && i < lines.Count - 1)
                yield return new WaitForSeconds(gapSeconds);
        }
        sequenceRoutine = null;
    }

    private IEnumerator StackSequenceRoutine(IReadOnlyList<string> lines)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            GameObject go = Instantiate(lineRootPrefab, stackRoot);
            TMP_Text t = go != null ? go.GetComponentInChildren<TMP_Text>(true) : null;
            if (t != null)
                t.text = lines[i] ?? string.Empty;

            SpeechLineAutoSize autoSize = go != null ? go.GetComponent<SpeechLineAutoSize>() : null;
            if (autoSize != null)
                autoSize.Refresh();

            // Ensure the root is visible if we're using CanvasGroup.
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            float hold = Mathf.Max(0.01f, lineLifetimeSeconds);
            yield return new WaitForSeconds(hold);

            if (go != null)
                Destroy(go);

            if (gapSeconds > 1e-4f && i < lines.Count - 1)
                yield return new WaitForSeconds(gapSeconds);
        }

        sequenceRoutine = null;
    }
}

