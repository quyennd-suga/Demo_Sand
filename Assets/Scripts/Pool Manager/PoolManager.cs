using UnityEngine;

public sealed class PoolManager : Singleton<PoolManager>
{
    [SerializeField] private PoolRegistry poolRegistry;

    public PoolService Pools { get; private set; }

    protected override void Awake()
    {
        base.Awake(); // ✅ quan trọng

        // Nếu object này bị destroy do duplicate, đừng init service
        if (Instance != this) return;

        Pools = new PoolService(poolRegistry, rootName: "[Pools]", dontDestroyOnLoad: true);
    }

    private void OnDestroy()
    {
        // Chỉ dispose ở instance thật sự
        if (Instance == this)
        {
            Pools?.Dispose();
            Pools = null;
        }
    }

    // API đúng: truyền pos/rot/parent
    public T Spawn<T>(PoolId id, Vector3 pos, Quaternion rot, Transform parent = null) where T : Component
        => Pools.Spawn<T>(id, pos, rot, parent);

    // Tiện: spawn mặc định
    public T Spawn<T>(PoolId id, Transform parent = null) where T : Component
        => Pools.Spawn<T>(id, Vector3.zero, Quaternion.identity, parent);

    public GameObject Spawn(PoolId id, Vector3 pos, Quaternion rot, Transform parent = null)
        => Pools.Spawn(id, pos, rot, parent);

    // Fallback: despawn bằng GameObject
    public void Despawn(GameObject go) => Pools.Despawn(go);

    public void Despawn(PooledHandle handle) => Pools.Despawn(handle);
}
