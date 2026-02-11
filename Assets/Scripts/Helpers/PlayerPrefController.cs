using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PlayerPrefController : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("My Helper / ClearPref")]
    public static void ClearPref()
    {
        PlayerPrefs.DeleteAll();
    }
#endif
}
