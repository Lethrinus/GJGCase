using UnityEngine;

[CreateAssetMenu(fileName = "BoardConfig", menuName = "ScriptableObjects/BoardConfig", order = 1)]
public class BoardConfig : ScriptableObject
{
    [Header("Basic Board Settings")]
    public float blockWidth = 2.3f;
    public float blockHeight = 2.555f;
    public float moveSpeed = 30f;
    public int thresholdA = 4;
    public int thresholdB = 7;
    public int thresholdC = 9;
    public float shuffleFadeDuration = 0.8f;
    public int initialMoves = 20;
    
    
    
    [Header("Goal Settings")]
    [Tooltip("Target number of blocks to clear (for levels 1 and 2)")]
    public int targetBlockGoal = 40;
    
    [Header("Goal Settings - Multi Icons")]
    public Sprite crateIcon;     
    public int crateGoalCount; 
    
    
    [Tooltip("Target block color ID for block-based goals (for levels 1 and 2)")]
    public int targetBlockColorID = 0;

    [Tooltip("Target number of crates to clear (for level 3)")]
    public int targetCrateGoal = 10;

    [Header("Board Shape")]
    public int rows = 8;
    public int columns = 8;
    [Tooltip("If true, uses cellMask to determine which cells are valid.")]
    public bool useShapeMask = false;
    [Tooltip("Must have length == rows * columns if useShapeMask is true.")]
    public bool[] cellMask;

    [Header("Crate Settings")]
    [Tooltip("If true, crates will be used (for example, on level 3).")]
    public bool useCrates = false;

    [Header("Art & Environment")]
    public GameObject environmentPrefab;
    [Header("UI")]
    public CanvasRoot uiCanvasPrefab;
}