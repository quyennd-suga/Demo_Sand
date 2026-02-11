using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System.IO;

public class AddressableUISetup : EditorWindow
{
    [MenuItem("Tools/UI/Setup Addressable UI Assets")]
    public static void ShowWindow()
    {
        GetWindow<AddressableUISetup>("Addressable UI Setup");
    }

    [MenuItem("Tools/UI/Auto Setup All UI Prefabs")]
    public static void AutoSetupAllUIPrefabs()
    {
        SetupAddressableUI();
    }

    private void OnGUI()
    {
        GUILayout.Label("Addressable UI Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Auto Setup All UI Prefabs", GUILayout.Height(30)))
        {
            SetupAddressableUI();
        }

        GUILayout.Space(10);
        GUILayout.Label("Tự động tìm và add tất cả UI prefabs vào Addressables với key = tên file", EditorStyles.helpBox);
    }

    public static void SetupAddressableUI()
    {
        // Tạo hoặc lấy Addressable Settings
        var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        if (settings == null)
        {
            Debug.LogError("Không thể tạo Addressable Settings!");
            return;
        }

        // Tìm hoặc tạo UI Group
        var uiGroup = FindOrCreateUIGroup(settings);

        int addedCount = 0;

        // Tìm tất cả prefabs trong thư mục UI
        string[] uiFolders = { "Assets/Resources/UI", "Assets/Resources_moved/UI", "Assets/UI" };
        
        foreach (string folder in uiFolders)
        {
            if (Directory.Exists(folder))
            {
                addedCount += ProcessUIFolder(folder, uiGroup, settings);
            }
        }

        // Lưu settings
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ Đã setup {addedCount} UI prefabs vào Addressables!");
        
        // Hiển thị Addressables window
        EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
    }

    private static int ProcessUIFolder(string folderPath, AddressableAssetGroup group, AddressableAssetSettings settings)
    {
        int count = 0;
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        foreach (string guid in prefabGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            // Kiểm tra xem asset đã có trong Addressables chưa
            var entry = settings.FindAssetEntry(guid);
            if (entry == null)
            {
                // Add asset vào group với key = tên file
                entry = settings.CreateOrMoveEntry(guid, group);
                entry.address = fileName; // Key = tên file (VD: SettingsPopup)
                count++;
                Debug.Log($"Added: {fileName} -> {assetPath}");
            }
            else
            {
                // Cập nhật address nếu cần
                if (entry.address != fileName)
                {
                    entry.address = fileName;
                    Debug.Log($"Updated: {fileName} -> {assetPath}");
                }
            }
        }

        return count;
    }

    private static AddressableAssetGroup FindOrCreateUIGroup(AddressableAssetSettings settings)
    {
        // Tìm group UI có sẵn
        foreach (var group in settings.groups)
        {
            if (group.name.Contains("UI") || group.name.Contains("Default Local"))
            {
                return group;
            }
        }

        // Tạo group mới nếu không tìm thấy
        var template = settings.GetGroupTemplateObject(0) as AddressableAssetGroupTemplate;
        var newGroup = settings.CreateGroup("UI Assets", false, false, true, null, template.GetTypes());
        
        // Setup schema cho local build
        var bundleSchema = newGroup.GetSchema<BundledAssetGroupSchema>();
        if (bundleSchema != null)
        {
            bundleSchema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            bundleSchema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
        }

        return newGroup;
    }
}