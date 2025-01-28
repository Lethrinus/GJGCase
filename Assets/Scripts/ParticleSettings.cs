using UnityEngine;

[CreateAssetMenu(fileName = "ParticleSettings", menuName = "ScriptableObjects/ParticleSettings", order = 3)]
public class ParticleSettings : ScriptableObject
{
    public ParticleSystem particlePrefab;
    public float particleLifeTime = 1f;
}