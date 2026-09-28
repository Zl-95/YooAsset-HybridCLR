using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

public class YooAssetManager
{
    private const string PACKAGE_NAME = "DefaultPackage";
    /// <summary>
    /// 资源包裹版本号
    /// </summary>
    private string packageVersion;
    private static YooAssetManager s_instance;
    public static YooAssetManager Instance
    {
        get
        {
            if (s_instance == null)
                s_instance = new YooAssetManager();
            return s_instance;
        }
    }
    public ResourcePackage ResourcePackage { get; private set; }
    /// <summary> 初始化资源包裹 </summary>
    /// <returns></returns>
    public IEnumerator InitPackage(EPlayMode playMode)
    {
        if (!YooAssets.TryGetPackage(PACKAGE_NAME, out var package))
            ResourcePackage = YooAssets.CreatePackage(PACKAGE_NAME);
        else
            ResourcePackage = package;
        InitializePackageOperation initOperation = null;
        //编辑器模拟运行模式
        if (playMode == EPlayMode.EditorSimulateMode)
        {
            var buildResult = EditorSimulateBuildInvoker.Build(PACKAGE_NAME, (int)EBundleType.VirtualAssetBundle);
            var packageRoot = buildResult.PackageRootDirectory;
            var fileSystemParams = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
            var createParameters = new EditorSimulateModeOptions();
            createParameters.EditorFileSystemParameters = fileSystemParams;

            initOperation = ResourcePackage.InitializePackageAsync(createParameters);
        }
        //单机运行模式
        if (playMode == EPlayMode.OfflinePlayMode)
        {
            var fileSystemParams = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
            var createParameters = new OfflinePlayModeOptions();
            createParameters.BuiltinFileSystemParameters = fileSystemParams;

            initOperation = ResourcePackage.InitializePackageAsync(createParameters);
        }
        //联机运行模式
        if (playMode == EPlayMode.HostPlayMode)
        {
            string defaultHostServer = GetHostServerURL();
            string fallbackHostServer = GetHostServerURL();
            IRemoteService remoteServices = new RemoteService(defaultHostServer, fallbackHostServer);
            var cacheFileSystemParams = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(remoteServices);
            var builtinFileSystemParams = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();

            var createParameters = new HostPlayModeOptions();
            createParameters.BuiltinFileSystemParameters = builtinFileSystemParams;
            createParameters.CacheFileSystemParameters = cacheFileSystemParams;
            createParameters.AutoUnloadBundleWhenUnused = true;
            initOperation = ResourcePackage.InitializePackageAsync(createParameters);
        }
        //Web运行模式
        if (playMode == EPlayMode.WebPlayMode)
        {
            //https://www.yooasset.com/docs/minigame/Wechat
#if UNITY_WEBGL && (WEIXINMINIGAME || UNITY_WECHATMINIGAME) && !UNITY_EDITOR
            var createParameters = new WebPlayModeOptions();
            string defaultHostServer = GetHostServerURL();
            string fallbackHostServer = GetHostServerURL();
            string packageRoot = $"{WeChatWASM.WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE"; // Change this path if subdirectories are required.
            IRemoteService remoteService = new RemoteService(defaultHostServer, fallbackHostServer);
            createParameters.WebNetworkFileSystemParameters = WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteService);
            initOperation = resourcePackage.InitializePackageAsync(createParameters);
#else
            var createParameters = new WebPlayModeOptions();
            createParameters.WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
            initOperation = ResourcePackage.InitializePackageAsync(createParameters);
#endif
        }
        //自定义运行模式
        if (playMode == EPlayMode.CustomPlayMode)
        {
            //// 配置各个文件系统参数
            //// ......
            // var createParameters = new CustomPlayModeOptions();
            // createParameters.FileSystemParameterList.Add(FileSystemParamsA);
            // createParameters.FileSystemParameterList.Add(FileSystemParamsB);
            // createParameters.FileSystemParameterList.Add(FileSystemParamsC);

            // initOperation = package.InitializePackageAsync(createParameters);
        }

        yield return initOperation;
        if (initOperation.Status == EOperationStatus.Succeeded)
            Debug.Log("资源包裹初始化成功！");
        else
            Debug.LogError($"资源包裹初始化失败：{initOperation.Error}");

