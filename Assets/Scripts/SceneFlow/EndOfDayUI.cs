using UnityEngine;
using TMPro;

public class EndOfDayUI : MonoBehaviour
{
    [SerializeField] private TMP_Text dayLabel;
    [SerializeField] private TMP_Text scoreLabel;
    [SerializeField] private TMP_Text totalScoreLabel;

    private void Start()
    {
        if (ScoreManager.Instance == null) return;

        if (dayLabel != null)
            dayLabel.text = $"День {ScoreManager.Instance.CurrentDay}";

        if (scoreLabel != null)
            scoreLabel.text = $"Очки за день: {ScoreManager.Instance.Score}";

        if (totalScoreLabel != null)
            totalScoreLabel.text = $"Всего: {ScoreManager.Instance.TotalScore}";
    }

    public void OnNextDayClicked()
    {
        ScoreManager.Instance?.Reset();
        SceneFlowManager.Instance.StartNextDay();
    }
}

