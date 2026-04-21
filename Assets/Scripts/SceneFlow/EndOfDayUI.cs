using UnityEngine;

public class EndOfDayUI : MonoBehaviour
{
    public void OnNextDayClicked()
    {
        SceneFlowManager.Instance.StartNextDay();
    }
}
