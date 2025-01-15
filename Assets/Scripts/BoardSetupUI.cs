using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// UI logic for selecting board size before starting the game.
/// Attach to a GameObject in a menu scene with sliders and button references.
/// </summary>
public class BoardSetupUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider rowsSlider;
    public Slider columnsSlider;
    public Text rowsValueText;
    public Text columnsValueText;

    [Header("Scene to Load")]
    public string gameSceneName = "MainScene"; // Name of your actual game scene.

    private void Start()
    {
        // Initialize the slider limits (using BoardSettings if you want to clamp).
        rowsSlider.minValue = BoardSettings.MinRows;
        rowsSlider.maxValue = BoardSettings.MaxRows;

        columnsSlider.minValue = BoardSettings.MinColumns;
        columnsSlider.maxValue = BoardSettings.MaxColumns;

        // Initialize sliders to default or previously chosen values:
        rowsSlider.value = BoardSettings.Rows;
        columnsSlider.value = BoardSettings.Columns;

        // Update the text values immediately:
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

    // Called when the Rows slider value changes:
    public void OnRowsSliderChanged(float value)
    {
        int intValue = Mathf.RoundToInt(value);
        BoardSettings.Rows = intValue;
        if (rowsValueText) rowsValueText.text = intValue.ToString();
    }

    // Called when the Columns slider value changes:
    public void OnColumnsSliderChanged(float value)
    {
        int intValue = Mathf.RoundToInt(value);
        BoardSettings.Columns = intValue;
        if (columnsValueText) columnsValueText.text = intValue.ToString();
    }

    // Called when the Start Game button is clicked:
    public void OnStartGameButton()
    {
        // If you want, add checks here for validity or display warnings.
        SceneManager.LoadScene(gameSceneName);
    }
}
