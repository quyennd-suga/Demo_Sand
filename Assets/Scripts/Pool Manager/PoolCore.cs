using System.Collections.Generic;
using UnityEngine;

internal sealed class PoolCore
{
    internal readonly GameObject Prefab;
    internal readonly Transform Root;
    private readonly Stack<PooledHandle> stack;

    public PoolCore(GameObject prefab, Transform root, int initialCapacity)
    {
        Prefab = prefab;
        Root = root;
        stack = new Stack<PooledHandle>(initialCapacity);
    }

    public void WarmupHandles(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var h = CreateNew();
            h.gameObject.SetActive(false);
            stack.Push(h);
        }
    }

    public PooledHandle GetHandle(Vector3 pos, Quaternion rot, Transform parent)
    {
        var h = stack.Count > 0 ? stack.Pop() : CreateNew();

        h.Tr.SetParent(parent, false);
        h.Tr.SetPositionAndRotation(pos, rot);
        h.gameObject.SetActive(true);

        return h;
    }

    public void ReleaseHandle(PooledHandle h)
    {
        h.gameObject.SetActive(false);
        h.Tr.SetParent(Root, false);
        stack.Push(h);
    }

    private PooledHandle CreateNew()
    {
        var go = Object.Instantiate(Prefab, Root);
        go.name = Prefab.name;

        var h = go.GetComponent<PooledHandle>() ?? go.AddComponent<PooledHandle>();
        return h;
    }
}
