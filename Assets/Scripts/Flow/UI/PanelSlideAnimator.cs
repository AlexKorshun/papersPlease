using UnityEngine;

/// <summary>
/// Minimal helper for sliding a panel via Animator.
/// Hook Toggle() to your UI button onClick.
/// </summary>
public class PanelSlideAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Animator params")]
    [Tooltip("Bool parameter that represents 'opened' state.")]
    [SerializeField] private string openedBoolParam = "Opened";


    private bool isOpened = false;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        // ApplyStateToAnimator();
    }

    public void Toggle()
    {
        SetOpened(!isOpened);
    }

    public void SetOpened(bool opened)
    {
        if (isOpened == opened) return;
        isOpened = opened;
        ApplyStateToAnimator();
    }

    private void ApplyStateToAnimator()
    {
        if (animator == null) return;

        if (!string.IsNullOrEmpty(openedBoolParam))
            animator.SetBool(openedBoolParam, isOpened);
    }
}