        //获取资源版本
        yield return RequestPackageVersion();
        //更新资源清单
        yield return UpdatePackageManifest(packageVersion);
        //下载资源
        yield return Download();
        yield return ClearCache();
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
            return $"{hostServerIP}/CDN/YooAsset/PC1/{appVersion}";
#else
        if (Application.platform == RuntimePlatform.Android)
            return $"{hostServerIP}/CDN/YooAsset/Android/{appVersion}";
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
            return $"{hostServerIP}/CDN/YooAsset/IPhone/{appVersion}";
        else if (Application.platform == RuntimePlatform.WebGLPlayer)
            return $"{hostServerIP}/CDN/YooAsset/WebGL/{appVersion}";
        else
            return $"{hostServerIP}/CDN/YooAsset/PC1/{appVersion}";
#endif
    }

    /// <summary>    /// 请求资源包裹的版本号    /// </summary>
    /// <returns></returns>
    private IEnumerator RequestPackageVersion()
    {
        var operation = ResourcePackage.RequestPackageVersionAsync();
        yield return operation;

        if (operation.Status == EOperationStatus.Succeeded)
        {
            //请求成功
            packageVersion = operation.PackageVersion;
            Debug.Log($"Request package Version : {packageVersion}");
        }
        else
        {
            //请求失败
            Debug.LogError(operation.Error);
        }
    }

    /// <summary>    /// 更新资源包裹的清单    /// </summary>
    /// <returns></returns>
    private IEnumerator UpdatePackageManifest(string packageVersion)
    {
        var operation = ResourcePackage.LoadPackageManifestAsync(new LoadPackageManifestOptions(packageVersion, 60));
        yield return operation;

        if (operation.Status == EOperationStatus.Succeeded)
        {
            //更新成功
        }
        else
        {
            //更新失败
            Debug.LogError(operation.Error);
        }
    }
    IEnumerator Download()
    {
        int downloadingMaxNum = 10;
        int failedTryAgain = 3;
        //创建资源下载器
        var downloader = ResourcePackage.CreateResourceDownloader(new ResourceDownloaderOptions(downloadingMaxNum, failedTryAgain));

        //没有需要下载的资源
        if (downloader.TotalDownloadCount == 0)
        {
            yield break;
        }

        //需要下载的文件总数和总大小
        int totalDownloadCount = downloader.TotalDownloadCount;
        long totalDownloadBytes = downloader.TotalDownloadBytes;

        //注册回调方法
        downloader.DownloadCompleted += OnDownloadCompletedFunction; //当下载器结束（无论成功或失败）
        downloader.DownloadError += OnDownloadErrorFunction; //当下载器发生错误
        downloader.DownloadProgressChanged += OnDownloadProgressChangedFunction; //当下载进度发生变化
        downloader.DownloadFileStarted += OnDownloadFileStartedFunction; //当开始下载某个文件

        //开启下载
        downloader.StartDownload();
        yield return downloader;

        //检测下载结果
        if (downloader.Status == EOperationStatus.Succeeded)
        {
            //下载成功
            Debug.Log($"下载成功！总文件数：{totalDownloadCount}，总大小：{totalDownloadBytes}");
        }
        else
        {
            //下载失败
            Debug.LogError(downloader.Error);
        }
    }

    private void OnDownloadFileStartedFunction(DownloadFileStartedEventArgs args)
    {
        Debug.Log($"当前正在下载的文件：BundleName：{args.BundleName}；FileName: {args.FileName}");
    }

    private void OnDownloadCompletedFunction(DownloadCompletedEventArgs args)
    {
        Debug.Log($"下载是否成功：{args.Succeeded}");
    }

    private void OnDownloadErrorFunction(DownloadErrorEventArgs args)
    {
        Debug.LogError($"下载器发生错误：{args.ErrorInfo}；当前下载文件：{args.PackageName + args.FileName}");
    }

    private void OnDownloadProgressChangedFunction(DownloadProgressChangedEventArgs args)
    {
        Debug.Log($"当前下载字节/总下载字节：{args.CurrentDownloadBytes} / {args.TotalDownloadBytes}；当前下载数/总下载数：{args.CurrentDownloadCount} / {args.TotalDownloadCount}");
    }

    IEnumerator ClearCache()
    {
        var options = new ClearCacheOptions(ClearCacheMethods.ClearUnusedBundleFiles);
        var operation = ResourcePackage.ClearCacheAsync(options);
        yield return operation; 
        if (operation.Status == EOperationStatus.Succeeded)
        {
            //清理成功
        }
        else
        {
            //清理失败
            Debug.LogError(operation.Error);
        }
    }

    /// <summary>/// 远端资源地址查询服务类/// </summary>
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
            return new[]
            {
            $"{_defaultHostServer}/{fileName}",
            $"{_fallbackHostServer}/{fileName}"
        };
        }
    }

}
