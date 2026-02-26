using System.Collections.Generic;
using UnityEngine;

public class Board
{
    public int width, height;

    public Dictionary<int, Block> blocks = new();
    public Dictionary<Vector2Int, Pipe> pipesByPos = new();

    // Occupancy: cell -> blockIndex
    private Dictionary<Vector2Int, int> cellToBlock = new();

    // Holes / empty nodes
    private HashSet<Vector2Int> holes = new();

    // Lock state while pouring
    public bool IsBusy { get; private set; }

    private int _removedBlocksCount;
    public int removedBlocksCount
    { 
        get => _removedBlocksCount;
        set
        {
            _removedBlocksCount = value;
        }
    }

    public Board(LevelData level)
    {
        width = level.width;
        height = level.height;

        holes = new HashSet<Vector2Int>();
        foreach (var node in level.emptyNodes)
            holes.Add(node.position);


        // init pipes
        foreach (var pd in level.pipes)
        {
            var p = new Pipe(pd);
            pipesByPos[p.position] = p;
        }

        // init blocks
        for (int i = 0; i < level.blocks.Count; i++)
            blocks[i] = new Block(i, level.blocks[i]);

        RebuildOccupancy();
    }
    public int TryGetBlockOnPipe(Vector2Int pipeCell)
    {
        if(cellToBlock.TryGetValue(pipeCell, out var blockIndex))
        {
            return blockIndex;
        }
        return -1;
    }

    private void HandleBlockCompleted()
    {
        removedBlocksCount++;
        RemoveIceBlock();
    }
    private void RemoveIceBlock()
    {
        foreach(var kv in blocks)
        {
            var block = kv.Value;
            if(block.data.iceCount > 0)
            {
                block.data.iceCount--;
            }
        }
    }
    public bool CheckWinCondition()
    {
        foreach(var bl in blocks.Values)
        {
            if(!bl.isCompleted)
            {
                return false;
            }
        }
        return true;
    }
    public void CheckBlocksOnPipes()
    {
        foreach (var kv in pipesByPos)
        {
            var pipeCell = kv.Key;
            var pipe = kv.Value;
            int blockIndex = TryGetBlockOnPipe(pipeCell);
            if (blockIndex >= 0)
            {
                var block = blocks[blockIndex];
                if(block.isCompleted)
                {
                    continue;
                }
                if(block.data.iceCount > 0)
                {
                    continue;
                }
                if(block.data.isStone)
                {
                    continue;
                }
                if (block.data.innerBlockColor >= 0)
                {
                    if(!block.isInnerCompleted)
                    {
                        if(block.data.innerBlockColor == pipe.TopColor)
                        {
                            // Inner block matches pipe color
                            int takeAmount = pipe.TopValue;
                            int remainingUnits = block.InnerRemainingUnits();
                            int fillAmount = Mathf.Min(takeAmount, remainingUnits);
                            if (fillAmount > 0)
                            {
                                int taken = pipe.TakeFromTop(fillAmount);
                                bool isInnerComplete = block.FillInnerBlock(taken);
                                if (isInnerComplete) HandleBlockCompleted();
                                PipeView pipeView = GameController.Instance.boardView.GetPipeView(pipeCell);
                                float pourWorldX = pipeView != null ? pipeView.transform.position.x : 0f;
                                BlockView blockView = GameController.Instance.boardView.GetBlockView(blockIndex);
                                if (blockView != null)
                                {
                                    blockView.PourInner(taken, isInnerComplete, pourWorldX);
                                }
                                if (pipeView != null)
                                {
                                    Vector2Int targetCell = GetCellTargetToPour(pipeCell, blockIndex);
                                    Vector3 target = new Vector3(targetCell.x, targetCell.y - 0.5f, 0f);
                                    pipeView.SqueezeFruit(taken, target);
                                }
                                
                            }
                        }
                        continue;
                    }
                }
                if (block.data.isMixColor == false)
                {
                    if (block.data.color == pipe.TopColor)
                    {
                        // Block matches pipe color
                        int takeAmount = pipe.TopValue;
                        int remainingUnits = block.RemainingUnits();
                        int fillAmount = Mathf.Min(takeAmount, remainingUnits);
                        if (fillAmount > 0)
                        {
                            int taken = pipe.TakeFromTop(fillAmount);
                            bool isComplete = block.FillBlock(taken);
                            if (isComplete) HandleBlockCompleted();
                            PipeView pipeView = GameController.Instance.boardView.GetPipeView(pipeCell);
                            float pourWorldX = pipeView != null ? pipeView.transform.position.x : 0f;
                            BlockView blockView = GameController.Instance.boardView.GetBlockView(blockIndex);
                            if (blockView != null)
                            {
                                blockView.Pour(taken, isComplete, block.data.color, pourWorldX);
                            }
                            if (pipeView != null)
                            {
                                Vector2Int targetCell = GetCellTargetToPour(pipeCell, blockIndex);
                                Vector3 target = new Vector3(targetCell.x, targetCell.y - 0.5f, 0f);
                                pipeView.SqueezeFruit(taken, target);
                            }
                        }
                    }
                }    
                else
                {
                    for(int i = 0; i < block.data.mixColors.Count; i++)
                    {
                        var mixColor = block.data.mixColors[i];
                        if(mixColor.color == pipe.TopColor)
                        {
                            // Block matches pipe color
                            int takeAmount = pipe.TopValue;
                            int remainingUnits = block.RemainingUnitsColor(mixColor.color);
                            int fillAmount = Mathf.Min(takeAmount, remainingUnits);
                            if (fillAmount > 0)
                            {
                                int taken = pipe.TakeFromTop(fillAmount);
                                bool isComplete = block.FillBlockColor(mixColor.color, taken);
                                if (isComplete) HandleBlockCompleted();
                                PipeView pipeView = GameController.Instance.boardView.GetPipeView(pipeCell);
                                float pourWorldX = pipeView != null ? pipeView.transform.position.x : 0f;
                                BlockView blockView = GameController.Instance.boardView.GetBlockView(blockIndex);
                                if (blockView != null)
                                {
                                    blockView.Pour(taken, isComplete, mixColor.color, pourWorldX);
                                }
                                if (pipeView != null)
                                {
                                    Vector2Int targetCell = GetCellTargetToPour(pipeCell, blockIndex);
                                    Vector3 target = new Vector3(targetCell.x, targetCell.y - 0.5f, 0f);
                                    pipeView.SqueezeFruit(taken, target);
                                }
                            }
                            break;
                        }
                    }
                }    
            }
        }
    }

