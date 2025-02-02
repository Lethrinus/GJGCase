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
    
    public int targetBlockGoal = 40;
    
    [Header("Goal Settings - Multi Icons")]
    public Sprite crateIcon;
    public Sprite targetIcon;
    
    
    public int targetBlockColorID;
    public int targetCrateGoal = 10;

    [Header("Board Shape")]
    public int rows = 8;
    public int columns = 8;
    public bool useShapeMask;
    public bool[] cellMask;

    [Header("Crate Settings")]
    public bool useCrates;

    [Header("Art & Environment")]
    public GameObject environmentPrefab;
    [Header("UI")]
    public CanvasRoot uiCanvasPrefab;
}