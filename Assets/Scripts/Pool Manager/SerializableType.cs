using System;
using UnityEngine;

[Serializable]
public struct SerializableType
{
    [SerializeField] private string assemblyQualifiedName;

    [NonSerialized] private Type cachedType;

    public Type Type
    {
        get
        {
            if (cachedType != null) return cachedType;
            if (string.IsNullOrEmpty(assemblyQualifiedName)) return null;

            cachedType = System.Type.GetType(assemblyQualifiedName);
            return cachedType;
        }
    }

    public string AssemblyQualifiedName => assemblyQualifiedName;

    public void Set(Type t)
    {
        cachedType = t;
        assemblyQualifiedName = t != null ? t.AssemblyQualifiedName : string.Empty;
    }

    public override string ToString() => assemblyQualifiedName;
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class TypeDropdownAttribute : PropertyAttribute
{
    public readonly Type BaseType;
    public readonly bool IncludeAbstract;

    public TypeDropdownAttribute(Type baseType, bool includeAbstract = false)
    {
        BaseType = baseType;
        IncludeAbstract = includeAbstract;
    }
}
