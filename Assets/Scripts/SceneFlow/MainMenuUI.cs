using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public void OnStartGameClicked()
    {
        SceneFlowManager.Instance.StartGame();
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
