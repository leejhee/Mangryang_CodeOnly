using Cysharp.Threading.Tasks;
using UIs.Runtime;
using UnityEngine.SceneManagement;

namespace GamePlay.Features.Explore.Scripts
{
    public static class ExploreSceneRunner
    {
        private static bool scheduled;

        public static void RunAfterLoading()
        {
            if (scheduled) return;
            scheduled = true;
            UniTask.Void(async () =>
            {
                await UniTask.WaitUntil(() =>
                {
                    UnityEngine.SceneManagement.Scene s = SceneManager.GetSceneByName("LoadingScene");
                    return !s.IsValid() || !s.isLoaded;
                });

                scheduled = false;
            });
            
        }
    }
}