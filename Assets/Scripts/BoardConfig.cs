using UnityEngine;

[CreateAssetMenu(fileName = "BoardConfig", menuName = "ScriptableObjects/BoardConfig", order = 1)]
public class BoardConfig : ScriptableObject
{
    public float blockWidth = 2.3f;
    public float blockHeight = 2.555f;
    public float moveSpeed = 30f;
    public int thresholdA = 4;
    public int thresholdB = 7;
    public int thresholdC = 9;
    public float shuffleFadeDuration = 0.4f;
}