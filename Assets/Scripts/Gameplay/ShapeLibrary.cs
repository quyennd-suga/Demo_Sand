using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class ShapeLibrary
{
    private static readonly Dictionary<BlockShape, Vector2Int[]> BaseOffsets = new()
    {
        { BlockShape.One,   new[] { new Vector2Int(0,0) } },
        { BlockShape.Two,   new[] { new Vector2Int(0,0), new Vector2Int(0,1) } },
        { BlockShape.Three, new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2) } },

        { BlockShape.L,      new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(1,0) } },
        { BlockShape.ShortL, new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,0) } },

        { BlockShape.TwoSquare, new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) } },

        { BlockShape.ShortT, new[] { new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,-1) } },

        { BlockShape.Plus,   new[] { new Vector2Int(0,-1), new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1) } },
        { BlockShape.ReverseL, new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(-1,0) } },
    };

    private static readonly Dictionary<int, Vector2Int[]> RotCache = new();
    


    public static Vector2Int[] GetOffsets(BlockData data)
    {
        if (!BaseOffsets.TryGetValue(data.shapeType, out var baseOff))
            baseOff = BaseOffsets[BlockShape.One];

        //if(data.shapeType == BlockShape.TwoSquare || data.shapeType == BlockShape.Three || data.shapeType == BlockShape.Two)
        //    return baseOff;

        int rot = NormalizeRotation((int)data.rotation);
        int key = ((int)data.shapeType * 1000) + rot;

        if (RotCache.TryGetValue(key, out var cached))
            return cached;

        var rotated = GetRotation(baseOff, rot, data.shapeType);
        RotCache[key] = rotated;
        return rotated;
    }

    private static Vector2Int[] GetRotation(Vector2Int[] baseOffset, int rot, BlockShape shape)
    {
        if(shape == BlockShape.TwoSquare)
            return RotateOffsetsCenter(baseOffset, rot);
        else if (shape == BlockShape.Three)
            return RotateThreeDiscrete(rot);
        else if (shape == BlockShape.Two)
            return RotateTwoDiscrete(rot);
        else
            return RotateOffsets(baseOffset, rot);
    }
    private static Vector2Int[] RotateThreeDiscrete(int rot)
    {
        // Anchor luôn tại (0,0)
        return rot switch
        {
            0 => new[]
            {
            new Vector2Int(0,0),
            new Vector2Int(0,1),
            new Vector2Int(0,2),
        },

            90 => new[]
            {
            new Vector2Int(-1,0),
            new Vector2Int(0,0),
            new Vector2Int(1,0),
        },
        
            180 => new[]
                {
            new Vector2Int(0,2),
            new Vector2Int(0,1),
            new Vector2Int(0,0),
        },

            270 => new[]
                {
            new Vector2Int(1,0),
            new Vector2Int(0,0),
            new Vector2Int(-1,0),
        },

            _ => new[]
            {
            new Vector2Int(0,0),
            new Vector2Int(0,1),
            new Vector2Int(0,2),
        }
        };
    }

    private static Vector2Int[] RotateTwoDiscrete(int rot)
    {
        // Anchor at (0,0)
        return rot switch
        {
            0 => new[] { new Vector2Int(0, 0), new Vector2Int(0, 1) },
            90 => new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) },
            180 => new[] { new Vector2Int(0, 1), new Vector2Int(0, 0) },
            270 => new[] { new Vector2Int(1, 0), new Vector2Int(0, 0) },
            _ => new[] { new Vector2Int(0, 0), new Vector2Int(0, 1) }
        };
    }

    public static void FillOccupiedCells(BlockData data, Vector2Int position, List<Vector2Int> buffer)
    {
        buffer.Clear();
        var offs = GetOffsets(data);
        for (int i = 0; i < offs.Length; i++)
            buffer.Add(position + offs[i]);
    }

    public static Vector2Int[] GetOccupiedCells(BlockData data)
    {
        var offs = GetOffsets(data);
        var arr = new Vector2Int[offs.Length];
        var pos = data.position;
        for (int i = 0; i < offs.Length; i++)
            arr[i] = pos + offs[i];
        return arr;
    }

    private static int NormalizeRotation(int rot)
    {
        rot %= 360;
        if (rot < 0) rot += 360;
        rot = Mathf.RoundToInt(rot / 90f) * 90;
        rot %= 360;
        return rot;
    }

    private static Vector2Int[] RotateOffsets(Vector2Int[] src, int rot)
    {
        var arr = new Vector2Int[src.Length];
        for (int i = 0; i < src.Length; i++)
        {
            var p = src[i];
            arr[i] = rot switch
            {
                0 => p,
                90 => new Vector2Int(-p.y, p.x),
                180 => new Vector2Int(-p.x, -p.y),
                270 => new Vector2Int(p.y, -p.x),
                _ => p
            };
        }
        return arr;
    }
    private static Vector2Int[] RotateOffsetsCenter(Vector2Int[] src, int rot)
    {
        // 1) Tính bounding box của shape
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        for (int i = 0; i < src.Length; i++)
        {
            var p = src[i];
            if (p.x < minX) minX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.x > maxX) maxX = p.x;
            if (p.y > maxY) maxY = p.y;
        }

        // 2) Tâm hình (có thể là .5 với shape chẵn như 2x2)
        Vector2 center = new Vector2(
            (minX + maxX) * 0.5f,
            (minY + maxY) * 0.5f
        );

        var arr = new Vector2Int[src.Length];

        for (int i = 0; i < src.Length; i++)
        {
            // 3) đưa point về quanh tâm
            Vector2 p = (Vector2)src[i] - center;

            // 4) rotate quanh (0,0)
            Vector2 r = rot switch
            {
                0 => p,
                90 => new Vector2(-p.y, p.x),
                180 => new Vector2(-p.x, -p.y),
                270 => new Vector2(p.y, -p.x),
                _ => p
            };

            // 5) đưa về lại không gian grid
            Vector2 final = r + center;

            // 6) snap về cell integer
            arr[i] = new Vector2Int(
                Mathf.RoundToInt(final.x),
                Mathf.RoundToInt(final.y)
            );
        }

        return arr;
    }


    private readonly static float cellSize = 1f; // Assuming each cell is 1 unit in size
    public static Vector2 GetBlockOffset(BlockShape shape, float rotation)
    {
        switch (shape)
        {
            case BlockShape.Two:
                if (rotation == 0f)
                    return new Vector2(0f, cellSize / 2f);
                else if (rotation == 180f)
                    return new Vector2(0f, cellSize / 2f);
                else if (rotation == 90f)
                    return new Vector2(cellSize / 2f, 0f);
                else
                    return new Vector2(cellSize / 2f, 0f);
            case BlockShape.Three:
                if (rotation == 0f)
                    return new Vector2(0f, cellSize);
                else if (rotation == 180f)
                    return new Vector2(0f, cellSize);
                else if (rotation == 90f)
                    return new Vector2(0f, 0f);
                else // 270
                    return new Vector2(0f, 0f);
            case BlockShape.TwoSquare:
                return new Vector2(cellSize / 2f, cellSize / 2f);
            default:
                return Vector2.zero;
        }
    }
}
