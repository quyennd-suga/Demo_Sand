using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlockData
{
    public int blockIndex; // index của block trong LevelData.blocks
    public Vector2Int position;
    public float rotation;

    public BlockShape shapeType;
    public int color; // màu của block
    public bool isStone; // có phải block đá không
    public int innerBlockColor; // color của block bên trong (dùng cho Block có 2 lớp), mặc định = -1 (không có)
    // Chung
    public bool hasStar;
    public List<int> linkedBlockIndexes; // danh sách index các block liên kết, index ở đây là index trong LevelData.blocks

    public int iceCount; // số lớp băng, mặc định = 0 (không có băng)

    // OpenClose - trạng thái đang đóng hay mở
    public bool isCloseOpen;
    public bool isStartOpen;

    // Direction, Stone - hướng di chuyển
    public MoveAxis direction;

    // Crate - số thùng cần phá
    public int crateCount; // số thùng cần phá, if <= 0 thì không có thùng


    // Key - màu khóa
    public int keyColorIndex; // index màu khóa, mặc định = -1 (không có khóa)

    // Bomb - số giây đếm ngược

    public float bombTimeLimit; // thời gian đếm ngược, if <= 0 thì không có bom

    // BombStepLimit - số nước đi trước khi nổ
    public int bombMoveLimit; // số nước đi trước khi nổ, if <= 0 thì không có bom

    // Rope - list màu dây đang khóa
    public List<int> ropeColors = new();

    // Scissor - màu kéo
    public int scissorColor; // màu kéo, if < 0 thì không có kéo

    // TwoColor - màu cho từng cell (index tương ứng với shapeOffsets)
    public bool isMixColor; // có phải block trộn màu không
    public List<ColorBlockData> mixColors = new();


    public BlockData() { }

    public BlockData(BlockData other)
    {
        blockIndex = other.blockIndex;
        position = other.position;
        rotation = other.rotation;

        shapeType = other.shapeType;
        color = other.color;
        isStone = other.isStone;
        innerBlockColor = other.innerBlockColor;

        hasStar = other.hasStar;

        linkedBlockIndexes = other.linkedBlockIndexes != null
            ? new List<int>(other.linkedBlockIndexes)
            : null;

        iceCount = other.iceCount;

        isCloseOpen = other.isCloseOpen;
        isStartOpen = other.isStartOpen;

        direction = other.direction;
        crateCount = other.crateCount;

        keyColorIndex = other.keyColorIndex;

        bombTimeLimit = other.bombTimeLimit;
        bombMoveLimit = other.bombMoveLimit;

        ropeColors = other.ropeColors != null
            ? new List<int>(other.ropeColors)
            : new List<int>();

        scissorColor = other.scissorColor;

        isMixColor = other.isMixColor;

        mixColors = other.mixColors != null
            ? new List<ColorBlockData>(other.mixColors.Count)
            : new List<ColorBlockData>();

        if (other.mixColors != null)
        {
            for (int i = 0; i < other.mixColors.Count; i++)
            {
                var c = other.mixColors[i];
                mixColors.Add(c != null ? new ColorBlockData(c) : null);
            }
        }
    }



}

[System.Serializable]
public class ColorBlockData
{
    public int color;
    public int colorCount; // 1 cell tương đương 4 units

    public ColorBlockData() { }

    public ColorBlockData(ColorBlockData other)
    {
        color = other.color;
        colorCount = other.colorCount;
    }

}