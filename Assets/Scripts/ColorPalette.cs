using UnityEngine;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "ScriptableObjects/ColorPalette", order = 2)]
public class ColorPalette : ScriptableObject
{
    public Color[] colors;
}