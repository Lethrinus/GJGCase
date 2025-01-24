using UnityEngine;

[CreateAssetMenu(fileName="LevelConfig",menuName="ScriptableObjects/LevelConfig")]
public class LevelConfig : ScriptableObject
{
    public int rows;
    public int columns;
    public Vector2Int[] disabledCells;
    public Vector2Int[] lockedCells;
    public int moveCount;
    public int goalCount;
}