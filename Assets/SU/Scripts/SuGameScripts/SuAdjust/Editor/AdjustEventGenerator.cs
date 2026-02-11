using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class AdjustEventGenerator : EditorWindow
{
    [MenuItem("Adjust/EventGenerator")]
    public static void ShowExample()
    {
        AdjustEventGenerator wnd = GetWindow<AdjustEventGenerator>();
        wnd.titleContent = new GUIContent("AdjustEventGenerator");
    }
    static string inputText;
    private void OnGUI()
    {
        GUILayout.Label("Nội dung events", EditorStyles.boldLabel);
        inputText = EditorGUILayout.TextArea(inputText, GUILayout.Height(500));

        if (GUILayout.Button("Submit"))
        {
            // Xử lý logic khi người dùng nhấn nút Submit
            CreateDatabase();
        }
    }

    void CreateDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:" + typeof(AdjustEventTokenDB));
        AdjustEventTokenDB currentTokenDB = null;
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            AdjustEventTokenDB asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(AdjustEventTokenDB)) as AdjustEventTokenDB;

            if (asset != null)
            {
                // Xử lý đối tượng tìm thấy ở đây
                Debug.Log("Found object: " + asset.name);
                currentTokenDB = asset;
                break;
            }
        }
        if (currentTokenDB != null)
        {
            currentTokenDB.EventTokenDict.Clear();
            currentTokenDB.EventTokens.Clear();
            string[] sEvents = inputText.Split('\n');
            Debug.Log("Số dòng là " + sEvents.Length);
            List<string> EventErrorList = new List<string>();
            for (int i = 0; i < sEvents.Length - 1; i += 2)
            {
                string sEventName = sEvents[i];
                string token = sEvents[i + 1];
                EventName evName;
                bool parse = System.Enum.TryParse<EventName>(sEventName, out evName);
                if (parse)
                {
                    currentTokenDB.EventTokens.Add(new AdjustEventTokenModule()
                    {
                        eventName = evName,
                        token = token
                    });
                }
                else
                {
                    EventErrorList.Add(sEventName);
                }

            }
            if (EventErrorList.Count == 0)
            {
                EditorUtility.SetDirty(currentTokenDB);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.Log("Các event lỗi là : ");
                for (int i = 0; i < EventErrorList.Count; i++)
                {
                    Debug.Log("- " + EventErrorList[i]);
                }
            }
        }
    }
}
