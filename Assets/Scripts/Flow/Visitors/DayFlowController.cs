using UnityEngine;

public class DayFlowController : MonoBehaviour
{
    [SerializeField] private DayManager dayManager;
    [SerializeField] private VisitorSpawner visitorSpawner;
    [SerializeField] private bool autoStartOnAwake = true;

    [Header("Timer")]
    [Tooltip("Продолжительность рабочего дня в секундах. 480 = 8 минут.")]
    [SerializeField] private float dayDuration = 60f;

    private float timeLeft;
    private bool dayActive;

    private void Awake()
    {
        if (visitorSpawner != null)
            visitorSpawner.OnDayEnded += HandleDayEnded;
    }

    private void Start()
    {
        if (autoStartOnAwake)
            StartDay();
    }

    private void OnDestroy()
    {
        if (visitorSpawner != null)
            visitorSpawner.OnDayEnded -= HandleDayEnded;
    }

    private void Update()
    {
        if (!dayActive) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
            TimerExpired();
    }

    public void StartDay()
    {
        if (dayManager == null || visitorSpawner == null) return;
        timeLeft = dayDuration;
        dayActive = true;
        visitorSpawner.StartDay();
    }

    // Таймер вышел — останавливаем спавнер, он сам вызовет OnDayEnded
    private void TimerExpired()
    {
        dayActive = false;
        visitorSpawner.StopDay();
    }

    // Все посетители прошли раньше, чем вышел таймер
    private void HandleDayEnded()
    {
        dayActive = false;
        if (SceneFlowManager.Instance != null)
            SceneFlowManager.Instance.EndDay();
    }
}
