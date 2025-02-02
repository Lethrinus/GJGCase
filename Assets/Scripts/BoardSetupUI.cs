using UnityEngine;
using UnityEngine.SceneManagement;

public class BoardSetupUI : MonoBehaviour
{
    public BoardConfig[] levels; 
    public string gameSceneName = "MainScene";

    public void LoadLevel(int levelIndex)
    {
        LevelTracker.selectedConfig = levels[levelIndex];
        SceneManager.LoadScene(gameSceneName);
    }
}

public static class LevelTracker
{
    public static BoardConfig selectedConfig;
}