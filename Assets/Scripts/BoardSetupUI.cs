using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BoardSetupUI : MonoBehaviour
{
    
    public string gameSceneName = "MainScene";
    void Start(){
        
    
    }
    void OnDestroy()
    {
    }
    
    
    public void OnStartGameButton()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}