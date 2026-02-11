using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Pooling/Pool Registry")]
public sealed class PoolRegistry : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public PoolId id;
        public GameObject prefab;

        [TypeDropdown(typeof(Component))]
        public SerializableType mainType;

        [Min(0)] public int warmup;
        [Min(1)] public int initialCapacity;
    }

    public Entry[] entries;
}
