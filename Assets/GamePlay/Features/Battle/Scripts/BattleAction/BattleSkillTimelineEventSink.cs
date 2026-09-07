using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Contracts;
using GamePlay.Common.Scripts.Entities.Character.Components;
using GamePlay.Common.Scripts.Entities.Skills;
using GamePlay.Common.Scripts.Keyword;
using GamePlay.Common.Scripts.Skill;
using GamePlay.Features.Battle.Scripts.BattleMap;
using GamePlay.Features.Battle.Scripts.Unit;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.BattleAction
{
    /// <summary>
    /// Skill Timeline이 발행한 연출 큐를 실제 전투 효과로 변환합니다.
    /// 회피·데미지 계산은 Timeline Marker가 아니라 전투 계층이 소유합니다.
    /// </summary>
    public sealed class BattleSkillTimelineEventSink : ISkillTimelineEventSink
    {
        private readonly CharBase _caster;
        private readonly IReadOnlyList<IDamageable> _targets;
        private readonly BattleStageGrid _grid;
        private readonly KeywordGrantResolver _keywordGrantResolver = new();
        private readonly IKeywordTriggeredSkillExecutor _triggeredSkillExecutor;

        public BattleSkillTimelineEventSink(
            CharBase caster,
            IReadOnlyList<IDamageable> targets,
            BattleStageGrid grid)
        {
            _caster = caster
                ? caster
                : throw new ArgumentNullException(nameof(caster));
            _targets = targets ?? throw new ArgumentNullException(nameof(targets));
            _grid = grid;
            _triggeredSkillExecutor = new BattleKeywordTriggeredSkillExecutor(grid);
        }

        public UniTask PublishAsync(
            SkillTimelineCue cue,
            SkillModel model,
            CancellationToken ct)
        {
            return cue switch
            {
                SkillTimelineCue.Impact => ApplyImpactAsync(model, ct),
                SkillTimelineCue.Push => ApplyPushAsync(ct),
                SkillTimelineCue.ImpactAndPush => ApplyImpactAndPushAsync(model, ct),
                SkillTimelineCue.ApplyKeywordGrants => ApplyKeywordGrantsAsync(model, ct),
                _ => throw new ArgumentOutOfRangeException(nameof(cue), cue, null)
            };
        }

        private UniTask ApplyKeywordGrantsAsync(SkillModel model, CancellationToken ct)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            return _keywordGrantResolver.ApplySkillGrantsAsync(
                model.SkillIndex,
                _caster,
                _targets,
                ct);
        }

        private async UniTask ApplyImpactAsync(SkillModel model, CancellationToken ct)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            foreach (IDamageable target in _targets)
            {
                ct.ThrowIfCancellationRequested();
                if (target == null) continue;

                DamageParameter parameter = new()
                {
                    Attacker = _caster,
                    Target = target,
                    Model = model
                };

                if (target is CharBase character)
                {
                    bool evaded = await character.TryEvade(parameter);
                    if (evaded) continue;

                    bool intercepted = character.KeywordInfo != null &&
                        await character.KeywordInfo.TryInterceptIncomingHitAsync(
                            parameter,
                            _triggeredSkillExecutor,
                            ct);
                    if (intercepted) continue;

                    await character.SkillDamage(parameter);
                }
                else
                {
                    await target.DamageAsync(1, ct);
                }
            }
        }

        private async UniTask ApplyPushAsync(CancellationToken ct)
        {
            BattleStageGrid grid = RequireGrid();
            CharBase target = FirstCharacterTarget();

            Vector2Int pivot = grid.WorldToCell(_caster.CharTransform.position);
            Vector2Int targetCell = grid.WorldToCell(target.CharTransform.position);
            PushEngine.PushResult result = PushEngine.ComputePushResult(pivot, targetCell, grid);
            await PushEngine.ApplyPushResult(target, result, grid, ct, true);
        }

        private async UniTask ApplyImpactAndPushAsync(
            SkillModel model,
            CancellationToken ct)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (_targets.Count == 0)
                throw new InvalidOperationException("ImpactAndPush requires a target.");

            IDamageable target = _targets[0];
            if (target is not CharBase character)
            {
                await target.DamageAsync(1, ct);
                return;
            }

            DamageParameter parameter = new()
            {
                Attacker = _caster,
                Target = target,
                Model = model
            };

            bool evaded = await character.TryEvade(parameter);
            if (evaded) return;

            bool intercepted = character.KeywordInfo != null &&
                await character.KeywordInfo.TryInterceptIncomingHitAsync(
                    parameter,
                    _triggeredSkillExecutor,
                    ct);
            if (intercepted) return;

            await character.SkillDamage(parameter, false, true);

            BattleStageGrid grid = RequireGrid();
            Vector2Int pivot = grid.WorldToCell(_caster.CharTransform.position);
            Vector2Int targetCell = grid.WorldToCell(character.CharTransform.position);
            PushEngine.PushResult result = PushEngine.ComputePushResult(pivot, targetCell, grid);
            await PushEngine.ApplyPushResult(character, result, grid, ct, true, true);
            character.CharReturnIdle();
        }

        private BattleStageGrid RequireGrid()
        {
            if (!_grid)
                throw new InvalidOperationException("This skill cue requires BattleStageGrid.");
            return _grid;
        }

        private CharBase FirstCharacterTarget()
        {
            foreach (IDamageable target in _targets)
            {
                if (target is CharBase character)
                    return character;
            }

            throw new InvalidOperationException("Push requires a character target.");
        }
    }
}
