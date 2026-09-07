using GamePlay.Common.Scripts.Skill;
using GamePlay.Features.Battle.Scripts.Unit;

namespace GamePlay.Common.Scripts.Timeline.PlayableBehaviour
{
    public abstract class SkillTimeLinePlayableBehaviour : UnityEngine.Playables.PlayableBehaviour
    {
        protected CharBase Char { get; private set; }
        protected SkillBase Skill { get; private set; }
        public bool HasContext { get; private set; }

        public virtual void InitBehaviour(CharBase character, SkillBase skill)
        {
            Char = character;
            Skill = skill;
            HasContext = (Char != null && Skill != null);
        }

        public void MarkNoContext()
        {
            Char = null;
            Skill = null;
            HasContext = false;
        }
    }
}