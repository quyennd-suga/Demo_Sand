using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PoolService : IDisposable
{
    private readonly Transform root;

    // runtime mapping
    private readonly Dictionary<PoolId, Type> idToType = new(64);
    private readonly Dictionary<PoolId, PoolCore> idToCore = new(64);
    private readonly Dictionary<PoolId, ISpawner> idToSpawner = new(64);

    // fallback despawn(go) không GetComponent
    private readonly Dictionary<int, PooledHandle> idToHandle = new(2048);

    public PoolService(PoolRegistry registry, string rootName = "[Pools]", bool dontDestroyOnLoad = true)
    {
        if (registry == null) throw new ArgumentNullException(nameof(registry));

        var go = new GameObject(rootName);
        root = go.transform;
        if (dontDestroyOnLoad) UnityEngine.Object.DontDestroyOnLoad(go);

        // Build pools
        foreach (var e in registry.entries)
        {
            if (e.prefab == null) throw new ArgumentException($"PoolId {e.id} missing prefab");

            if (idToCore.ContainsKey(e.id))
                throw new ArgumentException($"Duplicate PoolId in registry: {e.id}");

            var poolRoot = new GameObject(e.id + "_Pool").transform;
            poolRoot.SetParent(root, false);

            var core = new PoolCore(e.prefab, poolRoot, Math.Max(1, e.initialCapacity));
            core.WarmupHandles(Math.Max(0, e.warmup));

            idToCore[e.id] = core;

            // ===== IMPORTANT CHANGE =====
            // mainType giờ OPTIONAL:
            // - Nếu có mainType hợp lệ => hỗ trợ Spawn<T>() (typed spawner)
            // - Nếu không có => vẫn Spawn(id) trả GameObject bình thường
            var t = e.mainType.Type;
            if (t != null)
            {
                if (!typeof(Component).IsAssignableFrom(t))
                    throw new ArgumentException($"PoolId {e.id} mainType must be Component, got {t.Name}");

                idToType[e.id] = t;

                // typed spawner cached per id
                var spawner = CreateTypedSpawner(t, core, prebindCount: Math.Max(0, e.warmup), RegisterHandle);
                idToSpawner[e.id] = spawner;
            }
        }
    }

    /// ✅ Bắt buộc typeof(T) đúng với mainType của id (nếu id có khai báo mainType)
    public T Spawn<T>(PoolId id, Vector3 pos, Quaternion rot, Transform parent = null) where T : Component
    {
        if (!idToType.TryGetValue(id, out var expected))
            throw new ArgumentException($"PoolId {id} has no mainType. Use Spawn(id) to get GameObject, or set mainType in registry.");

        if (expected != typeof(T))
            throw new ArgumentException($"PoolId {id} bound to {expected.Name}, but requested {typeof(T).Name}");

        if (!idToSpawner.TryGetValue(id, out var sp))
            throw new ArgumentException($"PoolId {id} missing typed spawner. Check registry warmup/mainType.");

        var spawner = (Spawner<T>)sp;
        return spawner.Spawn(pos, rot, parent);
    }

    // ✅ Spawn chỉ GameObject, không phụ thuộc Type
    public GameObject Spawn(PoolId id, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (!idToCore.TryGetValue(id, out var core))
            throw new ArgumentException($"Pool not found: {id}");

        var h = core.GetHandle(pos, rot, parent);
        RegisterHandle(h);
        return h.gameObject;
    }

    /// Fast path: nếu bạn có handle
    public void Despawn(PooledHandle handle) => handle.Release();

    /// Fallback: chỉ có GameObject => vẫn không GetComponent
    public void Despawn(GameObject go)
    {
        int instId = go.GetInstanceID();
        if (idToHandle.TryGetValue(instId, out var h))
        {
            h.Release();
            return;
        }
        UnityEngine.Object.Destroy(go);
    }

    private void RegisterHandle(PooledHandle h)
    {
        idToHandle[h.gameObject.GetInstanceID()] = h;
    }

    public void Dispose()
    {
        if (root != null) UnityEngine.Object.Destroy(root.gameObject);
        idToType.Clear();
        idToCore.Clear();
        idToSpawner.Clear();
        idToHandle.Clear();
    }

    // ===== Internals =====

    private interface ISpawner
    {
        Component SpawnAsComponent(Vector3 pos, Quaternion rot, Transform parent);
    }

    /// Spawner<T> caches Record(handle + T + IPoolable). Hot-path không GetComponent.
    private sealed class Spawner<T> : ISpawner where T : Component
    {
        private readonly PoolCore core;
        private readonly Action<PooledHandle> registerHandle;

        private readonly Stack<Record> cache;

        private sealed class Record
        {
            public PooledHandle h;
            public T t;
            public IPoolable p;
        }

        public Spawner(PoolCore core, int initialCapacityForCache, int prebindCount, Action<PooledHandle> registerHandle)
        {
            this.core = core;
            this.registerHandle = registerHandle;
            cache = new Stack<Record>(Math.Max(16, initialCapacityForCache));

            // Prebind: tạo record typed sẵn theo warmup để gameplay "zero GetComponent"
            for (int i = 0; i < prebindCount; i++)
            {
                var h = core.GetHandle(Vector3.zero, Quaternion.identity, core.Root);
                registerHandle?.Invoke(h);

                var t = h.GetComponent<T>();
                if (t == null)
                    throw new ArgumentException($"Prefab '{core.Prefab.name}' missing component {typeof(T).Name}");

                var p = t as IPoolable;

                var r = new Record { h = h, t = t, p = p };
                h.ReleaseFn = _ => Release(r);

                h.gameObject.SetActive(false);
                h.Tr.SetParent(core.Root, false);
                h.Tr.localPosition = Vector3.zero;
                h.Tr.localRotation = Quaternion.identity;

                cache.Push(r);
            }
        }

        public T Spawn(Vector3 pos, Quaternion rot, Transform parent)
        {
            if (cache.Count > 0)
            {
                var r = cache.Pop();

                r.h.Tr.SetParent(parent, false);
                r.h.Tr.SetPositionAndRotation(pos, rot);
                r.h.gameObject.SetActive(true);

                r.p?.OnSpawned();
                return r.t;
            }

            var h2 = core.GetHandle(pos, rot, parent);
            registerHandle?.Invoke(h2);

            var t2 = h2.GetComponent<T>();
            if (t2 == null)
                throw new ArgumentException($"Prefab '{core.Prefab.name}' missing component {typeof(T).Name}");

            var p2 = t2 as IPoolable;

            var r2 = new Record { h = h2, t = t2, p = p2 };
            h2.ReleaseFn = _ => Release(r2);

            p2?.OnSpawned();
            return t2;
        }

        private void Release(Record r)
        {
            r.p?.OnDespawned();

            r.h.gameObject.SetActive(false);
            r.h.Tr.SetParent(core.Root, false);
            r.h.Tr.localPosition = Vector3.zero;
            r.h.Tr.localRotation = Quaternion.identity;

            cache.Push(r);
        }

        public Component SpawnAsComponent(Vector3 pos, Quaternion rot, Transform parent) => Spawn(pos, rot, parent);
    }

    private static ISpawner CreateTypedSpawner(Type t, PoolCore core, int prebindCount, Action<PooledHandle> registerHandle)
    {
        var spawnerType = typeof(Spawner<>).MakeGenericType(t);
        return (ISpawner)Activator.CreateInstance(spawnerType, core, prebindCount, prebindCount, registerHandle);
    }
}
