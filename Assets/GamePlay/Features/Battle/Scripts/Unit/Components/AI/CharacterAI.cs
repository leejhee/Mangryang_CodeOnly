using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Contracts;
using GamePlay.Common.Scripts.Entities.Skills;
using GamePlay.Features.Battle.Scripts.BattleAction;
using GamePlay.Features.Battle.Scripts.BattleMap;
using GamePlay.Features.Battle.Scripts.BattleTurn;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.Unit.Components.AI
{
    public abstract class CharacterAI
    {
        protected readonly CharBase Owner;
        protected Turn CurrentTurn { get; private set; }
        protected StageField StageField { get; private set; }
        protected BattleStageGrid Grid { get; private set; }

        protected CharacterAI(CharBase owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        /// <summary>
        /// 턴 시스템에서 호출하는 진입점
        /// - Turn 세팅
        /// - 필드/그리드 초기화
        /// - 실제 턴 로직 실행
        /// </summary>
        public async UniTask ExecuteTurn(Turn turn)
        {
            CurrentTurn = turn ?? throw new ArgumentNullException(nameof(turn));

            if (!TryInitFieldAndGrid())
                return;

            await ExecuteTurnInternal();
        }

        /// <summary>
        /// 구현부
        /// </summary>
        protected abstract UniTask ExecuteTurnInternal();
        
        protected virtual bool TryInitFieldAndGrid()
        {
            StageField = BattleController.Instance.StageField;
            if (!StageField)
            {
                Debug.LogError("[AI] StageField를 찾을 수 없습니다.");
                return false;
            }

            Grid = StageField.GetComponent<BattleStageGrid>();
            if (!Grid)
            {
                Debug.LogError("[AI] BattleStageGrid를 찾을 수 없습니다.");
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// 타깃 셀 방향으로 방향 전환
        /// </summary>
        protected virtual void AdjustDirection(Vector2Int targetCell)
        {
            Vector2Int currentCell = Grid.WorldToCell(Owner.CharTransform.position);
            bool shouldFaceRight = targetCell.x > currentCell.x;

            if (Owner.LastDirectionRight != shouldFaceRight)
            {
                Owner.LastDirectionRight = shouldFaceRight;
            }
        }

        /// <summary>
        /// 이동
        /// </summary>
        protected virtual async UniTask<bool> ExecuteMove(Vector2Int target)
        {
            Vector2Int currentPos = Grid.WorldToCell(Owner.CharTransform.position);
            int moveDistance = Mathf.Abs(target.x - currentPos.x);

            if (!CurrentTurn.CanPerformAction(TurnActionState.ActionCategory.Move, moveDistance))
            {
                Debug.LogWarning("[AI] 이동력 부족");
                return false;
            }

            var context = new BattleActionContext
            {
                battleActionType = ActionType.Move,
                actor = Owner,
                battleField = StageField,
                TargetCell = target
            };

            var moveAction = new MoveBattleAction(context);
            var result = await moveAction.ExecuteAction(CancellationToken.None);

            if (result.ActionSuccess)
            {
                CurrentTurn.TryConsumeMove(moveDistance);
                Debug.Log($"[AI] 이동 성공: {currentPos} to {target}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 스킬 실행
        /// </summary>
        protected virtual async UniTask<bool> ExecuteSkill(
            SkillModel skill,
            Vector2Int targetCell,
            List<IDamageable> targets)
        {
            if (!CurrentTurn.CanPerformAction(TurnActionState.ActionCategory.SkillAction))
            {
                Debug.LogWarning("[AI] 주요 행동 사용 불가");
                return false;
            }

            if (skill == null)
            {
                Debug.LogWarning("[AI] 스킬 정보 없음");
                return false;
            }

            var context = new BattleActionContext
            {
                battleActionType = ActionType.Skill,
                actor = Owner,
                battleField = StageField,
                skillModel = skill,
                TargetCell = targetCell,
                targets = targets
            };

            var action = new SkillBattleAction(context);
            var result = await action.ExecuteAction(CancellationToken.None);

            if (result.ActionSuccess)
            {
                CurrentTurn.TryUseSkill();
                Debug.Log($"[AI] 스킬 성공: {skill.SkillName}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 푸시 액션
        /// </summary>
        protected virtual async UniTask<bool> ExecutePush(Vector2Int targetCell)
        {
            if (!CurrentTurn.CanPerformAction(TurnActionState.ActionCategory.ExtraAction))
            {
                Debug.LogWarning("[AI] 주요 행동 사용 불가 (Push)");
                return false;
            }

            var context = new BattleActionContext
            {
                battleActionType = ActionType.Push,
                actor = Owner,
                battleField = StageField,
                TargetCell = targetCell
            };

            var action = new PushBattleAction(context);
            var result = await action.ExecuteAction(CancellationToken.None);

            if (result.ActionSuccess)
            {
                CurrentTurn.TryUseExtra();
                Debug.Log($"[AI] 푸시 성공: {targetCell}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 점프 액션
        /// </summary>
        protected virtual async UniTask<bool> ExecuteJump(Vector2Int targetCell)
        {
            if (!CurrentTurn.CanPerformAction(TurnActionState.ActionCategory.ExtraAction))
            {
                Debug.LogWarning("[AI] 주요 행동 사용 불가 (Jump)");
                return false;
            }

            var context = new BattleActionContext
            {
                battleActionType = ActionType.Jump,
                actor = Owner,
                battleField = StageField,
                TargetCell = targetCell
            };

            var action = new JumpBattleAction(context);
            var result = await action.ExecuteAction(CancellationToken.None);

            if (result.ActionSuccess)
            {
                CurrentTurn.TryUseExtra();
                Debug.Log($"[AI] 점프 성공: {targetCell}");
                return true;
            }

            return false;
        }

    }
}
