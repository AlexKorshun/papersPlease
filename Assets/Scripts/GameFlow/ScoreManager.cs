using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public static event Action<int> OnScoreChanged;

    [SerializeField] private int scorePerCorrect  = 10;
    [SerializeField] private int penaltyPerWrong  = 5;

    public int Score { get; private set; }
    public int TotalScore { get; private set; }
    public int CurrentDay => SceneFlowManager.Instance != null ? SceneFlowManager.Instance.CurrentDay : 1;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddScore()
    {
        Score += scorePerCorrect;
        TotalScore += scorePerCorrect;
        OnScoreChanged?.Invoke(Score);
    }

    public void AddPenalty()
    {
        Score -= penaltyPerWrong;
        TotalScore -= penaltyPerWrong;
        OnScoreChanged?.Invoke(Score);
    }

    public void Reset()
    {
        Score = 0;
        OnScoreChanged?.Invoke(Score);
    }
}
