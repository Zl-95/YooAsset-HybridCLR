using System;
using UniFramework.Machine;
using UniFramework.Event;
using YooAsset;

/// <summary>
/// 补丁管理器，负责管理整个补丁流程的状态机和事件监听。
/// </summary>
public static class PatchManager
{
    private static readonly EventGroup _eventGroup = new EventGroup();
    private static StateMachine _machine;

    public static void Create(string packageName, EPlayMode playMode)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            throw new ArgumentException("Package name cannot be null or empty.", nameof(packageName));
        if (!IsValidPlayMode(playMode))
            throw new ArgumentException($"Invalid play mode: {playMode}.", nameof(playMode));

        // Register event listeners.
        _eventGroup.AddListener<UserTryInitializePackageEvent>(OnHandleEventMessage);
        _eventGroup.AddListener<UserBeginDownloadWebFilesEvent>(OnHandleEventMessage);
        _eventGroup.AddListener<UserTryRequestPackageVersionEvent>(OnHandleEventMessage);
        _eventGroup.AddListener<UserTryUpdatePackageManifestEvent>(OnHandleEventMessage);
        _eventGroup.AddListener<UserTryDownloadWebFilesEvent>(OnHandleEventMessage);

        // Create state machine.
        _machine = new StateMachine(null);
        _machine.AddNode<FsmInitializePackage>();//初始化包节点
        _machine.AddNode<FsmRequestPackageVersion>();//请求包版本节点
        _machine.AddNode<FsmUpdatePackageManifest>();//更新包清单节点
        _machine.AddNode<FsmCreateDownloader>();//创建下载器节点
        _machine.AddNode<FsmDownloadPackageFiles>();//下载包文件节点
        _machine.AddNode<FsmDownloadPackageOver>();//下载包完成节点
        _machine.AddNode<FsmClearCacheBundle>();//清理缓存节点
        _machine.AddNode<FsmStartGame>();//启动游戏节点

        _machine.SetBlackboardValue("PackageName", packageName);
        _machine.SetBlackboardValue("PlayMode", playMode);
    }
    public static void Start()
    {
        if (_machine == null)
            throw new InvalidOperationException("Patch manager has not been created. Call Create before Start.");

        _machine.Run<FsmInitializePackage>();//首先运行初始化包节点
    }
    public static void Update()
    {
        if (_machine == null)
            throw new InvalidOperationException("Patch manager has not been created. Call Create before Update.");

        _machine.Update();
    }

    /// <summary>
    /// Handles event messages.
    /// </summary>
    private static void OnHandleEventMessage(IEventMessage message)
    {
        if (message is UserTryInitializePackageEvent)
        {
            _machine.ChangeState<FsmInitializePackage>();
        }
        else if (message is UserBeginDownloadWebFilesEvent)
        {
            _machine.ChangeState<FsmDownloadPackageFiles>();
        }
        else if (message is UserTryRequestPackageVersionEvent)
        {
            _machine.ChangeState<FsmRequestPackageVersion>();
        }
        else if (message is UserTryUpdatePackageManifestEvent)
        {
            _machine.ChangeState<FsmUpdatePackageManifest>();
        }
        else if (message is UserTryDownloadWebFilesEvent)
        {
            _machine.ChangeState<FsmCreateDownloader>();
        }
        else
        {
            throw new InvalidOperationException($"Unsupported patch event message type: {message.GetType().FullName}.");
        }
    }

    private static bool IsValidPlayMode(EPlayMode playMode)
    {
        switch (playMode)
        {
            case EPlayMode.EditorSimulateMode:
            case EPlayMode.OfflinePlayMode:
            case EPlayMode.HostPlayMode:
            case EPlayMode.WebPlayMode:
                return true;
            default:
                return false;
        }
    }
}