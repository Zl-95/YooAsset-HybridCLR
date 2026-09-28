using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

public class RunGame
{
    public static void Run()
    {
        Debug.Log("RunGame");
        var canvas = GameObject.Find("Canvas");
        var package = YooAssetManager.Instance.ResourcePackage;
        var assetHandle = package.LoadAssetSync<GameObject>("PanelMain");
        if (assetHandle.Status == EOperationStatus.Succeeded)
        {
            // assetHandle.InstantiateAsync //实例化游戏对象。
            var panelMain = GameObject.Instantiate(assetHandle.AssetObject, canvas.transform) as GameObject;
            panelMain.AddComponent<PanelMain>();
            assetHandle.Release();
        }
    }
}
