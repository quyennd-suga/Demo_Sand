using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockColorData", menuName = "Level Generator/BlockColorData")]
public class BlockColorData : ScriptableObject
{
    [SerializeField] private List<BlockColor> colors;

    private Dictionary<ColorEnum, Color> _colorMap;

    private void OnEnable()
    {
        BuildCache();
    }

    private void OnValidate()
    {
        BuildCache();
    }

    private void BuildCache()
    {
        _colorMap = new Dictionary<ColorEnum, Color>();

        foreach (var bc in colors)
        {
            if (_colorMap.ContainsKey(bc.colorName))
            {
                Debug.LogWarning($"Duplicate color entry for {bc.colorName}", this);
                continue;
            }

            _colorMap.Add(bc.colorName, bc.color);
        }
    }

    public Color GetColor(ColorEnum colorEnum)
    {
        if (_colorMap != null && _colorMap.TryGetValue(colorEnum, out var c))
            return c;

        Debug.LogWarning($"Color for {colorEnum} not found. Returning white.", this);
        return Color.white;
    }
}


[System.Serializable]
public struct BlockColor
{

    public ColorEnum colorName;
    public Color color;
}
