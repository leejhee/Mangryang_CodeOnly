using Core.Scripts.Foundation.Define;
using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UIs.Runtime;
using UnityEngine;

namespace GamePlay.Features.Explore.Scripts
{
    public static class ExploreInitializer
    {
        public static async UniTask InitializeAsync(CancellationToken ct, IProgress<float> progress)
        {
            Debug.Log($"-----------------Explore Initialization Started----------------");
            GameManager.Instance.GameState = SystemEnum.GameState.Explore;
            progress?.Report(0.05f);

            await ExploreManager.Instance.ExploreInitialize();
            progress?.Report(0.5f);
            
            await UIManager.Instance.ShowViewAsync(ViewID.ExploreSceneView);
            progress?.Report(0.8f);
            
            ExploreSceneRunner.RunAfterLoading();
            progress?.Report(1.0f);
            Debug.Log($"-----------------Explore Initialization Succeed----------------");
        }
    }
}