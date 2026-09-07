using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Contracts;
using GamePlay.Common.Scripts.Keyword;
using GamePlay.Common.Scripts.Skill;
using GamePlay.Features.Battle.Scripts.BattleMap;
using GamePlay.Features.Battle.Scripts.Unit;
using System.Collections.Generic;
using System.Threading;

namespace GamePlay.Features.Battle.Scripts.BattleAction
{
    public sealed class BattleKeywordTriggeredSkillExecutor : IKeywordTriggeredSkillExecutor
    {
        private readonly BattleStageGrid _grid;

        public BattleKeywordTriggeredSkillExecutor(BattleStageGrid grid)
        {
            _grid = grid;
        }

        public async UniTask<bool> ExecuteAsync(
            long skillID,
            CharBase caster,
            CharBase target,
            CancellationToken ct)
        {
            if (skillID <= 0 ||
                !caster || caster.IsDead ||
                !target || target.IsDead ||
                caster.SkillInfo == null)
            {
                return false;
            }

            List<IDamageable> targets = new(1) { target };
            BattleSkillTimelineEventSink sink = new(caster, targets, _grid);
            SkillParameter parameter = new(caster, targets, _grid, sink);

            KeywordDebugLogger.TriggeredSkill(caster, skillID, target);

            return await caster.SkillInfo.PlayTriggeredSkillAsync(
                skillID,
                parameter,
                ct);
        }
    }
}
