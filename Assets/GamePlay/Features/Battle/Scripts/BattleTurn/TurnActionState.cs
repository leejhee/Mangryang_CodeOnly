using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.BattleTurn
{
    public class TurnActionState
    {
        // 행동 타입 정의
        public enum ActionCategory
        {
            None, // 무효
            Move, // 이동 
            SkillAction, // 스킬 
            ExtraAction // 밀기 / 점프 / 인벤 상호작용
        }
        
        #region Fields
        // 이동력 관리
        private float _maxMovePoint;
        private float _remainingMovePoint;
        
        // 주요 행동 사용 여부
        private bool _extraActionUsed;
        private bool _skillActionUsed;
        
        #endregion
        
        #region Properties
        public float MaxMovePoint => _maxMovePoint;
        public float RemainingMovePoint => _remainingMovePoint;
        public bool ExtraActionUsed => _extraActionUsed;
        public bool SkillActionUsed => _skillActionUsed;
        
        #endregion
        
        /// <summary>
        /// 턴 시작 시 행동 상태 초기화
        /// </summary>
        public void Initialize(float movePoint)
        {
            _maxMovePoint = movePoint;
            _remainingMovePoint = movePoint;
            _skillActionUsed = false;
            _extraActionUsed = false;
        }
        
        /// <summary>
        /// 행동 수행 가능 여부 검증
        /// </summary>
        public bool CanPerformAction(ActionCategory category, int moveDistance)
        {
            return category switch
            {
                ActionCategory.Move => //남아는 있어야 하고, moveDistance 이상으로 남아야 하니까.
                    _remainingMovePoint > 0 && _remainingMovePoint >= moveDistance,
                ActionCategory.SkillAction => !_skillActionUsed,
                ActionCategory.ExtraAction => !_extraActionUsed,
                _ => false
            };
        }
        
        /// <summary>
        /// 이동력 소모 (이동 시 호출)
        /// </summary>
        public bool ConsumeMovePoint(float amount)
        {
            if (_remainingMovePoint < amount)
            {
                Debug.LogWarning($"이동력 부족: 필요 {amount}, 남은 {_remainingMovePoint}");
                return false;
            }
            
            _remainingMovePoint -= amount;
            Debug.Log($"이동력 소모: {amount} (남은 이동력: {_remainingMovePoint}/{_maxMovePoint})");
            return true;
        }
        
        public bool UseSkillAction()
        {
            if (_skillActionUsed)
            {
                Debug.LogWarning("이미 스킬을 사용했습니다. (스킬은 턴당 1회)");
                return false;
            }
            
            _skillActionUsed = true;
            Debug.Log("스킬 사용됨 (이번 턴에 추가 스킬 불가)");
            return true;
        }
        
        // TODO : Jump와 Push를 구별해야 할지 정할 것.
        public bool UseExtraAction()
        {
            if (_extraActionUsed)
            {
                Debug.LogWarning("이미 부가 행동을 사용했습니다.");
                return false;
            }
            
            _extraActionUsed = true;
            Debug.Log("부가 행동 사용됨 (이번 턴에 추가 스킬 불가)");
            return true;
        }
        
        /// <summary>
        /// 현재 행동 가능 상태 요약 (디버깅/UI용)
        /// </summary>
        public string GetStatusSummary()
        {
            return $"이동력: {_remainingMovePoint:F1}/{_maxMovePoint:F1} | " +
                   $"주요행동: {(_skillActionUsed ? "사용완료" : "사용가능")}";
        }
    }
}