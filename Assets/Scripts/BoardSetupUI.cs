using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


// UI logic for selecting board size before starting the game.

public class BoardSetupUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider rowsSlider;
    public Slider columnsSlider;
    public Text rowsValueText;
    public Text columnsValueText;

    [Header("Scene to Load")]
    public string gameSceneName = "MainScene"; 

    private void Start()
    {
        
        rowsSlider.minValue = BoardSettings.MinRows;
        rowsSlider.maxValue = BoardSettings.MaxRows;
        columnsSlider.minValue = BoardSettings.MinColumns;
        columnsSlider.maxValue = BoardSettings.MaxColumns;
       
        rowsSlider.value = BoardSettings.Rows;
        columnsSlider.value = BoardSettings.Columns;

        // Update the text values:
        OnRowsSliderChanged(rowsSlider.value);
        OnColumnsSliderChanged(columnsSlider.value);

        // Add listeners:
        rowsSlider.onValueChanged.AddListener(OnRowsSliderChanged);
        columnsSlider.onValueChanged.AddListener(OnColumnsSliderChanged);
    }

    private void OnDestroy()
    {
        // Remove listeners to avoid memory leaks if the object is destroyed
        rowsSlider.onValueChanged.RemoveListener(OnRowsSliderChanged);
        columnsSlider.onValueChanged.RemoveListener(OnColumnsSliderChanged);
    }

    private void OnRowsSliderChanged(float value)
    {
        int intValue = Mathf.RoundToInt(value);
        BoardSettings.Rows = intValue;
        if (rowsValueText) rowsValueText.text = (("Rows : ") + intValue.ToString());
    }

    private void OnColumnsSliderChanged(float value)
    {
        int intValue = Mathf.RoundToInt(value);
        BoardSettings.Columns = intValue;
        if (columnsValueText) columnsValueText.text = ("Columns : " + intValue.ToString());
    }
    
    public void OnStartGameButton()
    {
        SceneManager.LoadScene(gameSceneName);
    }
    
}
