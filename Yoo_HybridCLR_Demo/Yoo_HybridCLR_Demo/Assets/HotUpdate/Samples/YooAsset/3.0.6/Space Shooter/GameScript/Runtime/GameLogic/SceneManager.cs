using System;
using System.Collections;
using UnityEngine;
using UniFramework.Event;
using YooAsset;

/// <summary>
/// 场景管理器，切换场景
/// </summary>
public class SceneManager
{
    private static SceneManager s_instance;
    public static SceneManager Instance
    {
        get
        {
            if (s_instance == null)
                s_instance = new SceneManager();
            return s_instance;
        }
    }

    private readonly EventGroup _eventGroup = new EventGroup();
    private ResourcePackage _gamePackage;

    /// <summary>
    /// Game package.
    /// </summary>
    public ResourcePackage GamePackage
    {
        get
        {
            if (_gamePackage == null)
                throw new InvalidOperationException("Game package has not been set. Call SetGamePackage before loading game assets.");
            return _gamePackage;
        }
    }

    /// <summary>
    /// Sets the game package.
    /// </summary>
    public void SetGamePackage(ResourcePackage gamePackage)
    {
        _gamePackage = gamePackage ?? throw new ArgumentNullException(nameof(gamePackage));
    }


    private SceneManager()
    {
        // 添加切换场景事件监听器
        _eventGroup.AddListener<SceneChangeToHomeEvent>(OnHandleEventMessage);
        _eventGroup.AddListener<SceneChangeToBattleEvent>(OnHandleEventMessage);
    }


    /// <summary>
    /// Handles event messages.
    /// </summary>
    private void OnHandleEventMessage(IEventMessage message)
    {
        if (message is SceneChangeToHomeEvent)
        {
            GamePackage.LoadSceneAsync("scene_home");
        }
        else if (message is SceneChangeToBattleEvent)
        {
            GamePackage.LoadSceneAsync("scene_battle");
        }
    }
}