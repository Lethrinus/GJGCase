using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BoardManagerUI : MonoBehaviour
{
    public void OnReturnToMainMenu()
    {
        DOTween.KillAll();
        SceneManager.LoadScene("MainMenu");
    }
}