using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager Instance { get; private set; }

    public static event Action<SceneFlowState, SceneFlowState> OnSceneStateChanged;

    public SceneFlowState CurrentState { get; private set; }
    public int CurrentDay { get; private set; } = 1;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentState = SceneFlowState.MainMenu;
    }

    // Call from "Начать игру" button in MainMenu scene
    public void StartGame()
    {
        CurrentDay = 1;
        TransitionTo(SceneFlowState.Intro);
    }

    // Call from "Продолжить" button in Intro scene
    public void StartFirstDay()
    {
        TransitionTo(SceneFlowState.Game);
    }

    // Called by DayFlowController when all visitors have been processed
    public void EndDay()
    {
        TransitionTo(SceneFlowState.EndOfDay);
    }

    // Call from "Следующий день" button in EndOfDay scene
    public void StartNextDay()
    {
        CurrentDay++;
        TransitionTo(SceneFlowState.Game);
    }

    private void TransitionTo(SceneFlowState next)
    {
        SceneFlowState prev = CurrentState;
        CurrentState = next;
        OnSceneStateChanged?.Invoke(prev, next);

        switch (next)
        {
            case SceneFlowState.MainMenu: SceneManager.LoadScene(SceneIndex.MainMenu); break;
            case SceneFlowState.Intro:    SceneManager.LoadScene(SceneIndex.Intro);    break;
            case SceneFlowState.Game:     SceneManager.LoadScene(SceneIndex.Game);     break;
            case SceneFlowState.EndOfDay: SceneManager.LoadScene(SceneIndex.EndOfDay); break;
        }
    }
}
