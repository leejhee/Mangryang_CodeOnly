using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Entities.Skills;
using GamePlay.Common.Scripts.Skill;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace GamePlay.Common.Scripts.Timeline.Marker
{
    public abstract class SkillTimeLineMarker : UnityEngine.Timeline.Marker, INotification
    {
        public PropertyName id => new PropertyName("SkillTimeLineMarker");

        private Action<UniTask> _trackHook;
        protected SkillParameter InputParam => SkillBase?.SkillParameter;
        protected SkillModel Model => SkillBase?.SkillModel;
        protected SkillBase SkillBase { get; private set; }
        
        public void AttachTracker(Action<UniTask> trackHook) => _trackHook = trackHook;
        protected void Track(UniTask task) => _trackHook?.Invoke(task);
        
        public virtual void InitContext(SkillBase sb) => SkillBase = sb;

        public virtual void MarkerAction() { }

        public virtual UniTask BuildTaskAsync(CancellationToken ct)
        {
            try { MarkerAction(); }
            catch (System.Exception e) { Debug.LogException(e); }
            return UniTask.CompletedTask;
        }

        protected UniTask PublishCueAsync(SkillTimelineCue cue, CancellationToken ct)
        {
            if (InputParam?.TimelineEventSink == null)
            {
                Debug.LogError($"[{GetType().Name}] Timeline event sink is missing.");
                return UniTask.CompletedTask;
            }

            if (Model == null)
            {
                Debug.LogError($"[{GetType().Name}] SkillModel is missing.");
                return UniTask.CompletedTask;
            }

            return InputParam.TimelineEventSink.PublishAsync(cue, Model, ct);
        }
        
        protected virtual void SkillInitialize() { }

        public override void OnInitialize(TrackAsset aPent)
        {
            base.OnInitialize(aPent);
            SkillInitialize();
        }
        
    }
    
    

}
