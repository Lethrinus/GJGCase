using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BoardSetupUI : MonoBehaviour
{
    public Slider rowsSlider;
    public Slider columnsSlider;
    public Text rowsValueText;
    public Text columnsValueText;
    public string gameSceneName = "MainScene";
    void Start()
    {
        rowsSlider.minValue = BoardSettings.MinRows;
        rowsSlider.maxValue = BoardSettings.MaxRows;
        columnsSlider.minValue = BoardSettings.MinColumns;
        columnsSlider.maxValue = BoardSettings.MaxColumns;
        rowsSlider.value = BoardSettings.Rows;
        columnsSlider.value = BoardSettings.Columns;
        OnRowsSliderChanged(rowsSlider.value);
        OnColumnsSliderChanged(columnsSlider.value);
        rowsSlider.onValueChanged.AddListener(OnRowsSliderChanged);
        columnsSlider.onValueChanged.AddListener(OnColumnsSliderChanged);
    }
    void OnDestroy()
    {
        rowsSlider.onValueChanged.RemoveListener(OnRowsSliderChanged);
        columnsSlider.onValueChanged.RemoveListener(OnColumnsSliderChanged);
    }
    void OnRowsSliderChanged(float value)
    {
        int v = Mathf.RoundToInt(value);
        BoardSettings.Rows = v;
        if (rowsValueText) rowsValueText.text = "Rows : " + v;
    }
    void OnColumnsSliderChanged(float value)
    {
        int v = Mathf.RoundToInt(value);
        BoardSettings.Columns = v;
        if (columnsValueText) columnsValueText.text = "Columns : " + v;
    }
    public void OnStartGameButton()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}