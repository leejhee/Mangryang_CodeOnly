using Core.Scripts.Foundation.Define;
using GamePlay.Features.Battle.Scripts.Unit;
using System.Collections.Generic;

namespace GamePlay.Features.Battle.Scripts.BattleTurn
{
    #region Battle Domain DTO
    
    public readonly struct RoundEventContext
    {
        public int Round { get; }

        public RoundEventContext(int round)
        {
            Round = round;
        }
    }
    
    public sealed class TurnEventContext
    {
        /// <summary> 현재 라운드 번호 </summary>
        public int Round { get; }

        /// <summary> 이번 턴의 주인 </summary>
        public CharBase Actor { get; }

        /// <summary> 이 Actor가 전투 시작 이후 "몇 번째로 행동하는 턴"인지 </summary>
        public int ActorTurnCount { get; }

        public TurnEventContext(int round, CharBase actor, int actorTurnCount)
        {
            Round = round;
            Actor = actor;
            ActorTurnCount = actorTurnCount;
        }
    }
    
    #endregion
    
    #region Battle UI DTO
    
    public readonly struct TurnChangedDTO
    {
        public int Round { get; }
        public long ActorId { get; }
        public SystemEnum.eCharType Side { get; }
        
        public TurnChangedDTO(int round, CharBase actor)
        {
            Round          = round;
            ActorId        = actor.GetID();
            Side           = actor.GetCharType();
        }

        public TurnChangedDTO(TurnEventContext ctx)
            : this(ctx.Round, ctx.Actor) {}
    }
    
    public readonly struct TurnSlotDTO
    {
        public long ActorId { get; }
        public SystemEnum.eCharType Side { get; }
        public bool IsDead { get; }

        public TurnSlotDTO(Turn t)
        {
            ActorId = t.TurnOwner.GetID();
            Side    = t.TurnOwner.GetCharType();
            IsDead  = !t.IsValid;
        }
    }

    public readonly struct TurnOrderDTO
    {
        public IReadOnlyList<TurnSlotDTO> Slots { get; }

        public TurnOrderDTO(IReadOnlyCollection<Turn> turns)
        {
            List<TurnSlotDTO> list = new(turns.Count);
            foreach (Turn t in turns)
                list.Add(new TurnSlotDTO(t));

            Slots = list;
        }
    }
    
    public readonly struct TurnActionDTO
    {
        public long ActorId { get; }
        
        public float MaxMovePoint         { get; }
        public float RemainingMovePoint   { get; }
    
        public bool CanStartMove         { get; }
        public bool CanUseSkill          { get; }
        public bool CanUseExtra          { get; }

        public TurnActionDTO(
            long actorId,
            float maxMovePoint,
            float remainingMovePoint,
            bool canStartMove,
            bool canUseSkill,
            bool canUseExtra)
        {
            ActorId            = actorId;
            MaxMovePoint       = maxMovePoint;
            RemainingMovePoint = remainingMovePoint;
            CanStartMove       = canStartMove;
            CanUseSkill        = canUseSkill;
            CanUseExtra        = canUseExtra;
        }

        public TurnActionDTO(long actorId, TurnActionState state)
        {
            ActorId = actorId;
            MaxMovePoint       = state.MaxMovePoint;
            RemainingMovePoint = state.RemainingMovePoint;
            CanStartMove = state.RemainingMovePoint > 0;
            CanUseExtra = !state.ExtraActionUsed;
            CanUseSkill = !state.SkillActionUsed;
        }
        
        public static TurnActionDTO FromState(long actorId, TurnActionState s)
        {
            return new TurnActionDTO(
                actorId:            actorId,
                maxMovePoint:       s.MaxMovePoint,
                remainingMovePoint: s.RemainingMovePoint,
                canStartMove:       s.RemainingMovePoint > 0f,
                canUseSkill:        !s.SkillActionUsed,
                canUseExtra:        !s.ExtraActionUsed
            );
        }
    }
    
    #endregion
    
}