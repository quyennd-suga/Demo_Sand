#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SerializableType))]
public sealed class SerializableTypeDrawer : PropertyDrawer
{
    private static readonly Dictionary<(Type baseType, bool includeAbstract), Type[]> Cache = new();
    private static readonly Dictionary<(Type baseType, bool includeAbstract), string[]> NameCache = new();
    private static readonly Dictionary<string, string> SearchByPropertyPath = new();

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight * 2f + 4f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var aqnProp = property.FindPropertyRelative("assemblyQualifiedName");

        var filter = fieldInfo != null
            ? (TypeDropdownAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(TypeDropdownAttribute))
            : null;

        var baseType = filter?.BaseType ?? typeof(UnityEngine.Object);
        var includeAbstract = filter?.IncludeAbstract ?? false;

        EnsureCache(baseType, includeAbstract, out var types, out var displayNames);

        var line1 = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        var line2 = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 4f, position.width, EditorGUIUtility.singleLineHeight);

        EditorGUI.BeginProperty(position, label, property);

        var dropdownRect = EditorGUI.PrefixLabel(line1, label);
        DrawDropdown(dropdownRect, property.propertyPath, aqnProp, types, displayNames);

        DrawSearch(line2, property.propertyPath);

        EditorGUI.EndProperty();
    }

    private void DrawDropdown(Rect rect, string propertyPath, SerializedProperty aqnProp, Type[] types, string[] displayNames)
    {
        int currentIndex = IndexOfAqn(types, aqnProp.stringValue);

        string search = GetSearch(propertyPath);
        int[] filteredIndices;
        string[] filteredNames;

        if (string.IsNullOrWhiteSpace(search))
        {
            filteredIndices = Enumerable.Range(0, types.Length).ToArray();
            filteredNames = displayNames;
        }
        else
        {
            var hits = new List<int>(64);
            for (int i = 0; i < displayNames.Length; i++)
            {
                if (displayNames[i].IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    hits.Add(i);
            }

            if (!hits.Contains(0)) hits.Insert(0, 0); // keep None
            filteredIndices = hits.ToArray();
            filteredNames = hits.Select(i => displayNames[i]).ToArray();
        }

        int filteredPos = Array.IndexOf(filteredIndices, currentIndex);
        if (filteredPos < 0) filteredPos = 0;

        int newFilteredPos = EditorGUI.Popup(rect, filteredPos, filteredNames);
        int newIndex = filteredIndices[Mathf.Clamp(newFilteredPos, 0, filteredIndices.Length - 1)];

        if (newIndex != currentIndex)
        {
            aqnProp.stringValue = newIndex == 0 ? string.Empty : types[newIndex].AssemblyQualifiedName;
            aqnProp.serializedObject.ApplyModifiedProperties();
        }
    }

    private void DrawSearch(Rect rect, string propertyPath)
    {
        var rLabel = new Rect(rect.x, rect.y, 50f, rect.height);
        var rField = new Rect(rect.x + 50f, rect.y, rect.width - 80f, rect.height);
        var rClear = new Rect(rect.x + rect.width - 25f, rect.y, 25f, rect.height);

        EditorGUI.LabelField(rLabel, "Search");

        string search = GetSearch(propertyPath);
        string newSearch = EditorGUI.TextField(rField, search);

        if (GUI.Button(rClear, "X")) newSearch = string.Empty;
        if (newSearch != search) SearchByPropertyPath[propertyPath] = newSearch;
    }

    private static string GetSearch(string propertyPath)
        => SearchByPropertyPath.TryGetValue(propertyPath, out var s) ? s : string.Empty;

    private static int IndexOfAqn(Type[] types, string aqn)
    {
        if (string.IsNullOrEmpty(aqn)) return 0;
        for (int i = 1; i < types.Length; i++)
        {
            if (string.Equals(types[i].AssemblyQualifiedName, aqn, StringComparison.Ordinal))
                return i;
        }
        return 0;
    }

    private static void EnsureCache(Type baseType, bool includeAbstract, out Type[] types, out string[] names)
    {
        var key = (baseType, includeAbstract);
        if (Cache.TryGetValue(key, out types) && NameCache.TryGetValue(key, out names) && types != null && names != null)
            return;

        IEnumerable<Type> all = TypeCache.GetTypesDerivedFrom(baseType);

        var list = all
            .Where(t => t != null)
            .Where(t => !t.IsGenericTypeDefinition)
            .Where(t => t.IsClass)
            .Where(t => includeAbstract || !t.IsAbstract)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();

        list.Insert(0, null); // None at top

        types = list.ToArray();
        names = list.Select(t => t == null ? "None" : t.FullName).ToArray();

        Cache[key] = types;
        NameCache[key] = names;
    }
}
#endif
