using UnityEngine;

[CreateAssetMenu(fileName = "BoardConfig", menuName = "ScriptableObjects/BoardConfig", order = 1)]
public class BoardConfig : ScriptableObject
{
    [Header("Board Setup")]
    public float blockSize = 2.3f;
    public float moveSpeed = 30f;

    [Header("Thresholds")]
    public int thresholdA = 4;
    public int thresholdB = 7;
    public int thresholdC = 9;

    [Header("Shuffle / Misc")]
    public float shuffleFadeDuration = 0.4f;

    [Header("Camera Shake")]
    public float shakeDuration = 0.36f;
    public float shakeMagnitude = 0.24f;
}