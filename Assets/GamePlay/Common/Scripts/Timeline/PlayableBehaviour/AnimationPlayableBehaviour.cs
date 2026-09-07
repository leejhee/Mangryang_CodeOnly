using GamePlay.Features.Battle.Scripts.Unit.Components;
using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GamePlay.Common.Scripts.Timeline.PlayableBehaviour
{
    public class AnimationPlayableBehaviour: SkillTimeLinePlayableBehaviour
    {
        public CharAnimDriver animDriver { get; set; }
        public AnimationClip animationClip { get; set; }

        private PlayableGraph _playableGraph;
        private IDisposable _lease;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            _lease?.Dispose();
            _lease = animDriver?.AcquireTimelineLease();
            _playableGraph = PlayableGraph.Create("AnimationPlayable");

            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(_playableGraph, animationClip);

            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_playableGraph, "Animation", animDriver.Animator);
            output.SetSourcePlayable(clipPlayable);

            _playableGraph.Play();
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (_playableGraph.IsValid())
            {
                _playableGraph.Stop();
                _playableGraph.Destroy(); 
            }
            
            _lease?.Dispose();
            _lease = null;
        }
    }
}
