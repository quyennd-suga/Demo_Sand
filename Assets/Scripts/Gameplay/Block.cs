using System.Collections.Generic;
using UnityEngine;

public class Block
{
    public int index;
    public BlockData data;


    public bool isCompleted;
    public int filledUnits;   // single color only
    public int capacityUnits;

    public int innerFilledUnits;
    public int innerCapacityUnits;
    public bool isInnerCompleted;

    public Dictionary<int, BlockCapacity> capacitiesPerColor = new Dictionary<int, BlockCapacity>(); // multi-color support

    public Block(int index, BlockData data)
    {
        this.index = index;
        this.data = new BlockData(data); // ✅ clone runtime

        int cellCount = ShapeLibrary.GetOccupiedCells(this.data).Length;
        capacityUnits = cellCount * GlobalValues.waterUnit;
        filledUnits = 0;
        innerCapacityUnits = data.innerBlockColor >= 0 ? capacityUnits : 0;
        innerFilledUnits = 0;
        if (data.isMixColor)
        {
            capacitiesPerColor.Clear();
            for(int i = 0; i < data.mixColors.Count; i++)
            {
                var colorData = data.mixColors[i];
                BlockCapacity bc = new BlockCapacity
                {
                    capacityUnits = colorData.colorCount,
                    filledUnits = 0
                };
                capacitiesPerColor.Add(colorData.color, bc);
            }
        }
    }

    public bool FillInnerBlock(int units)
    {
        innerFilledUnits += units;
        if(innerFilledUnits >= innerCapacityUnits)
        {
            // Inner block complete
            InnerBlockComplete();
            return true;
        }
        return false;
    }
    public bool FillBlock(int units)
    {
        filledUnits += units;
        if(filledUnits >= capacityUnits)
        {
            BlockComplete();
            return true;
        }
        return false;
    }
    public bool FillBlockColor(int colorIndex, int units)
    {
        if (capacitiesPerColor.ContainsKey(colorIndex))
        {
            var bc = capacitiesPerColor[colorIndex];
            bc.filledUnits += units;

            // check if this color is full
            bool colorFull = bc.filledUnits >= bc.capacityUnits;
            // check if all colors are full
            bool allFull = true;
            foreach (var c in capacitiesPerColor.Values)
            {
                if (c.filledUnits < c.capacityUnits)
                {
                    allFull = false;
                    break;
                }
            }
            if (allFull)
            {
                BlockComplete();
                return true;
            }
        }
        return false;
    }

    private void BlockComplete()
    {
        filledUnits = 0;
        isCompleted = true;
    }   
    private void InnerBlockComplete()
    {
        innerFilledUnits = 0;
        isInnerCompleted = true;
    }


    public int RemainingUnits() => Mathf.Max(0, capacityUnits - filledUnits);
    public int RemainingUnitsColor(int colorIndex)
    {
        if (capacitiesPerColor.ContainsKey(colorIndex))
        {
            var bc = capacitiesPerColor[colorIndex];
            return Mathf.Max(0, bc.capacityUnits - bc.filledUnits);
        }
        return 0;
    }
    public int InnerRemainingUnits() => Mathf.Max(0, innerCapacityUnits - innerFilledUnits);
    public bool IsFull() => filledUnits >= capacityUnits;
}
public class BlockCapacity
{
    public int capacityUnits;
    public int filledUnits;
}
