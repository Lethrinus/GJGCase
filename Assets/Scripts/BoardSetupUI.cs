using UnityEngine;
using UnityEngine.SceneManagement;

public class BoardSetupUI : MonoBehaviour
{
    public BoardConfig[] levels; // set in inspector
    public string gameSceneName = "MainScene";

    public void LoadLevel(int levelIndex)
    {
        LevelTracker.SelectedConfig = levels[levelIndex];
        SceneManager.LoadScene(gameSceneName);
    }
}

public static class LevelTracker
{
    public static BoardConfig SelectedConfig;
}