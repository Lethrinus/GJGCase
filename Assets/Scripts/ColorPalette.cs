using UnityEngine;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "ScriptableObjects/ColorPalette", order = 3)]
public class ColorPalette : ScriptableObject
{
    public Color[] colors;
}