    private Vector2Int GetCellTargetToPour(Vector2Int pipePos, int blockIndex)
    {
        int currentY = pipePos.y;
        int returnY = pipePos.y;
        for (int i = currentY - 1; i >= 0; i--)
        {
            Vector2Int checkCell = new Vector2Int(pipePos.x, i);
            if(!IsHole(checkCell))
            {
                if(TryGetBlockAt(checkCell, out var bIndex))
                {
                    if(bIndex == blockIndex)
                    {
                        returnY = i;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
        return new Vector2Int(pipePos.x, returnY);
    }

    public Pipe GetPipeAt(Vector2Int cell)
    {
        pipesByPos.TryGetValue(cell, out var pipe);
        return pipe;
    }

    public bool IsHole(Vector2Int cell) => holes.Contains(cell);

    public bool InBounds(Vector2Int cell)
        => cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;

    public bool TryGetBlockAt(Vector2Int cell, out int blockIndex)
        => cellToBlock.TryGetValue(cell, out blockIndex);

    public void SetBusy(bool busy) => IsBusy = busy;

    public void RebuildOccupancy()
    {
        cellToBlock.Clear();
        foreach (var kv in blocks)
        {
            int idx = kv.Key;
            var b = kv.Value;
            var occupied = ShapeLibrary.GetOccupiedCells(b.data);
            for (int i = 0; i < occupied.Length; i++)
                cellToBlock[occupied[i]] = idx;
        }
    }

    public void UpdateBlockOccupancy(int blockIndex, Vector2Int oldPos, Vector2Int newPos)
    {
        // Remove old
        if (!blocks.TryGetValue(blockIndex, out var b))
            return;

        b.data.position = oldPos;
        var oldCells = ShapeLibrary.GetOccupiedCells(b.data);
        foreach (var c in oldCells)
            cellToBlock.Remove(c);

        // Add new
        b.data.position = newPos;
        var newCells = ShapeLibrary.GetOccupiedCells(b.data);


        //foreach (var c in newCells)
        //{
        //    CellView cellView = GameController.Instance.boardView.GetCellView(c);
        //    if (cellView != null)
        //        cellView.DebugColor(Color.yellow);
        //}

        foreach (var c in newCells)
            cellToBlock[c] = blockIndex;


        //foreach(var c in cellToBlock)
        //{
        //    CellView cellView = GameController.Instance.boardView.GetCellView(c.Key);
        //    cellView.blockIndex = c.Value;
        //}
    }

    public void RemoveBlock(int blockIndex)
    {
        if (!blocks.TryGetValue(blockIndex, out var b))
            return;

        var occupied = ShapeLibrary.GetOccupiedCells(b.data);
        foreach (var c in occupied)
            cellToBlock.Remove(c);

        blocks.Remove(blockIndex);
    }
}
