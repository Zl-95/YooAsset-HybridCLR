using UniFramework.Machine;
using UnityEngine;
using YooAsset;

internal class FsmStartGame : IStateNode
{
    void IStateNode.OnCreate(StateMachine machine)
    {
    }
    void IStateNode.OnEnter()
    {
        PatchStepChangedEvent.SendEventMessage("Starting game.");

        // Set default package.
        GameManager.Instance.SetGamePackage(YooAssets.GetPackage("DefaultPackage"));
        //添加 LoadDll , 运行热更新代码 
        var gamePackage = GameManager.Instance.GamePackage;
        var loadDll = new GameObject("LoadDll");
        loadDll.AddComponent<LoadDll>().LoadUpdateDllsAsync(gamePackage);
        
    }
    void IStateNode.OnUpdate()
    {
    }
    void IStateNode.OnExit()
    {
    }
}