using GamePlay.Common.Scripts.Contracts;
using GamePlay.Features.Battle.Scripts.BattleMap;
using GamePlay.Features.Battle.Scripts.Unit;
using System.Collections.Generic;
using System.Linq;

namespace GamePlay.Common.Scripts.Skill
{
    public class SkillParameter
    {
        public readonly CharBase                Caster;
        public readonly List<IDamageable>          Target;
        //public readonly SkillModel              Model;
        //public readonly SystemEnum.eSkillType   SkillType;
        public readonly BattleStageGrid         Grid;
        public readonly ISkillTimelineEventSink TimelineEventSink;
        
        public SkillParameter(
            CharBase caster, 
            List<IDamageable> target,
            //SkillModel model,
            BattleStageGrid grid,
            ISkillTimelineEventSink timelineEventSink
        )
        {
            Caster = caster;
            Target = target;
            //Model = model;
            Grid = grid;
            TimelineEventSink = timelineEventSink;
            //SkillType = model.skillType;
        }
        
        public List<CharBase> TargetCharacters =>
            Target.OfType<CharBase>().ToList();
    }
}
