using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Scene;
using System.Threading;

namespace GamePlay.Common.Scripts.Transition
{
    public static class FlowHelper
    {
        public static async UniTask LoadBattleSceneWithFadeAsync(
            float fadeOutDuration = 0.4f,
            float fadeInDuration  = 0.4f,
            CancellationToken ct  = default)
        {
            await ScreenTransitionManager.Instance.RunEnterFade(
                GamePlaySceneUtil.LoadBattleScene,
                fadeOutDuration,
                fadeInDuration,
                ScreenTransitionType.Fade,
                ct);
        }

        public static async UniTask LoadExploreSceneWithFadeAsync(
            float fadeOutDuration = 0.4f,
            float fadeInDuration  = 0.4f,
            CancellationToken ct  = default)
        {
            await ScreenTransitionManager.Instance
                .FlashAsync(fadeOutDuration, fadeInDuration, ScreenTransitionType.Fade, ct);

            GamePlaySceneUtil.LoadExploreScene();

            await ScreenTransitionManager.Instance
                .FlashAsync(fadeOutDuration, fadeInDuration, ScreenTransitionType.Fade, ct);
        }
    }
}
