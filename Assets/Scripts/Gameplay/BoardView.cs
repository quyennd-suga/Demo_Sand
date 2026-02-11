using System;
using System.Collections.Generic;
using UnityEngine;

public class BoardView : MonoBehaviour
{
    [Header("Transform Parent")]
    public Transform blocksRoot;  // optional
    public Transform pipesRoot;   // optional
    public Transform cellsRoot;   // optional
    public Transform borderRoot;               // optional
    
    public float pipeOffset = 8.5f;



    [Header("Grid")]
    public float cellSize = 1f;
    public float cellOfffset = 0f;
    public float borderOffset = 0f;
    public float cornerOffset = 0f;
    public Vector2 origin = Vector2.zero;

    private readonly Dictionary<int, BlockView> blockViews = new();
    private readonly Dictionary<Vector2Int, PipeView> pipeViews = new();
    private readonly Dictionary<Vector2Int, CellView> cellViews = new();




    public void HandleBlockFull()
    {
        RemoveIceBlocks();
        bool isWin = GameController.board.CheckWinCondition();
        if (isWin)
        {
            GameController.Instance.OnLevelComplete();
            return;
        }
        GameController.board.CheckBlocksOnPipes();
    }    

    public void RemoveIceBlocks()
    {
        foreach (var block in blockViews.Values)
        {
            if(block.iceCount > 0)
            {
                block.iceCount--;
            }
        }
    }    

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(origin.x + cell.x * cellSize, origin.y + cell.y * cellSize, 0f);
    }


    // Vertex on grid lines: vx in [0..width], vy in [0..height]
    private Vector3 VertexToWorld(int vx, int vy, Vector2Int dir)
    {
        float borderX = 0f;
        float borderY = 0f;
        if (dir.y != 0)
        {
            // vertical edge
            if(dir.y < 0)
                borderX = -borderOffset;
            else
                borderX = borderOffset;
        }
        else
        {
            // horizontal edge
            if(dir.x > 0)
                borderY = -borderOffset;
            else
                borderY = borderOffset;
        }
        float wx = origin.x + (vx - cellOfffset + borderX) * cellSize;
        float wy = origin.y + (vy - cellOfffset + borderY) * cellSize;
        return new Vector3(wx, wy, 0);
    }

    //public Vector3 GetCornerOffset(Vector2Int from, Vector2Int to, Vector2Int dir)
    //{
    //    float borderX = 0f;
    //    float borderY = 0f;
    //    if (dir.y != 0)
    //    {
    //        // vertical edge
    //        if (dir.y < 0)
    //            borderX = -borderOffset;
    //    }
    //    else
    //    {
    //        // horizontal edge
    //        if (dir.x > 0)
    //            borderY = -borderOffset;
    //    }
    //}
    private Vector2 GetPipeOffset(Direction dir)
    {
        switch(dir) {
            case Direction.Up:
                return new Vector2(0f, pipeOffset);
            case Direction.Down:
                return new Vector2(0f, -pipeOffset);
            case Direction.Left:
                return new Vector2(-pipeOffset, 0f);
            case Direction.Right:
                return new Vector2(pipeOffset, 0f);
            default:
                return Vector2.zero;
        }
    }
    private float GetPipeRotationZ(Direction dir)
    {
        if(dir == Direction.Up || dir == Direction.Down)
        {
            return 0f;
        }
        else // Left or Right
        {
            return 90f;
        }
    }
    private Vector2 GetBlockOffset(BlockShape shape, float rotation)
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
                    return new Vector2(cellSize, 0f);
                else // 270
                    return new Vector2(cellSize, 0f);
            case BlockShape.TwoSquare:
                return new Vector2(-cellSize / 2f, -cellSize / 2f);
            default:
                return Vector2.zero;
        }
    }
    public void SpawnAll(LevelData level, Board board)
    {
        ClearAll();

        GenerateLevel(level, board);


    }
    
    private void GenerateLevel(LevelData level, Board board)
    {
        BuildCells(board);
        BuildBorders(board);
        SetBoardPosition(board.width, board.height);
        // Spawn pipes 
        foreach (var pipe in board.pipesByPos.Values)
        {
            var view = PoolManager.Instance.Spawn<PipeView>(PoolId.Pipe, pipesRoot);
            Vector2Int pipePos = pipe.position;
            view.transform.localPosition = new Vector3(pipePos.x, pipePos.y, 0f) + (Vector3)GetPipeOffset(pipe.data.direction);
            view.transform.localRotation = Quaternion.Euler(0, 0, GetPipeRotationZ(pipe.data.direction));
            view.SetData(pipe);
            pipeViews[pipePos] = view;
        }

        // Spawn blocks
        foreach (var kv in board.blocks)
        {
            int idx = kv.Key;
            var block = kv.Value;

            BlockShape shape = block.data.shapeType;
            var view = PoolManager.Instance.Spawn<BlockView>(ConvertBlockShapeToPoolId(shape), Vector3.zero, Quaternion.identity, blocksRoot);
            view.SetData(block);
            Vector2Int cellPos = block.data.position;
            view.transform.localPosition = new Vector3(cellPos.x, cellPos.y, 0f) + (Vector3)GetBlockOffset(shape, block.data.rotation);
            view.transform.localRotation = Quaternion.Euler(0, 0, block.data.rotation);
            blockViews[idx] = view;
        }
    }    
    private static PoolId ConvertBlockShapeToPoolId(BlockShape shape)
    {
        switch (shape)
        {
            case BlockShape.One:
                return PoolId.Block_One;

            case BlockShape.Two:
                return PoolId.Block_Two;

            case BlockShape.Three:
                return PoolId.Block_Three;

            case BlockShape.TwoSquare:
                return PoolId.Block_Square;

            case BlockShape.ShortL:
                return PoolId.Block_ShortL;

            case BlockShape.L:
                return PoolId.Block_LongL;

            case BlockShape.ShortT:
                return PoolId.Block_ShortT;

            case BlockShape.Plus:
                return PoolId.Block_Plus;

            case BlockShape.ReverseL:
                return PoolId.Block_ReverseL;


            default:
                Debug.LogWarning($"Unhandled BlockShape: {shape}");
                return PoolId.Block_One;
        }
    }

    private void SetBoardPosition(int width, int height)
    {
        float offsetX = -width * 1f / 2f + cellSize / 2f;
        float offsetY = -height * 1f / 2f + cellSize / 2f;
        transform.localPosition = new Vector3(offsetX, offsetY, 0);
    }    




    public void ClearAll()
    {
        foreach (var v in blockViews.Values)
        {
            if (v)
            {
                v.ResetBlock();
                PoolManager.Instance.Despawn(v.gameObject);
            }
        }    
        foreach (var v in pipeViews.Values) if (v) PoolManager.Instance.Despawn(v.gameObject);
        foreach (var v in cellViews.Values) if (v) PoolManager.Instance.Despawn(v.gameObject);

        blockViews.Clear();
        pipeViews.Clear();
        cellViews.Clear();

        if (borderRoot == null) borderRoot = transform;
        for (int i = borderRoot.childCount - 1; i >= 0; i--)
            PoolManager.Instance.Despawn(borderRoot.GetChild(i).gameObject);
    }

    public BlockView GetBlockView(int index) =>
        blockViews.TryGetValue(index, out var v) ? v : null;

    public PipeView GetPipeView(Vector2Int pos) =>
        pipeViews.TryGetValue(pos, out var v) ? v : null;

    public CellView GetCellView(Vector2Int cell) =>
        cellViews.TryGetValue(cell, out var v) ? v : null;

    public void RemoveBlockView(int index)
    {
        if (!blockViews.TryGetValue(index, out var view)) return;
        blockViews.Remove(index);
        PoolManager.Instance.Despawn(view.gameObject);
    }

    // ---------------- Cells ----------------

    private void BuildCells(Board board)
    {

        for (int y = 0; y < board.height; y++)
            for (int x = 0; x < board.width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                bool isHole = board.IsHole(cell);

                
                Vector3 worldPos = CellToWorld(cell);
                Quaternion rot = new Quaternion(0f, 180f, 0f, 0f);
                CellView view = PoolManager.Instance.Spawn<CellView>(PoolId.Cell, worldPos, rot, cellsRoot);
                view.Init(cell, isHole);
                view.transform.localPosition = worldPos;
                cellViews[cell] = view;
            }
    }

    // ---------------- Borders (wrap holes) ----------------

    private bool IsSolid(Board board, int x, int y)
    {
        if (x < 0 || y < 0 || x >= board.width || y >= board.height) return false;
        return !board.IsHole(new Vector2Int(x, y));
    }

    private struct Edge
    {
        public Vector2Int a; // vertex
        public Vector2Int b; // vertex
        public Edge(Vector2Int a, Vector2Int b) { this.a = a; this.b = b; }
        public Vector2Int Dir => b - a; // one of (1,0),(0,1),(-1,0),(0,-1)
    }

    private void BuildBorders(Board board)
    {

        if (borderRoot == null) borderRoot = transform;

        var edges = BuildBoundaryEdges(board);
        var loops = BuildLoops(edges);

        SpawnBordersFromLoops(loops);
    }

    // 1) Collect boundary edges between solid and empty/outside
    private List<Edge> BuildBoundaryEdges(Board board)
    {
        var edges = new List<Edge>(board.width * board.height);

        for (int y = 0; y < board.height; y++)
            for (int x = 0; x < board.width; x++)
            {
                if (!IsSolid(board, x, y)) continue;

                // Bottom neighbor empty => edge along bottom of cell: (x,y) -> (x+1,y)
                if (!IsSolid(board, x, y - 1))
                    edges.Add(new Edge(new Vector2Int(x, y), new Vector2Int(x + 1, y)));

                // Top neighbor empty => edge along top of cell: (x+1,y+1) -> (x,y+1)
                if (!IsSolid(board, x, y + 1))
                    edges.Add(new Edge(new Vector2Int(x + 1, y + 1), new Vector2Int(x, y + 1)));

                // Left neighbor empty => edge along left: (x,y+1) -> (x,y)
                if (!IsSolid(board, x - 1, y))
                    edges.Add(new Edge(new Vector2Int(x, y + 1), new Vector2Int(x, y)));

                // Right neighbor empty => edge along right: (x+1,y) -> (x+1,y+1)
                if (!IsSolid(board, x + 1, y))
                    edges.Add(new Edge(new Vector2Int(x + 1, y), new Vector2Int(x + 1, y + 1)));
            }

        return edges;
    }

    // 2) Build loops by walking edges using adjacency
    private List<List<Edge>> BuildLoops(List<Edge> edges)
    {
        // vertex -> edge indices starting at vertex
        var adj = new Dictionary<Vector2Int, List<int>>(1024);
        for (int i = 0; i < edges.Count; i++)
        {
            if (!adj.TryGetValue(edges[i].a, out var list))
                adj[edges[i].a] = list = new List<int>(2);
            list.Add(i);
        }

        var used = new bool[edges.Count];
        var loops = new List<List<Edge>>();

        for (int i = 0; i < edges.Count; i++)
        {
            if (used[i]) continue;

            var loop = new List<Edge>(64);

            int curIdx = i;
            var cur = edges[curIdx];
            used[curIdx] = true;
            loop.Add(cur);

            Vector2Int startV = cur.a;
            Vector2Int curV = cur.b;
            Vector2Int prevDir = cur.Dir;

            // walk until return to start vertex
            while (curV != startV)
            {
                if (!adj.TryGetValue(curV, out var candidates))
                    break;

                int nextIdx = -1;
                int bestScore = int.MinValue;

                for (int k = 0; k < candidates.Count; k++)
                {
                    int ei = candidates[k];
                    if (used[ei]) continue;

                    var dir = edges[ei].Dir;
                    int score = TurnScore(prevDir, dir);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        nextIdx = ei;
                    }
                }

                if (nextIdx < 0) break;

                curIdx = nextIdx;
                cur = edges[curIdx];
                used[curIdx] = true;
                loop.Add(cur);

                prevDir = cur.Dir;
                curV = cur.b;
            }

            loops.Add(loop);
        }

        return loops;
    }

    // Prefer right turn > straight > left > back
    private int TurnScore(Vector2Int prevDir, Vector2Int dir)
    {
        int dot = prevDir.x * dir.x + prevDir.y * dir.y;
        if (dot == -1) return 0; // back

        int cross = prevDir.x * dir.y - prevDir.y * dir.x; // z of cross
        if (dot == 1) return 2;       // straight
        if (cross < 0) return 3;      // right turn (depending on coord system)
        return 1;                     // left
    }

    private Vector2 CornerOffsetWorld(Vector2Int from, Vector2Int to)
    {
        bool Connects(Vector2Int a, Vector2Int b, Vector2Int u, Vector2Int v) =>
            (a == u && b == v);

        if (Connects(from, to, Vector2Int.right, Vector2Int.down)) return new Vector2(0, -cornerOffset - borderOffset - borderOffset);
        if (Connects(from, to, Vector2Int.right, Vector2Int.up)) return new Vector2(-cornerOffset - borderOffset, -borderOffset);
        if (Connects(from, to, Vector2Int.left, Vector2Int.up)) return new Vector2(0, cornerOffset + borderOffset + borderOffset);
        if (Connects(from, to, Vector2Int.left, Vector2Int.down)) return new Vector2(cornerOffset + borderOffset, borderOffset);

        if (Connects(from, to, Vector2Int.down, Vector2Int.right)) return new Vector2(-borderOffset, cornerOffset + borderOffset);
        if (Connects(from, to, Vector2Int.up, Vector2Int.right)) return new Vector2(cornerOffset + borderOffset + borderOffset, 0);
        if (Connects(from, to, Vector2Int.up, Vector2Int.left)) return new Vector2(borderOffset, -cornerOffset - borderOffset);
        if (Connects(from, to, Vector2Int.down, Vector2Int.left)) return new Vector2(-cornerOffset - borderOffset - borderOffset, 0);

        return Vector2.zero;
    }

    // right turn = inner (nếu thấy inner bị ngược thì đổi cross < 0 thành cross > 0)
