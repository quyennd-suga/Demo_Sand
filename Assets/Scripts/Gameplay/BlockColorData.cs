using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockColorData", menuName = "Level Generator/BlockColorData")]
public class BlockColorData : ScriptableObject
{
    [SerializeField] private List<BlockColor> colors;

    private Dictionary<ColorEnum, BlockColor> _colorMap;

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
        _colorMap = new Dictionary<ColorEnum, BlockColor>();

        foreach (var bc in colors)
        {
            if (_colorMap.ContainsKey(bc.colorName))
            {
                Debug.LogWarning($"Duplicate color entry for {bc.colorName}", this);
                continue;
            }

            _colorMap.Add(bc.colorName, bc);
        }
    }

    public Color GetColor(ColorEnum colorEnum)
    {
        if (_colorMap != null && _colorMap.TryGetValue(colorEnum, out var bc))
            return bc.color;

        Debug.LogWarning($"Color for {colorEnum} not found. Returning white.", this);
        return Color.white;
    }

    /// <summary>
    /// Trả về 4 màu cát cho ColorEnum. Nếu chỉ set color chính,
    /// các màu phụ sẽ tự tạo variation nhẹ từ màu chính.
    /// </summary>
    public Color[] GetSandColors(ColorEnum colorEnum)
    {
        if (_colorMap != null && _colorMap.TryGetValue(colorEnum, out var bc))
            return bc.GetAllSandColors();

        Debug.LogWarning($"Sand colors for {colorEnum} not found. Returning white.", this);
        return new[] { Color.white, Color.white, Color.white, Color.white };
    }
}


[System.Serializable]
public struct BlockColor
{
    public ColorEnum colorName;
    public Color color;

    [Header("Sand Grain Colors (4 màu hạt cát)")]
    public Color sandColor2;
    public Color sandColor3;
    public Color sandColor4;

    /// <summary>
    /// Trả về mảng 4 màu. Nếu màu 2-4 chưa set (alpha=0) thì tự tạo variation.
    /// </summary>
    public Color[] GetAllSandColors()
    {
        Color c1 = color;
        Color c2 = sandColor2.a > 0.01f ? sandColor2 : DarkenColor(c1, 0.08f);
        Color c3 = sandColor3.a > 0.01f ? sandColor3 : LightenColor(c1, 0.06f);
        Color c4 = sandColor4.a > 0.01f ? sandColor4 : DarkenColor(c1, 0.04f);
        return new[] { c1, c2, c3, c4 };
    }

    private static Color DarkenColor(Color c, float amount)
    {
        return new Color(
            Mathf.Clamp01(c.r - amount),
            Mathf.Clamp01(c.g - amount),
            Mathf.Clamp01(c.b - amount),
            c.a
        );
    }

    private static Color LightenColor(Color c, float amount)
    {
        return new Color(
            Mathf.Clamp01(c.r + amount),
            Mathf.Clamp01(c.g + amount),
            Mathf.Clamp01(c.b + amount),
            c.a
        );
    }
}
