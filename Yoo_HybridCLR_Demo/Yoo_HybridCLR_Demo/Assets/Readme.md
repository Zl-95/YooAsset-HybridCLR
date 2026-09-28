# 使用YooAsset提供的打飞机 例子学习YooAsset资源管理；
# 从0开始配置HybridCLR 学习 代码热更新，仅实现了文档中 新手教程中 快速上手 、使用MonoBehaviour 两部分。
## 将例子中的代码区分为两部分 热更部分和AOT部分
	可以修改热更部分修改计分规则和显示，可以不重新打包。
	
# 加载热更代码需要在资源加载完后进行，注意热更代码的存储格式 需要添加后缀 .bytes 加载类型为 TextAsset
代码示例：
```C#
        Assembly hotUpdateAss = null;
#if !UNITY_EDITOR
        //热更新加载 先获取包 再获取资源
        var assetHandle = YooAssets.GetPackage("DefaultPackage").LoadAssetAsync<TextAsset>("HotUpdate.dll");
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
```