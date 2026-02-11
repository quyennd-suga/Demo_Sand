using System.Collections.Generic;
using UnityEngine;

public class PipeView : MonoBehaviour
{
    [SerializeField] private Transform pourOrigin; // where water comes out

    [Header("Layout")]
    [SerializeField] private float pipeOffset = 0.85f;
    [SerializeField] private float posOffset = 7.5f;

    private readonly Queue<Fruit> spawnedFruits = new Queue<Fruit>();

    private Vector2Int pipePos;

    private Direction direction;

    // Optional: nếu muốn clear cả machine theo cách chắc chắn hơn
    // (vì hiện tại code cũ không lưu reference machine)
    [SerializeField] private bool clearAllChildrenOnSetData = false;

    public FruitMachine machine;

    public void SetData(Pipe pipe)
    {
        ClearSpawned();
        direction = pipe.data.direction;
        pipePos = pipe.data.position;
        RefreshTopColor();
        SpawnFruits(pipe);
    }

    private void ClearSpawned()
    {
        // despawn fruits đang giữ trong queue
        while (spawnedFruits.Count > 0)
        {
            var f = spawnedFruits.Dequeue();
            if (f != null)
                PoolManager.Instance.Despawn(f.gameObject);
        }
    }

    private void SpawnFruits(Pipe pipe)
    {
        var parent = transform.parent;

        // Dùng localPosition cho đồng nhất: spawn -> squeeze đều dùng local
        Vector3 originLocal = transform.localPosition;
        originLocal.z = 0f;

        Vector3 step = GetPipeOffset(direction);              // bước xếp trái/phải/lên/xuống
        Vector3 startLocal = originLocal - GetPositionOffset(direction); // điểm bắt đầu xếp

        float rotationZ = GetRotationAngle(direction);

        int index = 0;
         // cache để giảm truy cập singleton nhiều lần
        var pool = PoolManager.Instance;

        machine = pool.Spawn<FruitMachine>(PoolId.Machine, parent);

        for(int i = 0; i < pipe.data.waterColors.Count; i++)
        {
            
            WaterColor waterColor = pipe.data.waterColors[i];
            Sprite sprite = DataContainer.Instance.fruitData.GetFruitSprite((ColorEnum)waterColor.color);
            if (i == 0)
            {


                Color color = DataContainer.Instance.blockColorData.GetColor((ColorEnum)waterColor.color);
                machine.SetColor(color);
                machine.Config(direction);
                machine.transform.localPosition = new Vector3(startLocal.x, startLocal.y, 0.35f);
                machine.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            }

            for (int id = 0; id < waterColor.value; id++)
            {
                index++;

                Fruit fruit = pool.Spawn<Fruit>(PoolId.Fruit, parent);
                fruit.ConfigFruit(sprite, waterColor.color);

                // IMPORTANT: dùng localPosition vì parent != null
                fruit.transform.localPosition = startLocal + step * index;

                spawnedFruits.Enqueue(fruit);

                
            }
        }
    }

    private Vector3 GetPipeOffset(Direction dir)
    {
        // switch expression gọn + nhanh hơn switch dài
        return dir switch
        {
            Direction.Up => new Vector3(0f, pipeOffset, 0f),
            Direction.Down => new Vector3(0f, -pipeOffset, 0f),
            Direction.Left => new Vector3(-pipeOffset, 0f, 0f),
            Direction.Right => new Vector3(pipeOffset, 0f, 0f),
            _ => Vector3.zero
        };
    }

    private Vector3 GetPositionOffset(Direction dir)
    {
        return dir switch
        {
            Direction.Up => new Vector3(0f, posOffset, 0f),
            Direction.Down => new Vector3(0f, -posOffset, 0f),
            Direction.Left => new Vector3(-posOffset, 0f, 0f),
            Direction.Right => new Vector3(posOffset, 0f, 0f),
            _ => Vector3.zero
        };
    }

    private float GetRotationAngle(Direction dir)
    {
        return dir switch
        {
            Direction.Up => 180f,
            Direction.Right => 90f,
            Direction.Down => 0f,
            Direction.Left => -90f,
            _ => 0f
        };
    }

    public Vector3 GetPourOriginWorld()
    {
        return pourOrigin != null ? pourOrigin.position : transform.position;
    }

    public void RefreshTopColor()
    {
        // TODO: update sprite color of gate to pipe.TopColor
    }

    /// <summary>
    /// amount quả đầu bị squeeze-out (despawn),
    /// phần còn lại shift lên amount bước để lấp chỗ trống.
    /// </summary>
    public void SqueezeFruit(int amount, Vector3 target)
    {
        if (amount <= 0 || spawnedFruits.Count == 0) return;
        Color fruitColor = DataContainer.Instance.blockColorData.GetColor((ColorEnum)spawnedFruits.Peek().colorId);
        
        // Truyền số lượng fruit để tính duration chính xác
        machine.PlayWaterStream(fruitColor, amount);

        int removeCount = Mathf.Min(amount, spawnedFruits.Count);
        Vector3 offset = GetPipeOffset(direction);

        // 1) Remove first N fruits (animate out + despawn)
        for (int i = 0; i < removeCount; i++)
        {
            var fruit = spawnedFruits.Dequeue();
            if (fruit != null)
                fruit.SqueezeOut(offset, i + 1); // i dùng để tạo feeling "xếp tầng" nếu muốn
        }

        // 2) Shift remaining fruits by removeCount steps

        
        foreach (var fruit in spawnedFruits)
        {
            if (fruit != null)
                fruit.Shift(offset, removeCount);
        }
        int nextColorId = spawnedFruits.Count > 0 ? spawnedFruits.Peek().colorId : -1;
        Color nextColor = nextColorId >= 0 ? DataContainer.Instance.blockColorData.GetColor((ColorEnum)nextColorId) : Color.white;
        machine.RefreshMachineColor(nextColor, removeCount * GlobalValues.fruitSqueezeOutDuration);
    }
}
