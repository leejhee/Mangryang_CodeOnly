using GamePlay.Common.Scripts.Contracts;
using GamePlay.Common.Scripts.Entities.Skills;
using GamePlay.Features.Battle.Scripts.BattleMap;
using GamePlay.Features.Battle.Scripts.BattleTurn;
using GamePlay.Features.Battle.Scripts.Unit;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.BattleAction
{
    public enum ActionType
    {
        None,
        Move,
        Jump,
        Push,
        Skill
    }
    
    [Serializable]
    public sealed class BattleActionContext
    {
        //=========== 전투 행동 타입 ============//
        public ActionType battleActionType;
        
        //=========== 행동 주체 ============//
        public CharBase actor;
        
        //=========== 전투 환경 ============//
        public StageField battleField;

        public Vector2Int? TargetCell;
        
        //=========== Action 중 별개 필요 요소 ============//
        public SkillModel skillModel;

        public List<IDamageable> targets;
        
        //=========== 외부 Cancel Token ============//
        public CancellationToken ExternalToken;
    }
    
    public readonly struct BattleActionPreviewData
    {
        public readonly List<Vector2Int> PossibleCells;
        public readonly List<Vector2Int> BlockedCells;
        public readonly List<Vector2Int> MaskedCells;
        public BattleActionPreviewData(
            List<Vector2Int> possibleCells, 
            List<Vector2Int> blockedCells,
            List<Vector2Int> maskedCells)
        {
            PossibleCells = possibleCells;
            BlockedCells = blockedCells;
            MaskedCells = maskedCells;
        }
    }

    public readonly struct BattleActionResult
    {
        public enum ResultReason
        {
            None,
            BattleActionAborted,
            InvalidTarget,
            InvalidContext,
            RestrictedByKeyword,
        }

        public BattleActionResult(bool success, ResultReason reason = ResultReason.None)
        {
            ActionSuccess = success;
            Reason = reason;
        }
        
        public readonly bool ActionSuccess;
        public readonly ResultReason Reason;

        public static BattleActionResult Success() => new(true);
        public static BattleActionResult Fail(ResultReason reason) => new(false, reason);

    }
    
    public static class ActionTypeExtensions
    {
        public static TurnActionState.ActionCategory GetActionCategory(this ActionType type)
        {
            switch (type)
            {
                case ActionType.Move:
                    return TurnActionState.ActionCategory.Move;
                    
                case ActionType.Jump:
                case ActionType.Push:
                    return TurnActionState.ActionCategory.ExtraAction;
                    
                case ActionType.Skill:
                    return TurnActionState.ActionCategory.SkillAction;
                    
                default:
                    return TurnActionState.ActionCategory.None; 
            }
        }
    }
    
}
