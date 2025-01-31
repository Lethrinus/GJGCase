using UnityEngine;

[CreateAssetMenu(fileName = "BoardConfigForLevel2", menuName = "ScriptableObjects/BoardConfigForLevel2", order = 2)]
public class BoardConfigForLevel2 : ScriptableObject
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
    public int goalBlocks = 40;

    [Header("Board Shape")]
    public int rows = 8;
    public int columns = 8;
    [Tooltip("If true, uses cellMask to determine which cells are valid.")]
    public bool useShapeMask = false;
    [Tooltip("Must have length == rows * columns if useShapeMask = true.")]
    public bool[] cellMask;  

    [Header("Background / Frame Art")] 
    public GameObject environmentPrefab;
}