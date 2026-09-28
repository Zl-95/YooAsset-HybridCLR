using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniFramework.Machine;
using YooAsset;

internal class FsmInitializePackage : IStateNode
{
    private StateMachine _machine;

    void IStateNode.OnCreate(StateMachine machine)
    {
        _machine = machine;
    }
    void IStateNode.OnEnter()
    {
        PatchStepChangedEvent.SendEventMessage("Initializing package.");
        GameManager.Instance.StartCoroutine(InitPackage());
    }
    void IStateNode.OnUpdate()
    {
    }
    void IStateNode.OnExit()
    {
    }

    /// <summary>
    /// 初始化包  ResourcePackage package,  package.InitializePackageAsync(createParameters);   根据不同的运行模式传入不同的参数
    /// 
    /// EPlayMode.EditorSimulateMode ：模拟编辑器环境。不需要手动打包
    /// EPlayMode.OfflinePlayMode ：离线模式运行；需要将打出的包内容放在项目的 StreamingAssets 文件夹中。在资源打包时，Bundled Copy Option 选择 Clear And Copy All
    /// EPlayMode.HostPlayMode ： 热更新模式运行；从远端服务器下载最新资源。
    /// 
    /// </summary>
    /// <returns></returns>
    private IEnumerator InitPackage()
    {
        var playMode = (EPlayMode)_machine.GetBlackboardValue("PlayMode");
        var packageName = (string)_machine.GetBlackboardValue("PackageName");

        // Create package.
        if (!YooAssets.TryGetPackage(packageName, out var package))
            package = YooAssets.CreatePackage(packageName);

        // 模拟编辑器模式 不需要手动打包
        InitializePackageOperation initializationOperation = null;
        if (playMode == EPlayMode.EditorSimulateMode)
        {
            var buildResult = EditorSimulateBuildInvoker.Build(packageName, (int)EBundleType.VirtualAssetBundle);
            var packageRoot = buildResult.PackageRootDirectory;
            var createParameters = new EditorSimulateModeOptions();
            createParameters.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
            createParameters.EditorFileSystemParameters.AddParameter(EFileSystemParameter.VirtualDownloadMode, true);
            createParameters.EditorFileSystemParameters.AddParameter(EFileSystemParameter.VirtualDownloadSpeed, 1024 * 1000);
            createParameters.EditorFileSystemParameters.AddParameter(EFileSystemParameter.AsyncSimulateMinFrame, 5);
            createParameters.EditorFileSystemParameters.AddParameter(EFileSystemParameter.AsyncSimulateMaxFrame, 10);
            initializationOperation = package.InitializePackageAsync(createParameters);
        }

        // 离线运行模式
        if (playMode == EPlayMode.OfflinePlayMode)
        {
            var createParameters = new OfflinePlayModeOptions();
            createParameters.BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
            initializationOperation = package.InitializePackageAsync(createParameters);
        }

        // 热更模式
        if (playMode == EPlayMode.HostPlayMode)
        {
            string defaultHostServer = GetHostServerURL();
            string fallbackHostServer = GetHostServerURL();
            IRemoteService remoteService = new RemoteService(defaultHostServer, fallbackHostServer);
            var createParameters = new HostPlayModeOptions();
            createParameters.BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
            createParameters.BuiltinFileSystemParameters.AddParameter(EFileSystemParameter.CopyBuiltinPackageManifest, true);
            createParameters.CacheFileSystemParameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(remoteService);
            createParameters.CacheFileSystemParameters.AddParameter(EFileSystemParameter.DownloadMaxConcurrency, 5);
            createParameters.CacheFileSystemParameters.AddParameter(EFileSystemParameter.DownloadMaxRequestPerFrame, 1);
            createParameters.CacheFileSystemParameters.AddParameter(EFileSystemParameter.DownloadWatchdogTimeout, 10);
            initializationOperation = package.InitializePackageAsync(createParameters);
        }

        // Web play mode.
        if (playMode == EPlayMode.WebPlayMode)
        {
#if UNITY_WEBGL && (WEIXINMINIGAME || UNITY_WECHATMINIGAME) && !UNITY_EDITOR
            var createParameters = new WebPlayModeOptions();
            string defaultHostServer = GetHostServerURL();
            string fallbackHostServer = GetHostServerURL();
            string packageRoot = $"{WeChatWASM.WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE"; // Change this path if subdirectories are required.
            IRemoteService remoteService = new RemoteService(defaultHostServer, fallbackHostServer);
            createParameters.WebNetworkFileSystemParameters = WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteService);
            initializationOperation = package.InitializePackageAsync(createParameters);
#else
            var createParameters = new WebPlayModeOptions();
            createParameters.WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
            initializationOperation = package.InitializePackageAsync(createParameters);
#endif
        }

        yield return initializationOperation;

        // Show prompt when initialization fails.
        if (initializationOperation.Status != EOperationStatus.Succeeded)
        {
            Debug.LogWarning($"{initializationOperation.Error}");
            PatchInitializeFailedEvent.SendEventMessage();
        }
        else
        {
            _machine.ChangeState<FsmRequestPackageVersion>();
        }
    }

    /// <summary>
    /// 获取资源服务器的URL.
    /// </summary>
    private string GetHostServerURL()
    {
        //string hostServerIP = "http://10.0.2.2"; // Android emulator address.
        string hostServerIP = "http://192.168.1.5";
        string appVersion = "v1.0";

#if UNITY_EDITOR
        if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
            return $"{hostServerIP}/CDN/YooAsset/Android/{appVersion}";
        else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
            return $"{hostServerIP}/CDN/YooAsset/IPhone/{appVersion}";
        else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL)
            return $"{hostServerIP}/CDN/YooAsset/WebGL/{appVersion}";
        else
            return $"{hostServerIP}/CDN/YooAsset/PC/{appVersion}";
#else
        if (Application.platform == RuntimePlatform.Android)
            return $"{hostServerIP}/CDN/YooAsset/Android/{appVersion}";
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
            return $"{hostServerIP}/CDN/YooAsset/IPhone/{appVersion}";
        else if (Application.platform == RuntimePlatform.WebGLPlayer)
            return $"{hostServerIP}/CDN/YooAsset/WebGL/{appVersion}";
        else
            return $"{hostServerIP}/CDN/YooAsset/PC/{appVersion}";
#endif
    }

    /// <summary>
    /// Remote resource URL query service.
    /// </summary>
    private class RemoteService : IRemoteService
    {
        private readonly string _defaultHostServer;
        private readonly string _fallbackHostServer;

        public RemoteService(string defaultHostServer, string fallbackHostServer)
        {
            _defaultHostServer = defaultHostServer;
            _fallbackHostServer = fallbackHostServer;
        }
        public IReadOnlyList<string> GetRemoteUrls(string fileName)
        {
            List<string> result = new List<string>();
            result.Add($"{_defaultHostServer}/{fileName}");
            result.Add($"{_fallbackHostServer}/{fileName}");
            return result;
        }
    }
}