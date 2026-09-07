using Core.Scripts.Data;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Contracts;
using GamePlay.Common.Scripts.Entities.Skills;
using GamePlay.Common.Scripts.Skill;
using GamePlay.Features.Battle.Scripts.BattleMap;
using GamePlay.Features.Battle.Scripts.Unit;
using System;
using System.Collections.Generic;
using System.Threading;

namespace GamePlay.Features.Battle.Scripts.BattleAction
{
    public class SkillBattleAction : BattleActionBase
    {
        public SkillBattleAction(BattleActionContext ctx) : base(ctx)
        { }

        public override UniTask<BattleActionPreviewData> BuildActionPreview(CancellationToken ct)
        {
            if(Context == null) throw new InvalidOperationException("[SkillBattleAction] - Context is null");
            
            SkillModel model = Context.skillModel;
            if(model == null) throw new InvalidOperationException("[SkillBattleAction] - SkillModel is null");
            SkillRangeData range = model.SkillRange;
            
            CharBase actor = Context.actor;
            BattleStageGrid stageGrid = Context.battleField.GetComponent<BattleStageGrid>();
            BattleActionPreviewData data = BattleRangeHelper.ComputeSkillRange(stageGrid, range, actor);
            
            return UniTask.FromResult(data);
        }

        public override async UniTask<BattleActionResult> ExecuteAction(CancellationToken ct)
        {
            if (Context.TargetCell == null || Context.actor == null || Context.battleField == null)
                return BattleActionResult.Fail(BattleActionResult.ResultReason.InvalidTarget);
            if(Context.skillModel == null)
                return BattleActionResult.Fail(BattleActionResult.ResultReason.InvalidContext);

            CharBase caster = Context.actor;
            List<IDamageable> targets = Context.targets ?? new List<IDamageable>();

            BattleStageGrid grid = Context.battleField.GetComponent<BattleStageGrid>();
            BattleSkillTimelineEventSink timelineEventSink = new(caster, targets, grid);
            SkillParameter parameter = new(
                caster: caster,
                target: targets,
                grid: grid,
                timelineEventSink: timelineEventSink);
            
            bool played = await caster.SkillInfo.PlaySkillAsync(Context.skillModel.SkillIndex, parameter, ct);
            if (!played)
                return BattleActionResult.Fail(BattleActionResult.ResultReason.InvalidContext);
            
            return BattleActionResult.Success();
        }
        
    }
}
