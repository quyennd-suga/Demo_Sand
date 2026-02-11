using UnityEngine;

public class CellView : MonoBehaviour
{
    public int blockIndex = -1;
    public Vector2Int Cell;
    public bool IsHole { get; private set; }

    /// <summary>
    /// Init cell view với dữ liệu board
    /// </summary>
    public void Init(Vector2Int cell, bool isHole)
    {
        Cell = cell;
        IsHole = isHole;

        // Tuỳ bạn: đổi màu hoặc sprite
        gameObject.SetActive(!isHole);  
    }

    public void DebugColor(Color color)
    {
        var sr = GetComponent<MeshRenderer>();
        if (sr != null)
            sr.material.color = color;
    }
}
