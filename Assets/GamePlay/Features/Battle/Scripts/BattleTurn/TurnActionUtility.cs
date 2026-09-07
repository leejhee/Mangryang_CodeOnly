using UnityEngine;
using GamePlay.Features.Battle.Scripts.BattleMap;
using GamePlay.Features.Battle.Scripts.Unit;

namespace GamePlay.Features.Battle.Scripts.BattleTurn
{
    /// <summary>
    /// 턴 행동 관련 유틸리티 메서드 모음
    /// </summary>
    public static class TurnActionUtility
    {
        /// <summary>
        /// 그리드 기반 이동 거리 계산
        /// 맨해튼 거리 또는 유클리드 거리 선택 가능
        /// </summary>
        public static float CalculateMoveDistance(
            Vector2Int fromCell, 
            Vector2Int toCell, 
            StageField battleField,
            bool useManhattan = false)
        {
            if (useManhattan)
            {
                // 맨해튼 거리: 그리드 칸 수 기준
                return Mathf.Abs(toCell.x - fromCell.x) + Mathf.Abs(toCell.y - fromCell.y);
            }
            else
            {
                // 유클리드 거리: 실제 월드 좌표 거리
                Vector3 fromPos = battleField.CellToWorldCenter(fromCell);
                Vector3 toPos = battleField.CellToWorldCenter(toCell);
                return Vector3.Distance(fromPos, toPos);
            }
        }
        
        /// <summary>
        /// 캐릭터의 현재 턴 이동력 계산
        /// 속도 스탯 기반으로 이동력 결정
        /// </summary>
        public static float CalculateMovePoint(CharBase character)
        {
            // 예시 공식: 속도의 10%를 이동력으로
            // 실제 게임 밸런스에 맞게 조정 필요
            float speed = character.RuntimeStat.GetStat(Core.Scripts.Foundation.Define.SystemEnum.eStats.NSPEED);
            
            // 최소 이동력 보장 (속도가 0이어도 최소한 이동 가능)
            float baseMovePoint = Mathf.Max(speed * 0.1f, 1f);
            
            // 추후 버프/디버프에 따른 이동력 보정 추가 가능
            // float buffedMovePoint = ApplyMovePointModifiers(baseMovePoint, character);
            
            return baseMovePoint;
        }
        
        /// <summary>
        /// 행동 타입별 설명 텍스트 반환 (UI용)
        /// </summary>
        public static string GetActionCategoryDescription(TurnActionState.ActionCategory category)
        {
            switch (category)
            {
                case TurnActionState.ActionCategory.Move:
                    return "이동 (이동력 소모)";
                case TurnActionState.ActionCategory.SkillAction:
                    return "스킬 (턴당 1회 제한)";
                case TurnActionState.ActionCategory.ExtraAction:
                    return "부가 액션 (턴당 1회 제한)";
                default:
                    return "알 수 없는 행동";
            }
        }
        
        /// <summary>
        /// 턴 종료 가능 여부 확인 (선택적 기능)
        /// 모든 이동력을 소진하지 않았거나 주요 행동을 사용하지 않은 경우 경고
        /// </summary>
        public static bool ShouldWarnBeforeEndTurn(TurnActionState actionState, out string warningMessage)
        {
            warningMessage = "";
            
            // 이동력이 많이 남았고 주요 행동도 사용하지 않은 경우
            if (actionState.RemainingMovePoint > actionState.MaxMovePoint * 0.5f && 
                !actionState.SkillActionUsed)
            {
                warningMessage = "아직 이동력과 주요 행동이 남아있습니다. 턴을 종료하시겠습니까?";
                return true;
            }
            
            // 주요 행동만 사용하지 않은 경우
            if (!actionState.SkillActionUsed && actionState.RemainingMovePoint <= 0.5f)
            {
                warningMessage = "밀기/점프/스킬을 사용하지 않았습니다. 턴을 종료하시겠습니까?";
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 디버그용: 행동 상태를 컬러 포맷으로 로깅
        /// </summary>
        public static void LogActionState(string characterName, TurnActionState state)
        {
            string moveStatus = state.RemainingMovePoint > 0 
                ? $"<color=green>이동력: {state.RemainingMovePoint:F1}/{state.MaxMovePoint:F1}</color>"
                : $"<color=red>이동력: 소진</color>";
            
            string actionStatus = !state.SkillActionUsed
                ? "<color=green>주요행동: 사용가능</color>"
                : "<color=yellow>주요행동: 사용완료</color>";
            
            Debug.Log($"[{characterName}] {moveStatus} | {actionStatus}");
        }
    }
}