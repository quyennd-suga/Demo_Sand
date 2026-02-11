using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "BlockOffsetData", menuName = "Level Generator/BlockOffsetData")]
public class BlockOffsetData : ScriptableObject
{
    public List<OffsetData> blockOffsets;

    public Vector2 GetOffset(BlockShape shape, float rotation)
    {
        foreach (var blockOffset in blockOffsets)
        {
            if (blockOffset.shape == shape)
            {
                return rotation switch
                {
                    0f => blockOffset.offset,
                    90f => blockOffset.offset_90,
                    180f => blockOffset.offset_180,
                    270f => blockOffset.offset_270,
                    _ => blockOffset.offset,
                };
            }
        }
        Debug.LogWarning($"Offset for {shape} not found. Returning Vector2.zero as default.");
        return Vector2.zero; // Default offset if not found
    }
}

[System.Serializable]
public struct OffsetData
{
    public BlockShape shape;
    public Vector2 offset;
    public Vector2 offset_90;
    public Vector2 offset_180;
    public Vector2 offset_270;
}
