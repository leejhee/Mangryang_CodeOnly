using AngelBeat;
using GamePlay.Common.Scripts.Entities.Skills;
using GamePlay.Common.Scripts.Keyword;
using GamePlay.Common.Scripts.Skill;
using GamePlay.Features.Battle.Scripts.Unit;
using GamePlay.Features.Scripts.Keyword;

namespace Modules.BT
{
    public class BTContext
    {
        /// <summary> 행동 지정 용도로 사용하면 되는 프로퍼티(이동, 점프, 공격) </summary>
        public CharBase Agent { get; }

        public BTContext(CharBase agent)
        {
            Agent = agent;
        }
        
        #region Properties

        public float HP => Agent.CurrentHP;
        public float MaxHP => Agent.MaxHP;
        public float MovePoint => Agent.MovePoint;
        
        public SkillInfo        SkillInfo => Agent.SkillInfo;
        //public ExecutionInfo    ExecutionInfo => Agent.ExecutionInfo;
        public KeywordInfo      KeywordInfo => Agent.KeywordInfo;
        
        #endregion
    }
}