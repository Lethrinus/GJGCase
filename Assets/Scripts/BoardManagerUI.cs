using UnityEngine;
using UnityEngine.SceneManagement;

public class BoardManagerUI : MonoBehaviour
{
    public void OnReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}