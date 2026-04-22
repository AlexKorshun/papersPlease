using System.Collections;
using UnityEngine;

public class ScannerController : MonoBehaviour
{
    [Header("State objects (assign child GameObjects from the Scanner prefab)")]
    [SerializeField] private GameObject stateIdle;
    [SerializeField] private GameObject stateCalm;
    [SerializeField] private GameObject stateAnxiety;
    [SerializeField] private GameObject stateFear;
    [SerializeField] private GameObject stateAngry;

    [Header("Slide animation")]
    [Tooltip("Смещение в скрытом состоянии относительно видимой позиции (локальные координаты).")]
    [SerializeField] private Vector3 hiddenOffset = new Vector3(0f, 6f, 0f);
    [SerializeField] private float slideInDuration  = 0.45f;
    [SerializeField] private float slideOutDuration = 0.25f;
    [SerializeField] private float bounceStrength   = 1.5f;

    private bool scannerActive;
    private Vector3 visiblePosition;
    private Coroutine slideRoutine;

    // Опускается с отскоком (ease out back)
    private float EaseOutBack(float t)
    {
        float c1 = bounceStrength;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // Поднимается плавно без отскока (ease in quad)
    private static float EaseInQuad(float t) => t * t;

    private void OnEnable()
    {
        GameFlowController.OnStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameFlowController.OnStateChanged -= OnGameStateChanged;
    }

    private void Start()
    {
        visiblePosition = transform.localPosition;
        transform.localPosition = visiblePosition + hiddenOffset;
        HideAll();
    }

    private void OnGameStateChanged(GameState prev, GameState next)
    {
        if (next != GameState.VisitorPresent)
        {
            scannerActive = false;
            SlideOut();
        }
    }

    public void ToggleScanner()
    {
        if (GameFlowController.Instance?.CurrentState != GameState.VisitorPresent)
            return;

        scannerActive = !scannerActive;

        if (scannerActive)
        {
            ShowVisitorState();
            SlideIn();
        }
        else
        {
            SlideOut();
        }
    }

    private void SlideIn()
    {
        if (slideRoutine != null) StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(SlideRoutine(
            visiblePosition + hiddenOffset, visiblePosition,
            slideInDuration, EaseOutBack, onComplete: null));
    }

    private void SlideOut()
    {
        if (slideRoutine != null) StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(SlideRoutine(
            transform.localPosition, visiblePosition + hiddenOffset,
            slideOutDuration, EaseInQuad, onComplete: HideAll));
    }

    private IEnumerator SlideRoutine(Vector3 from, Vector3 to, float duration,
        System.Func<float, float> easing, System.Action onComplete)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = easing(Mathf.Clamp01(t / duration));
            transform.localPosition = Vector3.LerpUnclamped(from, to, p);
            yield return null;
        }
        transform.localPosition = to;
        onComplete?.Invoke();
        slideRoutine = null;
    }

    private void ShowVisitorState()
    {
        VisitorController visitor = GameFlowController.Instance?.ActiveVisitor;
        if (visitor == null)
        {
            HideAll();
            return;
        }

        GameObject target = visitor.Profile.EmotionalState switch
        {
            ScannerEmotion.Calm    => stateCalm,
            ScannerEmotion.Anxiety => stateAnxiety,
            ScannerEmotion.Fear    => stateFear,
            _                      => stateCalm,
        };
        Show(target);
    }

    private void HideAll()
    {
        SetActive(stateIdle,    false);
        SetActive(stateCalm,    false);
        SetActive(stateAnxiety, false);
        SetActive(stateFear,    false);
        SetActive(stateAngry,   false);
    }

    private void Show(GameObject target)
    {
        HideAll();
        SetActive(target, true);
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}
