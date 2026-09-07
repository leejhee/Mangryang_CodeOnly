using AngelBeat;
using Core.Scripts.Foundation.Define;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Entities.Character.Components;
using GamePlay.Features.Battle.Scripts.BattleMap;
using GamePlay.Features.Battle.Scripts.Unit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GamePlay.Features.Battle.Scripts.BattleAction
{
    public class MoveBattleAction : BattleActionBase
    {
        private static readonly Dictionary<int, float> LevelThresholds = new() 
        { 
            { 1, 1f }, 
            { 2, 1.2f }, 
            { 4, 1.4f },
            { 7, 1.5f } 
        };   
        
        public MoveBattleAction(BattleActionContext ctx) : base(ctx)
        { }

        public override UniTask<BattleActionPreviewData> BuildActionPreview(CancellationToken ct)
        {
            if(Context == null) throw new InvalidOperationException("Context is null");

            // 이동 포인트를 가져오기
            CharBase actor = Context.actor;
            BattleStageGrid stageGrid = Context.battleField.GetComponent<BattleStageGrid>();

            BattleActionPreviewData resultData = BattleRangeHelper.ComputeMoveRangeFromClient(stageGrid, actor);
            return UniTask.FromResult(resultData);
        }

        public override async UniTask<BattleActionResult> ExecuteAction(CancellationToken ct)
        {
            if (Context == null || !Context.actor || !Context.battleField)
                return BattleActionResult.Fail(BattleActionResult.ResultReason.InvalidContext);
            if (Context.TargetCell == null)
                return BattleActionResult.Fail(BattleActionResult.ResultReason.InvalidTarget);
            if (BattleActionRestriction.IsBlocked(Context.actor, ActionType.Move))
            {
                Debug.LogWarning(
                    $"[BattleAction] {Context.actor.name}은 이동 불가 키워드로 Move를 사용할 수 없습니다.",
                    Context.actor);
                return BattleActionResult.Fail(BattleActionResult.ResultReason.RestrictedByKeyword);
            }

            StageField stage = Context.battleField;
            BattleStageGrid grid = stage.GetComponent<BattleStageGrid>();
            CharBase actor = Context.actor;
            Vector3 pos = actor.CharTransform.position;

            Vector2Int goal = Context.TargetCell.Value;
            Vector2Int pivot = grid.WorldToCell(pos);
            float multiplier = 0;
            int xGap = Math.Abs(goal.x - pivot.x);
            foreach (int threshold in LevelThresholds.Keys)
            {
                if (xGap >= threshold)
                    multiplier = LevelThresholds[threshold];
            }
            
            #region 연출
            Vector2 toWorld = stage.CellToWorldCenter(goal);

            BattleCameraDriver driver = BattleController.Instance.CameraDriver;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            UniTask moveTask = actor.CharMove(new Vector2(toWorld.x, pos.y), multiplier);
            if (driver)
            {
                await driver.FollowDuringAsync(actor.CharCameraPos, moveTask, 0.25f, driver.FocusOrthoSize, 0.25f);
            }
            else
            {
                await moveTask; 
            }
            
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            UnityEngine.Debug.Log("RunTime " + elapsedTime);
            #endregion
            
            // 그리드 위치정보 저장
            grid.MoveUnit(actor, goal);
            return BattleActionResult.Success();
        }
    }
}