private static bool IsInnerCorner(Vector2Int from, Vector2Int to)
{
    int cross = from.x * to.y - from.y * to.x;
    return cross < 0;
}

    private void SpawnBordersFromLoops(List<List<Edge>> loops)
    {
        // clear old
        for (int i = borderRoot.childCount - 1; i >= 0; i--)
            PoolManager.Instance.Despawn(borderRoot.GetChild(i).gameObject);



        foreach (var loop in loops)
        {
            if (loop == null || loop.Count == 0) continue;

            int n = loop.Count;

            var segs = new List<Transform>(n);
            var dirs = new List<Vector2Int>(n);
            var waList = new List<Vector3>(n);
            var wbList = new List<Vector3>(n);

            // PASS 1: Spawn segments full length
            for (int i = 0; i < n; i++)
            {
                var e = loop[i];
                var dir = e.Dir;

                Vector3 wa = VertexToWorld(e.a.x, e.a.y, dir);
                Vector3 wb = VertexToWorld(e.b.x, e.b.y, dir);
                Vector3 mid = (wa + wb) * 0.5f;

                var seg = PoolManager.Instance.Spawn(PoolId.Border_Straight, mid, Quaternion.identity, borderRoot);
                seg.transform.localPosition = mid;

                // Straight prefab default is VERTICAL (length along local Y)
                if (dir.y != 0) seg.transform.rotation = Quaternion.identity;
                else seg.transform.rotation = Quaternion.Euler(0, 0, 90f);

                // Full length
                var ls = seg.transform.localScale;
                seg.transform.localScale = new Vector3(ls.x, cellSize, ls.z);

                segs.Add(seg.transform);
                dirs.Add(dir);
                waList.Add(wa);
                wbList.Add(wb);
            }

            // PASS 1.5: Detect corners & compute REAL corner world pos (with offset)
            bool[] cornerAtV = new bool[n];        // corner tại vertex a của edge i
            bool[] innerCornerAtV = new bool[n];   // inner corner tại vertex a của edge i
            bool[] lockMid = new bool[n];          // segment này bị khoá midpoint => không push

            Vector3[] cornerWorldAtV = new Vector3[n]; // position thật của corner prefab
            bool[] hasCornerWorld = new bool[n];

            for (int i = 0; i < n; i++)
            {
                int prev = (i - 1 + n) % n;
                Vector2Int prevDir = dirs[prev];
                Vector2Int dir = dirs[i];

                cornerAtV[i] = (dir != prevDir);
                innerCornerAtV[i] = cornerAtV[i] && IsInnerCorner(prevDir, dir);

                if (cornerAtV[i])
                {
                    Vector3 wa = waList[i];
                    cornerWorldAtV[i] = wa + (Vector3)CornerOffsetWorld(prevDir, dir);
                    hasCornerWorld[i] = true;
                }
            }

            // Apply rule: segment between two corners
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;

                bool betweenTwoCorners = cornerAtV[i] && cornerAtV[next];
                if (!betweenTwoCorners) continue;

                bool iIsOuter = cornerAtV[i] && !innerCornerAtV[i];
                bool nextIsOuter = cornerAtV[next] && !innerCornerAtV[next];

                // giữa 2 inner corner -> hide
                if (innerCornerAtV[i] && innerCornerAtV[next])
                {
                    segs[i].gameObject.SetActive(false);
                    continue;
                }

                // ✅ NEW: giữa 2 outer corner -> scaleY=0.8, giữ nguyên position, và không push
                if (iIsOuter && nextIsOuter)
                {
                    var s = segs[i].localScale;
                    segs[i].localScale = new Vector3(s.x, cellSize * 0.8f, s.z);

                    // giữ nguyên position (đang là mid ở PASS 1)
                    lockMid[i] = true;
                    continue;
                }

                // còn lại (giữa 2 corner nhưng không phải 2 inner / 2 outer)
                {
                    var s = segs[i].localScale;
                    segs[i].localScale = new Vector3(s.x, cellSize * 0.4f, s.z);

                    if (hasCornerWorld[i] && hasCornerWorld[next])
                    {
                        segs[i].localPosition = (cornerWorldAtV[i] + cornerWorldAtV[next]) * 0.5f;
                        lockMid[i] = true;
                    }
                    else
                    {
                        segs[i].localPosition = (waList[i] + wbList[i]) * 0.5f;
                        lockMid[i] = true;
                    }
                }
            }


            // PASS 2: Spawn corner prefab + push segments near corners
            for (int i = 0; i < n; i++)
            {
                int prev = (i - 1 + n) % n;

                Vector2Int prevDir = dirs[prev];
                Vector2Int dir = dirs[i];

                if (dir == prevDir) continue; // not a corner

                bool isInner = innerCornerAtV[i];
                bool isOuter = cornerAtV[i] && !innerCornerAtV[i];
                // spawn corner at computed real position
                var corner = PoolManager.Instance.Spawn(isInner ? PoolId.Inner_Corner : PoolId.Outer_Corner, cornerWorldAtV[i], Quaternion.Euler(0, 0, CornerRotationZ(prevDir, dir)), borderRoot);
                corner.transform.localPosition = cornerWorldAtV[i];
                corner.transform.rotation = Quaternion.Euler(0, 0, CornerRotationZ(prevDir, dir));

                var cs = corner.transform.localScale;
                corner.transform.localScale = new Vector3(cellSize, cellSize, cs.z);

                // Optional: keep your debug naming behavior
                CornerOffsetWorld(prevDir, dir, corner);

                // ✅ Push rule
                // inner corner -> push 0.4 * cellSize
                // outer corner -> push 0.1 * cellSize
                

                float push = 0f;
                if (isInner) push = 0.5f * cellSize;
                else if (isOuter) push = 0.1f * cellSize;

                if (push <= 0f) continue;

                // prev segment has corner at its END (vertex b) => move away = -prevDir
                if (segs[prev].gameObject.activeSelf && !lockMid[prev])
                    segs[prev].localPosition += new Vector3(-prevDir.x, -prevDir.y, 0f) * push;

                // current segment has corner at its START (vertex a) => move away = +dir
                if (segs[i].gameObject.activeSelf && !lockMid[i])
                    segs[i].localPosition += new Vector3(dir.x, dir.y, 0f) * push;
            }

        }
    }


    // IMPORTANT: Adjust mapping once according to your corner sprite's default orientation.
    private float CornerRotationZ(Vector2Int from, Vector2Int to)
    {
        // Your corner default connects LEFT + DOWN (looks like "┐"):
        // arms along Vector2Int.left and Vector2Int.down at the vertex.

        bool Connects(Vector2Int a, Vector2Int b, Vector2Int u, Vector2Int v) =>
            (a == u && b == v);

        // 0° : Left + Down
        if (Connects(from, to, Vector2Int.right, Vector2Int.down)) return 0;

        // 90° : Down + Right
        if (Connects(from, to, Vector2Int.right, Vector2Int.up)) return -90f;

        // 180° : Right + Up
        if (Connects(from, to, Vector2Int.left, Vector2Int.up)) return -180f;

        // 270° : Up + Left
        if (Connects(from, to, Vector2Int.left, Vector2Int.down)) return 90f;

        if(Connects(from, to, Vector2Int.down, Vector2Int.right)) return 180f;
        if(Connects(from, to, Vector2Int.up, Vector2Int.right)) return 90f;
        if(Connects(from, to, Vector2Int.up, Vector2Int.left)) return 0f;
        if(Connects(from, to, Vector2Int.down, Vector2Int.left)) return -90f;

        return 0f;
    }
    private Vector2 CornerOffsetWorld(Vector2Int from, Vector2Int to, GameObject corner)
    {
        bool Connects(Vector2Int a, Vector2Int b, Vector2Int u, Vector2Int v) =>
            (a == u && b == v);

        // 0° : Left + Down
        corner.name = "Corner_RightDown";
        if (Connects(from, to, Vector2Int.right, Vector2Int.down)) return new Vector2(0, -cornerOffset - borderOffset - borderOffset);
        corner.name = "Corner_RightUp";
        // 90° : Down + Right
        if (Connects(from, to, Vector2Int.right, Vector2Int.up)) return new Vector2(-cornerOffset - borderOffset,  -borderOffset);
        corner.name = "Corner_LeftUp";
        // 180° : Right + Up
        if (Connects(from, to, Vector2Int.left, Vector2Int.up)) return new Vector2(0, cornerOffset + borderOffset + borderOffset);
        corner.name = "Corner_LeftDown";
        // 270° : Up + Left
        if (Connects(from, to, Vector2Int.left, Vector2Int.down)) return new Vector2(cornerOffset + borderOffset, borderOffset);
        corner.name = "Corner_DownRight";
        if (Connects(from, to, Vector2Int.down, Vector2Int.right)) return new Vector2(-borderOffset ,cornerOffset + borderOffset);
        corner.name = "Corner_UpRight";
        if (Connects(from, to, Vector2Int.up, Vector2Int.right)) return new Vector2(cornerOffset + borderOffset + borderOffset, 0);
        corner.name = "Corner_UpLeft";
        if (Connects(from, to, Vector2Int.up, Vector2Int.left)) return new Vector2(borderOffset, -cornerOffset - borderOffset);
        corner.name = "Corner_DownLeft";
        if (Connects(from, to, Vector2Int.down, Vector2Int.left)) return new Vector2(-cornerOffset - borderOffset - borderOffset, 0);

        return Vector2.zero;
    }

}
