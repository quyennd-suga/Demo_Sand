using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PooledHandle : MonoBehaviour
{
    internal Action<PooledHandle> ReleaseFn;
    internal Transform Tr;

    private void Awake() => Tr = transform;

    public void Release() => ReleaseFn?.Invoke(this);
}
