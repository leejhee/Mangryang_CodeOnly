using Core.Scripts.Foundation.Define;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Entities.Character;
using GamePlay.Common.Scripts.Novel;
using GamePlay.Common.Scripts.Scene;
using GamePlay.Common.Scripts.Transition;
using GamePlay.Features.Explore.Scripts;
using System.Threading;

namespace GamePlay.Common.Scripts.NewGameUtil
{
    public static class NewGameStartUtil
    {
        public static async UniTask StartNewGame(CancellationToken ct = default)
        {
            Party newParty = new();
            newParty.InitParty();
            ExploreSession.Instance.SetNewExplore(SystemEnum.Dungeon.TUTORIAL, 1, newParty);

            await ScreenTransitionManager.Instance.RunEnterFadeAsync(
                () => NovelDomainPlayer.PlayNovelScript("1", ct),
                outDuration: 0.25f,
                inDuration: 0.25f,
                type: ScreenTransitionType.Fade,
                ct: ct);
            
            await FlowHelper.LoadExploreSceneWithFadeAsync(ct: ct);
        }
    }
}