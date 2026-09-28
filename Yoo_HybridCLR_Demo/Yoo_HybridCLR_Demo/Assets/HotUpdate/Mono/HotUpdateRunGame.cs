using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

public class HotUpdateRunGame : MonoBehaviour
{
    List<int> ints; 
    void Start()
    {
        ints = new List<int>();
        ints.Add(1);
        ints.Add(2);
        for (int i = 0; i < ints.Count; i++)
        {
            Debug.Log($"[Print] GameObject:{name} - {ints[i]}");
        }

        // Scene manager.
        SceneManager.Instance.SetGamePackage(YooAssets.GetPackage("DefaultPackage"));
        // Change to home scene.
        SceneChangeToHomeEvent.SendEventMessage();
    }
}