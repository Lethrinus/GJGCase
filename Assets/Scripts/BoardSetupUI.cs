using UnityEngine;
using UnityEngine.SceneManagement;

public class BoardSetupUI : MonoBehaviour
{
    public string gameSceneName = "MainScene";

    public void OnStartGameButton()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}