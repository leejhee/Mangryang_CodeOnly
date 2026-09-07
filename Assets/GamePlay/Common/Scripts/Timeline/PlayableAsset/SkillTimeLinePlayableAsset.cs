using GamePlay.Common.Scripts.Skill;
using GamePlay.Common.Scripts.Timeline.PlayableBehaviour;
using GamePlay.Features.Battle.Scripts.Unit;
using UnityEngine;
using UnityEngine.Playables;

namespace GamePlay.Common.Scripts.Timeline.PlayableAsset
{
    public abstract class SkillTimeLinePlayableAsset : UnityEngine.Playables.PlayableAsset
    {
        protected bool TryResolveContext(GameObject owner, out CharBase caster, out SkillBase skill)
        {
            caster = null;
            skill = owner ? owner.GetComponent<SkillBase>() : null;
            if (skill == null && owner)
                skill = owner.GetComponentInParent<SkillBase>();

            if (skill != null)
                caster = skill.CharPlayer;

            return caster != null && skill != null;
        }

        /// <summary>
        /// 공통 생성기: Behaviour를 만들고 컨텍스트를 주입한다.
        /// 자식 에셋은 이걸 호출한 뒤 Behaviour 필드만 추가 설정하면 된다.
        /// </summary>
        protected ScriptPlayable<T> CreatePlayableWithContext<T>(PlayableGraph graph, GameObject owner)
            where T : SkillTimeLinePlayableBehaviour, new()
        {
            var playable = ScriptPlayable<T>.Create(graph);
            var bhv = playable.GetBehaviour();
            if (TryResolveContext(owner, out var c, out var s))
                bhv.InitBehaviour(c, s);
            else
                bhv.MarkNoContext(); // 컨텍스트 없으면 no-op로 동작
            return playable;
        }

        ///<summary>
        /// 베이스는 직접 플레이어블을 만들지 않는다. 자식이 생성기를 사용하도록 한다.
        /// </summary>
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            // 자식이 override 하므로 여기선 빈 Playable을 반환(문제 방지 차원에서 Null 반환).
            return Playable.Null;
        }
    }
}
