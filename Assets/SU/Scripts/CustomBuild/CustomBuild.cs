
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Ionic.Zip;
using UnityEngine;

public class CustomBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder
    {
        get
        {
            return 0;
        }
        set
        {

        }
    }

    static BuildPlayerOptions GetBuildPlayerOptions(bool askForLocation = false,
    BuildPlayerOptions defaultOptions = new BuildPlayerOptions())
    {
        // Get static internal "GetBuildPlayerOptionsInternal" method
        MethodInfo method = typeof(BuildPlayerWindow.DefaultBuildMethods).GetMethod(
           "GetBuildPlayerOptionsInternal",
           BindingFlags.NonPublic | BindingFlags.Static);

        // invoke internal method
        return (BuildPlayerOptions)method.Invoke(null, new object[] { askForLocation, defaultOptions });
    }

    static string Rename(string path, string newName)
    {
        // change file name
        int start = 0;
        int end = 0;
        start = path.LastIndexOf('/');
        end = path.LastIndexOf('.');
        string name = path.Substring(start + 1, end - start - 1);
        string newPath = path.Replace(name, newName);
        return newPath;
    }

    static string GetGameName(string path)
    {
        // lấy tên của file ( không lấy đuôi )
        int start = path.LastIndexOf('/');
        int end = path.LastIndexOf('.');
        string name = path.Substring(start + 1, end - start - 1);
        return name;
    }
    static bool addVersionToFinalFile;
    static bool convert2Apk;
    static string BuildOutputPath;
    static string OutPutGameName, OutPutSymbolName;
    //[MenuItem("SuTools/Custom Build")]
    public static void BuildGame()
    {
        CheckAndRemoveReporter();
        BuildPlayerOptions buildOption = GetBuildPlayerOptions(false);
        // tự động đổi keystore pass sang 123456 nếu chưa set
        if (PlayerSettings.Android.keystorePass == "")
        {
            PlayerSettings.Android.keystorePass = "123456";
        }
        // tự động đổi keysalias pass sang 123456 nếu chưa set
        if (PlayerSettings.Android.keyaliasPass == "")
        {
            PlayerSettings.Android.keyaliasPass = "123456";
        }

        BuildOutputPath = buildOption.locationPathName;
        OutPutGameName = GetGameName(BuildOutputPath);

        convert2Apk = false;
        // hỏi xem có muốn convert sang APK luôn không 
        // chỉ cho android , không hỗ trợ IOS
#if UNITY_ANDROID

        if (EditorUserBuildSettings.buildAppBundle)
        {
            convert2Apk = EditorUtility.DisplayDialog("Thông báo", "Bạn có muốn convert luôn từ .aab sang .apk không ?", "Có", "Không");
        }
        addVersionToFinalFile = EditorUtility.DisplayDialog("Thông báo", "Bạn có muốn thêm version vào tên file không ?", "Có", "Không");
#endif
        UnityEngine.Debug.Log("Convert2Apk = " + convert2Apk);
    }

    static void CheckAndRemoveReporter()
    {
        GameObject reporter = GameObject.Find("Reporter");
        if (reporter != null)
        {
            bool removeReporter = EditorUtility.DisplayDialog("Cảnh báo", "Chưa xóa reporter ", "Xóa", "Bỏ qua");
            if (removeReporter)
            {
                Object.DestroyImmediate(reporter, false);
            }
        }
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        // chạy trước khi build game
        BuildGame();
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        // sau khi build xong
        if (addVersionToFinalFile)
        {
            string outputPath = BuildOutputPath;
            int lastDotPos = outputPath.LastIndexOf('.');
            string versionName = Application.version;
            int versionCode = UnityEditor.PlayerSettings.Android.bundleVersionCode;
            string newOutPutPath = outputPath.Insert(lastDotPos, "_" + versionName + "_" + versionCode);
            File.Move(outputPath, newOutPutPath);
            BuildOutputPath = newOutPutPath;
            OutPutSymbolName = OutPutGameName + "-" + versionName + "-v" + versionCode + ".symbols.zip";
            OutPutGameName = OutPutGameName + "_" + versionName + "_" + versionCode;

        }
        if (convert2Apk)
        {
            ConvertToApk();
        }
    }

    public static string toolPathBase = "";
    static void ConvertToApk()
    {
        string path = BuildOutputPath;
        string fileName = Path.GetFileNameWithoutExtension(path);
        // lấy path của bundle tool
        if (toolPathBase == "")
        {
            string[] adr = AssetDatabase.FindAssets("CustomBuild_BundleTool");
            toolPathBase = Application.dataPath.Replace("Assets", "") + AssetDatabase.GUIDToAssetPath(adr[0]).Replace("CustomBuild_BundleTool", "");

        }
        string bundleToolPath = toolPathBase + "CustomBuild_BundleTool";
        UnityEngine.Debug.Log("Game Name là " + "/" + OutPutGameName);
        string outputPath = BuildOutputPath.Replace(OutPutGameName, "").Replace(".aab", "") + fileName;
        UnityEngine.Debug.Log("Toolpath là " + bundleToolPath + " \noutputPath là " + outputPath);

        string keyPath = PlayerSettings.Android.keystoreName;
        string keyPass = PlayerSettings.Android.keystorePass == "" ? "123456" : PlayerSettings.Android.keystorePass;
        // alias name được đặt auto là alias rồi
        //string alias = "alias";
        string aliasPass = PlayerSettings.Android.keyaliasPass == "" ? "123456" : PlayerSettings.Android.keyaliasPass;
        //string outputName = "output";
        if (path.Length != 0)
        {
            string argument = "java -jar " + "\"" + bundleToolPath + "\"" + " build-apks --mode=universal --bundle=" + "\"" + path + "\"" + " --output=" + "\"" + outputPath + "\"" + ".apks --ks=" + keyPath + " --ks-pass=pass:" + keyPass + " --ks-key-alias=alias --key-pass=pass:" + aliasPass;
            UnityEngine.Debug.Log("Argument la " + argument);
            var processInfo = new ProcessStartInfo("cmd.exe")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            var process = Process.Start(processInfo);

            process.StandardInput.WriteLine("chcp 65001");
            process.StandardInput.WriteLine(argument);
            process.StandardInput.WriteLine("exit");
            process.WaitForExit();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            ConvertToZip(outputPath);

        }
    }
    public static void ConvertToZip(string path)
    {
        string starPath = path + ".apks";
        string zipPath = path + ".zip";
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }
        File.Move(starPath, zipPath);
        AssetDatabase.Refresh();
        using (ZipFile zip = new ZipFile(zipPath))
        {
            // Set decompression password
            //zip.Password = "123456";
            zip.ExtractAll(path, ExtractExistingFileAction.OverwriteSilently);
            UnityEngine.Debug.Log("di chuyển file từ " + BuildOutputPath + " đến " + path + "/" + OutPutGameName);
            string newAABpath = path + "/" + OutPutGameName + ".aab";
            if (File.Exists(newAABpath))
            {
                UnityEngine.Debug.Log("File đã tồn tại ,xóa file cũ đi");
                File.Delete(newAABpath);
            }
            File.Move(BuildOutputPath, path + "/" + OutPutGameName + ".aab");
            // rename universa.apk thành tên game 
            string apkPath = path + "/universal.apk";
            string newApkPath = path + "/" + OutPutGameName + ".apk";
            if (File.Exists(newApkPath))
            {
                UnityEngine.Debug.Log("File đã tồn tại ,xóa file cũ đi");
                File.Delete(newApkPath);
            }
            File.Move(apkPath, newApkPath);
            // move symbols file vào folder này 
            string symbolsPath = path.Replace(OutPutGameName, "") + OutPutSymbolName;
            UnityEngine.Debug.Log("symbols path là " + symbolsPath);
            string newSymbolsPath = path + "/" + OutPutSymbolName;
            UnityEngine.Debug.Log("new symbols path là " + newSymbolsPath);
            if (File.Exists(newSymbolsPath))
            {
                UnityEngine.Debug.Log("File đã tồn tại ,xóa file cũ đi");
                File.Delete(newSymbolsPath);
            }
            File.Move(symbolsPath, newSymbolsPath);
            CreateBatchFileToUploadSymbolOnFirebase(path + "/", newSymbolsPath);
            ShowExplorer(newApkPath);
        }

        File.Delete(zipPath);
        AssetDatabase.Refresh();
    }


    static void CreateBatchFileToUploadSymbolOnFirebase(string filePath, string symbolPath)
    {

        string path = filePath + "UpLoadSymbolToFirebase.bat";
        string appID = Firebase.FirebaseApp.DefaultInstance.Options.AppId;
        UnityEngine.Debug.Log("AppID là " + appID);
        //Write some text to the test.txt file
        StreamWriter writer = new StreamWriter(path, true);
        writer.WriteLine("call firebase crashlytics:symbols:upload --app=" + appID + " " + symbolPath);
        writer.WriteLine("pause");
		
        writer.Close();

    }


    [MenuItem("Test/TestShowInExplorer")]
    public static void TestShowInExplorer()
    {
        UnityEngine.Debug.Log("Test");
        //string appID = Firebase.FirebaseApp.DefaultInstance.Options.AppId;
        //UnityEngine.Debug.Log("AppID là " + appID);
        //string path = BuildOutputPath;
        //UnityEngine.Debug.Log("Path là " + path);

        //System.Diagnostics.Process.Start("explorer.exe", "/select," + "C:\\Users\\User\\Desktop\\ExportGames\\Blockdoku_1.7_7\\...");
        //EditorUtility.RevealInFinder("C:\\Users\\User\\Desktop\\ExportGames\\Blockdoku_1.7_7\\Blockdoku_1.7_7.aab");
    }

    public static void ShowExplorer(string itemPath)
    {
        itemPath = itemPath.Replace(@"/", @"\");   // explorer doesn't like front slashes
        System.Diagnostics.Process.Start("explorer.exe", "/select," + itemPath);

    }
}
#endif