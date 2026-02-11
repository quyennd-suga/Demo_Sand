using System.Collections;
using UnityEngine;
using CostCenter;
public class SuGame : MonoBehaviour
{
    private static SuGame instance;

    public static SuGame Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SuGame>();
                if (instance != null)
                {
                    instance.Init();
                }
            }
            return instance;
        }
    }
    [SerializeField]
    private bool isTestMode;
    [SerializeField]
    private BaseSUUnit[] units;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            CheckDependencies();
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    private void Init()
    {
        Application.targetFrameRate = 60;
        LogManager.isTestMode = isTestMode;
        

        for (int i = 0; i < units.Length; i++)
        {
            units[i].Init(isTestMode);
        }

    }

    private void Start()
    {
        CCFirebase.instance.OnInitialized(true);
    }
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            //Debug.Log("save data when app is paused");
            DataManager.SaveData();
        }
        else
        {
            
        }


    }
    private T _Get<T>() where T : BaseSUUnit
    {
        foreach (BaseSUUnit unit in units)
        {
            if (typeof(T).Equals(unit.GetType()))
            {
                return unit as T;
            }
        }
        return default(T);
    }

    public static T Get<T>() where T : BaseSUUnit
    {
        return Instance._Get<T>();
    }

    public static bool haveDependencies = false;

    


    void CheckDependencies()
    {
        DataManager.LoadData();
#if UNITY_EDITOR
        haveDependencies = true;
        Init();
        // return;
#elif UNITY_ANDROID || UNITY_IOS

        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            ExecuteInUpdate.ExecuteInNextFrame(() =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == Firebase.DependencyStatus.Available)
                {
                    // Create and hold a reference to your FirebaseApp,
                    // where app is a Firebase.FirebaseApp property of your application class.
                    // Crashlytics will use the DefaultInstance, as well;
                    // this ensures that Crashlytics is initialized.
                    UnityEngine.Debug.Log("Check dependencies thanh cong");
                    //Firebase.FirebaseApp app = Firebase.FirebaseApp.DefaultInstance;
                    Debug.Log("~~~~~~~~~~~~~~~~~ Check xong ---- haveDependencies = " + haveDependencies);
                    haveDependencies = true;
                    // Set a flag here for indicating that your project is ready to use Firebase.
                }
                else
                {
                    haveDependencies = false;
                    UnityEngine.Debug.LogError(string.Format(
                      "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                    // Firebase Unity SDK is not safe to use here.
                }
                Debug.Log("********************************** Init Firebase xong ---- haveDependencies = " + haveDependencies);
                Init();
            });
        });

#endif



    }

}

