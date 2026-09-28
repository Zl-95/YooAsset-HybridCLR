using HybridCLR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using YooAsset;

public class LoadDll : MonoBehaviour
{
    public void LoadUpdateDllsAsync(ResourcePackage gamePackage)
    {
        Debug.Log("LoadDll - Start");
        StartCoroutine(LoadUpdateDlls(gamePackage));
    }
    IEnumerator LoadUpdateDlls(ResourcePackage gamePackage)
    {
        // Editor环境下，HotUpdate.dll.bytes已经被自动加载，不需要加载，重复加载反而会出问题。
        Assembly hotUpdateAss = null;
#if !UNITY_EDITOR
        //热更新加载
        var assetHandle = gamePackage.LoadAssetAsync<TextAsset>("HotUpdate.dll");
        yield return assetHandle;
        if (assetHandle.Status == YooAsset.EOperationStatus.Succeeded)
        {
            var dllBytes = assetHandle.AssetObject as TextAsset;
            //加载热更新程序集
            hotUpdateAss = Assembly.Load(dllBytes.bytes);
        }
        else
        {
            hotUpdateAss = Assembly.Load(File.ReadAllBytes($"{Application.streamingAssetsPath}/HotUpdate.dll.bytes"));
            Debug.LogError($"Failed to load HotUpdate.dll.bytes: {assetHandle.Error}");
        }
        assetHandle.Release();

#else
        // Editor下无需加载，直接查找获得HotUpdate程序集
        hotUpdateAss = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "HotUpdate");
        yield return null;
#endif

        //调用热更新代码
        Type helloType = hotUpdateAss.GetType("RunHotUpdate");
        MethodInfo runMethod = helloType.GetMethod("Run", BindingFlags.Public | BindingFlags.Static);
        runMethod.Invoke(null, null);
    }
}

