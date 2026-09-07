using Core.Scripts.Foundation.Define;
using GamePlay.Common.Scripts.Entities.Skills;
using GamePlay.Features.Battle.Scripts.BattleAction;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.Unit.Components.AI
{
    /// <summary>
    /// 모든 행동 조합을 만들지 않고, 정해진 우선순위 상태와 소수 후보만 비교한다.
    /// 상태 우선순위: 유효한 밀치기 -> 공격 -> 후퇴 -> 일반 밀치기 -> 층/틈 이동 -> 접근 -> 대기.
    /// </summary>
    public sealed class EnemyTurnPlanner
    {
        private readonly AIContext _context;
        private readonly CharBase _actor;

        public EnemyTurnPlanner(AIContext context)
        {
            _context = context;
            _actor = context.Actor;
        }

        public EnemyTurnPlan Decide(bool allowExtraAction = true)
        {
            if (!_context.HasPrimaryTarget)
                return Wait("살아 있는 적이 없음");

            EnemyTurnPlan push = allowExtraAction ? FindBestPushPlan() : null;
            EnemyTurnPlan attack = FindBestAttackPlan();
            EnemyTurnPlan retreat = _context.LowHP ? BuildRetreatPlan() : null;
            EnemyTurnPlan retreatJump = _context.LowHP && allowExtraAction && retreat == null
                ? BuildJumpPlan(retreat: true)
                : null;
            EnemyTurnPlan jump = allowExtraAction ? BuildJumpPlan(retreat: false) : null;
            EnemyTurnPlan approach = BuildApproachPlan();

            EnemyDecisionFacts facts = new()
            {
                HasLivingTarget = true,
                HasLethalPush = push != null && push.Score >= 5000f,
                HasAttack = attack != null,
                LowHP = _context.LowHP,
                CanRetreat = retreat != null,
                CanRetreatJump = retreatJump != null,
                HasPush = push != null,
                ShouldJumpBeforeWalking = jump != null && ShouldJumpBeforeWalking(jump),
                CanApproach = approach != null,
                CanJump = jump != null
            };

            return EnemyDecisionPolicy.Decide(facts) switch
            {
                EnemyDecisionState.Attack => attack,
                EnemyDecisionState.Push => push,
                EnemyDecisionState.Retreat => retreat,
                EnemyDecisionState.ChangeLevel => retreatJump ?? jump,
                EnemyDecisionState.Approach => approach,
                _ => Wait("현재 위치에서 목표에게 갈 수 있는 안전한 행동이 없음")
            };
        }

        private EnemyTurnPlan FindBestAttackPlan()
        {
            EnemyTurnPlan best = null;

            foreach (Vector2Int position in _context.MovableCells)
            {
                int moveCost = Mathf.Abs(position.x - _context.CurrentCell.x);
                foreach (SkillModel skill in _actor.SkillInfo.SkillSlots)
                {
                    if (!IsOffensiveSkill(skill))
                        continue;

                    foreach (bool faceRight in new[] { true, false })
                    {
                        BattleActionPreviewData range = BattleRangeHelper.ComputeSkillRange(
                            _context.Grid,
                            skill.SkillRange,
                            _actor.GetCharType(),
                            position,
                            faceRight);

                        foreach (CharBase enemy in _context.AllEnemies)
                        {
                            Vector2Int targetCell = _context.CellOf(enemy);
                            if (!range.PossibleCells.Contains(targetCell))
                                continue;

                            float hpRatio = enemy.MaxHP > 0 ? enemy.CurrentHP / enemy.MaxHP : 1f;
                            float damageValue = skill.SkillDamage != null
                                ? Mathf.Max(0f, skill.SkillDamage.DamageCoefficient)
                                : 0f;
                            float score = 2000f;
                            score += (1f - hpRatio) * 300f;
                            score += damageValue * 20f;
                            score -= moveCost * 5f;

                            if (enemy == _context.PrimaryTarget)
                                score += 25f;

                            int safety = _context.DistanceToNearestEnemy(position);
                            score += _context.LowHP ? safety * 20f : -safety * 2f;

                            if (best == null || score > best.Score)
                            {
                                best = new EnemyTurnPlan
                                {
                                    State = EnemyDecisionState.Attack,
                                    MoveTo = position == _context.CurrentCell ? null : position,
                                    TargetCell = targetCell,
                                    Target = enemy,
                                    Skill = skill,
                                    FaceRight = faceRight,
                                    Score = score,
                                    Reason = _context.LowHP
                                        ? "공격 가능한 위치 중 생존 거리와 마무리 가능성을 우선"
                                        : "공격 가능한 위치 중 낮은 체력과 스킬 효율을 우선"
                                };
                            }
                        }
                    }
                }
            }

            return best;
        }

        private EnemyTurnPlan FindBestPushPlan()
        {
            EnemyTurnPlan best = null;

            foreach (Vector2Int position in _context.MovableCells)
            {
                foreach (Vector2Int direction in new[] { Vector2Int.left, Vector2Int.right })
                {
                    Vector2Int targetCell = position + direction;
                    CharBase target = _context.Grid.GetUnitAt(targetCell);
                    if (!target || target.CurrentHP <= 0 || target.GetCharType() == _actor.GetCharType())
                        continue;

                    PushEngine.PushResult result = PushEngine.ComputePushResult(position, targetCell, _context.Grid);
                    float hpRatio = target.MaxHP > 0 ? target.CurrentHP / target.MaxHP : 1f;
                    bool lethal = result.Result == PushEngine.VictimResult.Fall ||
                                  result.Result == PushEngine.VictimResult.Land && result.FallCells * 0.3f >= hpRatio ||
                                  result.Result == PushEngine.VictimResult.WallSmack && hpRatio <= 0.3f;

                    float score = lethal ? 5000f : result.Result switch
                    {
                        PushEngine.VictimResult.Land => 1500f + result.FallCells * 150f,
                        PushEngine.VictimResult.WallSmack => 1450f,
                        _ => 1100f
                    };
                    score -= Mathf.Abs(position.x - _context.CurrentCell.x) * 5f;

                    if (best == null || score > best.Score)
                    {
                        best = new EnemyTurnPlan
                        {
                            State = EnemyDecisionState.Push,
                            MoveTo = position == _context.CurrentCell ? null : position,
                            TargetCell = targetCell,
                            Target = target,
                            FaceRight = direction.x > 0,
                            Score = score,
                            Reason = lethal ? "낙사 또는 밀치기 피해로 처치 가능" : "공격은 불가능하지만 유효한 밀치기가 가능"
                        };
                    }
                }
            }

            return best;
        }

        private EnemyTurnPlan BuildRetreatPlan()
        {
            int currentSafety = _context.DistanceToNearestEnemy(_context.CurrentCell);
            Vector2Int bestCell = _context.CurrentCell;
            int bestSafety = currentSafety;
            int bestMoveCost = int.MaxValue;

            foreach (Vector2Int cell in _context.MovableCells)
            {
                int safety = _context.DistanceToNearestEnemy(cell);
                int moveCost = Mathf.Abs(cell.x - _context.CurrentCell.x);
                if (safety > bestSafety || safety == bestSafety && moveCost < bestMoveCost)
                {
                    bestCell = cell;
                    bestSafety = safety;
                    bestMoveCost = moveCost;
                }
            }

            if (bestCell == _context.CurrentCell || bestSafety <= currentSafety)
                return null;

            return new EnemyTurnPlan
            {
                State = EnemyDecisionState.Retreat,
                MoveTo = bestCell,
                TargetCell = bestCell,
                Score = bestSafety,
                Reason = "체력 30% 이하이며 이번 턴에 공격할 수 없어 가장 먼 칸으로 후퇴"
            };
        }

        private EnemyTurnPlan BuildApproachPlan()
        {
            Vector2Int targetCell = _context.CellOf(_context.PrimaryTarget);
            int currentDistance = AIContext.Distance(_context.CurrentCell, targetCell);
            Vector2Int bestCell = _context.CurrentCell;
            int bestDistance = currentDistance;
            int bestMoveCost = int.MaxValue;

            foreach (Vector2Int cell in _context.MovableCells)
            {
                int distance = AIContext.Distance(cell, targetCell);
                int moveCost = Mathf.Abs(cell.x - _context.CurrentCell.x);
                if (distance < bestDistance || distance == bestDistance && moveCost < bestMoveCost)
                {
                    bestCell = cell;
                    bestDistance = distance;
                    bestMoveCost = moveCost;
                }
            }

            if (bestCell == _context.CurrentCell || bestDistance >= currentDistance)
                return null;

            return new EnemyTurnPlan
            {
                State = EnemyDecisionState.Approach,
                MoveTo = bestCell,
                TargetCell = bestCell,
                Target = _context.PrimaryTarget,
                Score = currentDistance - bestDistance,
                Reason = "가장 가까운 적과의 거리를 안전하게 단축"
            };
        }

        private EnemyTurnPlan BuildJumpPlan(bool retreat)
        {
            BattleActionPreviewData jumpRange = BattleRangeHelper.ComputeJumpRangeFromPos(
                _context.Grid,
                _context.CurrentCell);
            Vector2Int targetCell = _context.CellOf(_context.PrimaryTarget);
            int currentDistance = retreat
                ? _context.DistanceToNearestEnemy(_context.CurrentCell)
                : AIContext.Distance(_context.CurrentCell, targetCell);

            Vector2Int? bestCell = null;
            int bestValue = 0;
            foreach (Vector2Int cell in jumpRange.PossibleCells)
            {
                int nextDistance = retreat
                    ? _context.DistanceToNearestEnemy(cell)
                    : AIContext.Distance(cell, targetCell);
                int improvement = retreat
                    ? nextDistance - currentDistance
                    : currentDistance - nextDistance;

                if (improvement > bestValue)
                {
                    bestValue = improvement;
                    bestCell = cell;
                }
            }

            if (!bestCell.HasValue)
                return null;

            return new EnemyTurnPlan
            {
                State = EnemyDecisionState.ChangeLevel,
                TargetCell = bestCell.Value,
                Target = _context.PrimaryTarget,
                Score = bestValue,
                Reason = retreat
                    ? "걷기로 거리를 벌릴 수 없어 점프로 이탈"
                    : "다른 층 또는 끊긴 발판을 점프로 통과"
            };
        }

        private bool ShouldJumpBeforeWalking(EnemyTurnPlan jump)
        {
            Vector2Int targetCell = _context.CellOf(_context.PrimaryTarget);
            int verticalGap = Mathf.Abs(targetCell.y - _context.CurrentCell.y);
            return verticalGap > 0 && jump.TargetCell.HasValue &&
                   Mathf.Abs(targetCell.y - jump.TargetCell.Value.y) < verticalGap;
        }

        private static bool IsOffensiveSkill(SkillModel skill)
        {
            if (skill == null || skill.SkillRange == null)
                return false;

            return skill.SkillType == SystemEnum.eSkillType.PhysicalAttack ||
                   skill.SkillType == SystemEnum.eSkillType.MagicAttack ||
                   skill.SkillType == SystemEnum.eSkillType.Debuff;
        }

        private static EnemyTurnPlan Wait(string reason) => new()
        {
            State = EnemyDecisionState.Wait,
            Reason = reason
        };
    }
}
