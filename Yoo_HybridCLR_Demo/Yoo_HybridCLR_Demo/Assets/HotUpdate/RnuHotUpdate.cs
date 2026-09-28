using UnityEngine;
public class RunHotUpdate
{
    public static void Run()
    {
        Debug.Log("Hello, HybridCLR");
        Debug.Log("你好，世界！！！");
        Debug.Log("你好，世界！！！");
        var go = new GameObject("RunHotUpdate");
        go.AddComponent<DontDestroyOnLoad>();
        go.AddComponent<HotUpdateRunGame>();
    }
}
