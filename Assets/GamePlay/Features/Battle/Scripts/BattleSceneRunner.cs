using Cysharp.Threading.Tasks;
using GamePlay.Features.Battle.Scripts.BattleMap;
using GamePlay.Features.Battle.Scripts.BattleTurn;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamePlay.Features.Battle.Scripts
{
    public static class BattleSceneRunner
    {
        private static int _runGeneration;

        public static void RunAfterLoading(StageField stage, TurnController turn)
        {
            int generation = ++_runGeneration;

            UniTask.Void(async () =>
            {
                try
                {
                    // 로딩씬 완전 언로드까지 대기
                    await UniTask.WaitUntil(() =>
                    {
                        UnityEngine.SceneManagement.Scene s = SceneManager.GetSceneByName("LoadingScene");
                        return !s.IsValid() || !s.isLoaded;
                    });

                    if (generation != _runGeneration || !stage)
                        return;

                    BattleCameraInput input = Object.FindFirstObjectByType<BattleCameraInput>(FindObjectsInactive.Exclude);
                    if (input) input.enableDuringTurn = false;

                    // 전장 전체 인트로
                    BattleController battle = BattleController.Instance;
                    BattleCameraDriver driver = battle ? battle.CameraDriver : null;
                    if (driver && stage)
                        await driver.ShowStageIntro(stage, paddingWorld:1.0f, fadeSeconds:0.8f);

                    if (generation != _runGeneration || !stage)
                        return;

                    if (battle != null)
                        await battle.RaiseBattleStartAsync();

                    if (generation != _runGeneration || !stage || battle == null || battle.IsBattleEnding)
                        return;

                    // 첫 턴 시작
                    if (turn != null)
                        await turn.ChangeTurn();
                    if (input && battle != null && !battle.IsBattleEnding)
                        input.enableDuringTurn = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            });
        }
    }
}
