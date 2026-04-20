using UnityEngine;

/// <summary>
/// Glue component: start a day and begin spawning visitors.
/// You can call StartDay() from UI/button or on scene start.
/// </summary>
public class DayFlowController : MonoBehaviour
{
    [SerializeField] private DayManager dayManager;
    [SerializeField] private VisitorSpawner visitorSpawner;
    [SerializeField] private bool autoStartOnAwake = true;

    private void Awake()
    {
        if (!autoStartOnAwake) return;
        StartDay();
    }

    public void StartDay()
    {
        if (dayManager == null || visitorSpawner == null) return;
        visitorSpawner.StartDay();
    }
}

