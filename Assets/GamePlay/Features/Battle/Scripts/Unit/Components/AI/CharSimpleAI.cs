using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Contracts;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.Unit.Components.AI
{
    /// <summary>
    /// 턴마다 전장을 다시 읽고 하나의 명확한 상태를 선택하는 적 AI.
    /// Behavior Tree 대신 전투 규칙에 맞춘 얕은 우선순위 상태 머신을 사용한다.
    /// </summary>
    public sealed class CharSimpleAI : CharacterAI
    {
        public CharSimpleAI(CharBase owner) : base(owner)
        {
        }

        protected override async UniTask ExecuteTurnInternal()
        {
            await UniTask.Delay(500);

            AIContext context = new(Owner, Grid);
            context.AnalyzeSituation();

            EnemyTurnPlan plan = new EnemyTurnPlanner(context).Decide();
            Debug.Log($"[AI] {Owner.name} 결정: {plan}");

            bool success = await ExecutePlan(plan);
            if (!success)
            {
                Debug.LogWarning($"[AI] {Owner.name} 행동 실행 실패: {plan.State}");
                return;
            }

            // 점프는 부가 행동이므로, 착지 후 남은 이동/스킬 행동을 한 번 더 판단한다.
            if (plan.State == EnemyDecisionState.ChangeLevel)
            {
                context.AnalyzeSituation();
                EnemyTurnPlan followUp = new EnemyTurnPlanner(context).Decide(allowExtraAction: false);
                if (followUp.State != EnemyDecisionState.Wait)
                {
                    Debug.Log($"[AI] {Owner.name} 점프 후 결정: {followUp}");
                    await ExecutePlan(followUp);
                }
            }

            await UniTask.Delay(300);
        }

        private async UniTask<bool> ExecutePlan(EnemyTurnPlan plan)
        {
            if (plan.MoveTo.HasValue)
            {
                bool moved = await ExecuteMove(plan.MoveTo.Value);
                if (!moved)
                    return false;
            }

            if (plan.TargetCell.HasValue &&
                (plan.State == EnemyDecisionState.Attack || plan.State == EnemyDecisionState.Push))
                Owner.LastDirectionRight = plan.FaceRight;

            switch (plan.State)
            {
                case EnemyDecisionState.Attack:
                    if (plan.Skill == null || !plan.Target || !plan.TargetCell.HasValue)
                        return false;

                    return await ExecuteSkill(
                        plan.Skill,
                        plan.TargetCell.Value,
                        new List<IDamageable> { plan.Target });

                case EnemyDecisionState.Push:
                    return plan.TargetCell.HasValue && await ExecutePush(plan.TargetCell.Value);

                case EnemyDecisionState.ChangeLevel:
                    return plan.TargetCell.HasValue && await ExecuteJump(plan.TargetCell.Value);

                case EnemyDecisionState.Approach:
                case EnemyDecisionState.Retreat:
                    return plan.MoveTo.HasValue;

                case EnemyDecisionState.Wait:
                    await UniTask.Delay(250);
                    return true;

                default:
                    return false;
            }
        }
    }
}
