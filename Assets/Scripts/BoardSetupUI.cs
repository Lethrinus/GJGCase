using UnityEngine;
using UnityEngine.SceneManagement;

public class BoardSetupUI : MonoBehaviour
{
    public BoardConfig[] levels; 
    public string gameSceneName = "MainScene";

    private void Awake()
    {
        LevelTracker.levels = levels;
    }

    public void LoadLevel(int levelIndex)
    {
        LevelTracker.currentLevelIndex = levelIndex;
        LevelTracker.selectedConfig = levels[levelIndex];
        SceneManager.LoadScene(gameSceneName);
    }
}