using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PipeData 
{
    public Vector2Int position;
    public Direction direction;
    public List<WaterColor> waterColors = new();

    // Chung
    public bool hasStar;

    public bool isLocked;
    // Lock - màu khóa cần để mở
    public int unlockColorId;

    public bool hasOpenClose;
    // OpenClose - trạng thái đang đóng hay mở
    public bool isClosed;

    // Ice - số băng cần phá
    public int iceCount;

    // Có cửa di chuyển đang che pipe này không
    //Note: lưu list pipe theo chiều kim đồng hồ
    public bool hasBarrier;
    public bool isClockwise;

    // Default constructor
    public PipeData()
    {
        waterColors = new List<WaterColor>();
    }

    public PipeData(PipeData other)
    {
        position = other.position;
        direction = other.direction;
        hasStar = other.hasStar;
        isLocked = other.isLocked;
        unlockColorId = other.unlockColorId;
        hasOpenClose = other.hasOpenClose;
        isClosed = other.isClosed;
        iceCount = other.iceCount;
        hasBarrier = other.hasBarrier;
        isClockwise = other.isClockwise;

        waterColors = new List<WaterColor>();
        foreach (var wc in other.waterColors)
        {
            waterColors.Add(new WaterColor(wc.color, wc.value));
        }
    }
}

[System.Serializable]   
public class WaterColor
{
    public int color;
    public int value; // 1 block cell = 4 unit value
    public WaterColor(int _color, int _value) {
        color = _color;
        value = _value;
    }
}
