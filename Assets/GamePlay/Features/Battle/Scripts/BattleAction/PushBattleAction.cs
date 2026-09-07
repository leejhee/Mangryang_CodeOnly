using Cysharp.Threading.Tasks;
using GamePlay.Features.Battle.Scripts.BattleMap;
using GamePlay.Features.Battle.Scripts.Unit;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.BattleAction
{
    public class PushBattleAction : BattleActionBase
    {
        // 앞뒤만 밀 수 있다
        private static readonly List<Vector2Int> PushableRange = new() { new Vector2Int(-1, 0), new Vector2Int(1, 0) };
        
        public PushBattleAction(BattleActionContext ctx) : base(ctx)
        { }
        
        public override UniTask<BattleActionPreviewData> BuildActionPreview(CancellationToken ct)
        {
            if(Context == null) throw new InvalidOperationException("Context is null");

            CharBase actor = Context.actor;
            
            List<Vector2Int> pushablePoints = new();
            List<Vector2Int> blockedPoints = new();
            List<Vector2Int> maskedCells = new();
            
            BattleStageGrid stageGrid = Context.battleField.GetComponent<BattleStageGrid>();
            Vector2Int pivot = stageGrid.WorldToCell(actor.CharTransform.position);

            foreach (Vector2Int point in PushableRange)
            {
                Vector2Int candidate = pivot + point;
                if (stageGrid.IsMaskable(candidate))
                {
                    maskedCells.Add(candidate);
                }
                else if (stageGrid.IsOccupied(candidate) && stageGrid.IsPlatform(candidate))
                {
                    pushablePoints.Add(candidate);
                }
                else
                {
                    blockedPoints.Add(candidate);
                }
            }
            // 상태 머신 그냥 안만들기 위해서
            return UniTask.FromResult(new BattleActionPreviewData(pushablePoints, blockedPoints, maskedCells));
        }

        public override async UniTask<BattleActionResult> ExecuteAction(CancellationToken ct)
        {
            if(Context == null || !Context.actor || !Context.battleField)
                return BattleActionResult.Fail(BattleActionResult.ResultReason.InvalidContext);
            if(Context.TargetCell == null)
                return BattleActionResult.Fail(BattleActionResult.ResultReason.InvalidTarget);

            StageField stage = Context.battleField;
            BattleStageGrid grid = stage.GetComponent<BattleStageGrid>();
            
            CharBase actor = Context.actor;
            Vector2Int pivot = grid.WorldToCell(actor.CharTransform.position);
            
            Vector2Int targetingCell = Context.TargetCell.Value;
            CharBase victim = grid.GetUnitAt(targetingCell);

            PushEngine.PushResult res = PushEngine.ComputePushResult(pivot, targetingCell, grid);
            actor.CharPushPlay(grid.CellToWorldCenter(targetingCell));
            await PushEngine.ApplyPushResult(
                victim, res, grid, ct, 
                BattleController.Instance.CameraDriver, true, true);
            actor.CharReturnIdle();
            return BattleActionResult.Success();
            
        }
    }
}