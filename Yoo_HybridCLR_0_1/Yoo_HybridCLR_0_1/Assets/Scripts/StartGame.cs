//using HybridCLR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using UnityEngine;
using YooAsset;

public class StartGame : MonoBehaviour
{
    public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;
    // Start is called before the first frame update
    IEnumerator Start()
    {
        //初始化YooAsset
        YooAssets.Initialize();
        yield return YooAssetManager.Instance.InitPackage(PlayMode);
        //加载热更新代码

        Assembly hotUpdateAss = null;
        // Editor环境下，HotUpdateScripts.dll.bytes已经被自动加载，不需要加载，重复加载反而会出问题。
#if !UNITY_EDITOR
        var assetHandle = YooAssetManager.Instance.ResourcePackage.LoadAssetAsync<TextAsset>("HotUpdateScripts.dll");
        yield return assetHandle;
        if (assetHandle.Status == YooAsset.EOperationStatus.Succeeded)
        {
            var dllBytes = assetHandle.AssetObject as TextAsset;
            //加载热更新程序集
            hotUpdateAss = Assembly.Load(dllBytes.bytes);
        }
        else
        {
            //使用Application.streamingAssetsPath文件夹模拟服务器
            hotUpdateAss = Assembly.Load(File.ReadAllBytes($"{Application.streamingAssetsPath}/HotUpdateScripts.dll.bytes"));
            Debug.LogError($"Failed to load HotUpdateScripts.dll.bytes: {assetHandle.Error}");
        }
        assetHandle.Release();
#else

        // Editor下无需加载，直接查找获得HotUpdateScripts程序集
        var Assemblies = AppDomain.CurrentDomain.GetAssemblies();
        hotUpdateAss = Assemblies .First(a => a.GetName().Name == "HotUpdateScripts");
        
#endif
        Type type = hotUpdateAss.GetType("RunGame");
        type.GetMethod("Run").Invoke(null,null);
    }

}

