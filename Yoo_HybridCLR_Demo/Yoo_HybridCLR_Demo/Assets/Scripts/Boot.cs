using UnityEngine;
using UniFramework.Event;
using YooAsset;

public class Boot : MonoBehaviour
{
    /// <summary>
    /// Resource system play mode.
    /// </summary>
    public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

    void Awake()
    {
        Debug.Log($"Resource system play mode: {PlayMode}.");
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        DontDestroyOnLoad(this.gameObject);
    }
    void Start()
    {
        // Game manager.
        GameManager.Instance.SetBehaviour(this);

        // Initialize event system.
        UniEvent.Initalize();

        // Initialize asset system.
        YooAssets.Initialize();

        // 加载补丁窗口预制体并实例化
        var go = Resources.Load<GameObject>("PatchWindow");
        GameObject.Instantiate(go);

        // Start patch workflow.
        PatchManager.Create("DefaultPackage", PlayMode);
        PatchManager.Start();
    }
    private void Update()
    {
        PatchManager.Update();
    }
}
