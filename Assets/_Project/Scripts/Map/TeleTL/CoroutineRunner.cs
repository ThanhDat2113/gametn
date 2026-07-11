using UnityEngine;
using System.Collections;

/// <summary>
/// Singleton dùng để chạy Coroutine độc lập với lifecycle của các GameObject khác.
/// Tồn tại suốt vòng đời game (DontDestroyOnLoad).
/// Dùng khi cần StartCoroutine trên một GameObject có thể bị deactivate/destroy.
/// </summary>
public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner _instance;

    public static CoroutineRunner Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[CoroutineRunner]");
                _instance = go.AddComponent<CoroutineRunner>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Chạy một coroutine độc lập với GameObject gốc.</summary>
    public void Run(IEnumerator coroutine)
    {
        StartCoroutine(coroutine);
    }
}
