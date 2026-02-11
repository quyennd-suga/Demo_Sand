using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static bool _isQuitting;
    private static readonly object _lock = new object();

    /// <summary>
    /// Override = true nếu muốn singleton sống qua scene.
    /// </summary>
    protected virtual bool DontDestroyOnLoadEnabled => true;

    public static T Instance
    {
        get
        {
            if (_isQuitting) return null;

            lock (_lock)
            {
                if (_instance != null) return _instance;

                // 1) Tìm instance đã có trong scene
                _instance = Object.FindFirstObjectByType<T>(FindObjectsInactive.Exclude);

                // 2) Nếu có nhiều hơn 1 => log lỗi (để phát hiện sai thiết kế)
                var all = Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (all != null && all.Length > 1)
                {
                    Debug.LogError($"[Singleton<{typeof(T).Name}>] Found {all.Length} instances. There should be only one.");
                    // Chọn cái đầu tiên để chạy tiếp, nhưng nên sửa root cause.
                    _instance = all[0];
                }

                // 3) Không có => tự tạo GameObject + add component
                if (_instance == null)
                {
                    var go = new GameObject($"{typeof(T).Name} (Singleton)");
                    _instance = go.AddComponent<T>();
                }

                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_isQuitting) return;

        if (_instance == null)
        {
            _instance = this as T;

            if (DontDestroyOnLoadEnabled)
                DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            // Nếu trùng => destroy cái dư (tránh 2 audio manager, 2 event bus,...)
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }
}
