using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public void OnStartGameClicked()
    {
        SceneFlowManager.Instance.StartGame();
    }
}
