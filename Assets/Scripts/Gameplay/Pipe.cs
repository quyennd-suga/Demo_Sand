using System.Collections.Generic;
using UnityEngine;

public class Pipe
{
    public Vector2Int position;
    public PipeData data;

    // Queue-like list: index 0 is gate
    private List<WaterColor> queue = new();

    public Pipe(PipeData data)
    {
        this.data = data;
        position = data.position;

        // copy to runtime queue
        foreach (var wc in data.waterColors)
            queue.Add(new WaterColor(wc.color, wc.value));
    }

    public bool IsBlocked()
    {
        if (data.isLocked) return true;
        if (data.hasOpenClose && data.isClosed) return true;
        if (data.iceCount > 0) return true;
        return false;
    }

    public bool HasWater => queue.Count > 0 && queue[0].value > 0;

    public int TopColor => queue.Count > 0 ? queue[0].color : -1;
    public int TopValue => queue.Count > 0 ? queue[0].value : 0;

    /// Take water ONLY from top color
    public int TakeFromTop(int amount)
    {
        if (queue.Count == 0) return 0;
        var top = queue[0];
        if (top.value <= 0) return 0;

        int take = Mathf.Min(top.value, amount);
        top.value -= take;
        queue[0] = top;

        // If empty -> pop
        if (queue[0].value <= 0)
            queue.RemoveAt(0);

        return take;
    }
}
