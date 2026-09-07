using Core.Scripts.Foundation.Define;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Scripts.Foundation.SceneUtil
{
    public static class SceneLoader
    {
        public static SystemEnum.eScene DestinationScene { get; private set; }
        public static Func<CancellationToken, IProgress<float>, UniTask> InitCallbackAsync { get; private set; } // 파이프라인을 여기에 쌓을 것
        public static ISceneArgs SceneArgs { get; private set; }
        public static bool IsLoading => DestinationScene != SystemEnum.eScene.None;
        
        public static void LoadSceneWithLoading(
            SystemEnum.eScene destination,
            Func<CancellationToken, IProgress<float>, UniTask> sceneInitCallback=null,
            ISceneArgs sceneArgs=null)
        {
            if (destination == SystemEnum.eScene.None)
                throw new ArgumentOutOfRangeException(nameof(destination), "Destination scene must be specified.");

            if (IsLoading)
            {
                Debug.LogWarning($"[SceneLoader] Scene transition already in progress: {DestinationScene}");
                return;
            }

            DestinationScene = destination;
            InitCallbackAsync = sceneInitCallback;
            SceneArgs = sceneArgs;
            SceneManager.LoadScene(nameof(SystemEnum.eScene.LoadingScene));
        }
        
        public static void Clear()
        {
            DestinationScene = SystemEnum.eScene.None;
            InitCallbackAsync = null;
            SceneArgs = null;
        }
    }
}
