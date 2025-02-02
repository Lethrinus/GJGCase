using UnityEngine;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "ScriptableObjects/ColorPalette", order = 3)]
public class ColorPalette : ScriptableObject
{
    
    [Header("Color palette for particle system")]
    public Color[] colors;
}