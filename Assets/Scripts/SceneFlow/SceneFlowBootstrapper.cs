using UnityEngine;

// Place this prefab in the MainMenu scene.
// It spawns SceneFlowManager once at startup so it persists across all scenes.
public class SceneFlowBootstrapper : MonoBehaviour
{
    [SerializeField] private SceneFlowManager sceneFlowManagerPrefab;

    void Awake()
    {
        if (SceneFlowManager.Instance != null) return;
        if (sceneFlowManagerPrefab != null)
            Instantiate(sceneFlowManagerPrefab);
    }
}
