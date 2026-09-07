using Core.Scripts.Foundation.Define;
using Cysharp.Threading.Tasks;
using GamePlay.Features.Battle.Scripts.Unit;
using System;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.BattleTurn
{
    public class Turn
    {
        public enum Side { None, Player, Enemy, Neutral, SideMax }

        public CharBase TurnOwner { get; private set; }
        public Side WhoseSide { get; private set; }
        public bool IsValid => TurnOwner && !_isDead;

        private TurnActionState _actionState = new();
        
        private bool _isDead = false;
        private event Action OnBeginTurn =     delegate { };
        private event Action OnEndTurn =       delegate { };

        private event Action TurnOwnerOutline;
        public event Action OnAITurnCompleted;
        public event Action<TurnActionDTO> OnTurnAction;
        
        
        public Turn(CharBase turnOwner)
        {
            TurnOwner = turnOwner;
            WhoseSide = turnOwner.GetCharType() == SystemEnum.eCharType.Enemy ?
                Side.Enemy : Side.Player;
            
            OnBeginTurn += DefaultTurnBegin;
            OnEndTurn += DefaultTurnEnd;
        }
        
        public void Begin() => OnBeginTurn?.Invoke();
        public void End() => OnEndTurn?.Invoke();

        public void KillTurn()
        {
            _isDead = true;
            if (TurnOwner)
            {
                if (TurnOwnerOutline != null)
                {
                    TurnOwner.OnUpdate -= TurnOwnerOutline;
                    TurnOwnerOutline = null;
                }
                TurnOwner.ClearOutline();
            }
            OnAITurnCompleted = null;
        }
        
        private void DefaultTurnBegin()
        {
            #region Action Point Initialize
            
            long maxMovePoint = TurnOwner.RuntimeStat.GetStat(SystemEnum.eStats.NMACTION_POINT);
            long currentMovePoint = TurnOwner.RuntimeStat.GetStat(SystemEnum.eStats.NACTION_POINT);
            _actionState.Initialize(maxMovePoint);
            
            if (currentMovePoint != maxMovePoint)
            {
                long delta = maxMovePoint - currentMovePoint;
                TurnOwner.RuntimeStat.ChangeStat(SystemEnum.eStats.NACTION_POINT, delta);
                Debug.Log($"[Turn] {TurnOwner.name} 이동력 회복: {currentMovePoint} -> {maxMovePoint}");
            }
            #endregion
                
            #region Control Logic
            
            RaiseTurnActionChanged();
            
            if (TurnOwner.GetCharType() == SystemEnum.eCharType.Enemy)
            {
                CharMonster monster = TurnOwner as CharMonster;
                if (monster)
                {
                    Debug.Log($"[AI] {monster.name} AI 실행 시작");
                    ExecuteMonsterAI(monster).SuppressCancellationThrow().Forget();
                }
                else
                {
                    Debug.LogWarning($"[Turn] {TurnOwner.name}은 CharMonster가 아닙니다.");
                }
            }
            
            TurnOwnerOutline = () => TurnOwner.OutlineCharacter(Color.green, 10f);
            TurnOwner.OnUpdate += TurnOwnerOutline;
            
            #endregion
        }
        
        private async UniTask ExecuteMonsterAI(CharMonster monster)
        {
            string monsterName = monster.name;

            try
            {
                await monster.ExecuteAITurn(this);
                Debug.Log($"[Turn] {monsterName} AI 행동 완료");
                
                TurnActionUtility.LogActionState(monsterName, _actionState);
                
                await UniTask.Delay(500);
                Debug.Log($"[Turn] {monsterName} AI 턴 종료");
                
                OnAITurnCompleted?.Invoke();
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[Turn] {monsterName} AI 행동이 취소되어 턴을 종료합니다.");
                OnAITurnCompleted?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"[Turn] {monsterName} AI 실행 중 오류 발생, 강제 턴 종료");
                
                OnAITurnCompleted?.Invoke();
            }
        }
        
        private void DefaultTurnEnd()
        {
            if (TurnOwner.GetCharType() == SystemEnum.eCharType.Enemy)
            {
                Debug.Log($"[Turn] {TurnOwner.name} 적 턴 종료 처리");
            }
            else
            {
                Debug.Log($"[Turn] {TurnOwner.name} 플레이어 턴 종료 처리");
            }

            TurnOwner.OnUpdate -= TurnOwnerOutline;
            TurnOwner.ClearOutline();
        }
        
        private void RaiseTurnActionChanged()
        {
            if (OnTurnAction == null) return;
            TurnActionDTO dto = new(TurnOwner.GetID(), _actionState);
            OnTurnAction.Invoke(dto);
        }
        
        /// <summary>
        /// 행동 수행 가능 여부 검증
        /// </summary>
        public bool CanPerformAction(TurnActionState.ActionCategory category, int moveDistance=0)
        {
            return _actionState.CanPerformAction(category, moveDistance);
        }
        
        /// <summary>
        /// 이동 실행 (이동력 소모)
        /// </summary>
        public bool TryConsumeMove(float distance)
        {
            if (!_actionState.ConsumeMovePoint(distance))
            {
                return false;
            }
            
            TurnOwner.RuntimeStat.ChangeStat(SystemEnum.eStats.NACTION_POINT, -(long)distance);
            RaiseTurnActionChanged();
            Debug.Log($"[Turn] {TurnOwner.name} 이동력 소모 완료: {distance} (남은: {_actionState.RemainingMovePoint})");
            return true;
        }

        /// <summary>
        /// 주요 행동 실행 (밀기/점프/스킬)
        /// </summary>
        public bool TryUseSkill()
        {
            if (!_actionState.UseSkillAction())
                return false;
            RaiseTurnActionChanged();
            return true;
        }

        public bool TryUseExtra()
        {
            if (!_actionState.UseExtraAction())
                return false;
            RaiseTurnActionChanged();
            return true;
        }
    }
}
