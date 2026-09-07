using GamePlay.Common.Scripts.Skill;
using GamePlay.Common.Scripts.Timeline.PlayableBehaviour;
using GamePlay.Features.Battle.Scripts.Unit;
using UnityEngine;
using UnityEngine.Playables;

namespace GamePlay.Common.Scripts.Timeline.PlayableAsset
{
    public class AnimationPlayableAsset: SkillTimeLinePlayableAsset
    {
        [SerializeField]
        private AnimationClip animationClip;

        public AnimationClip AnimationClip => animationClip;
        public override double duration => 
            animationClip != null ? animationClip.length : base.duration;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            base.CreatePlayable(graph, owner);

            var playableBehaviour = new AnimationPlayableBehaviour();
            
            SkillBase skill = owner.GetComponent<SkillBase>();
            CharBase player = skill.CharPlayer;
            
            playableBehaviour.InitBehaviour(player, skill);
            playableBehaviour.animDriver = player.Anim;
            playableBehaviour.animationClip = animationClip;
            var scriptPlayable = ScriptPlayable<AnimationPlayableBehaviour>.Create(graph, playableBehaviour);

            // var animationPlayable = AnimationClipPlayable.Create(graph, animationClip);
            //scriptPlayable.AddInput(animationPlayable, 0, 1);

            return scriptPlayable;
        }
    }
}