using Cysharp.Threading.Tasks;
using GamePlay.Features.Battle.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using UIs.Runtime;
using UnityEngine;

public class ViewTest : MonoBehaviour
{
    
    // Start is called before the first frame update
    private void Awake()
    {
        
    }

    void Start()
    {
        ShowBattleView().Forget();
    }

    private static async UniTask ShowBattleView()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("UIManager is null");
            return;
        }
        await UIManager.Instance.ShowViewAsync(ViewID.BattleSceneView);
    }
}
