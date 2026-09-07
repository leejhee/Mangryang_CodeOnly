using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Entities.Skills;
using GamePlay.Features.Battle.Scripts.Unit;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Playables;

namespace GamePlay.Common.Scripts.Skill
{
    [RequireComponent(typeof(SkillMarkerReceiver))]
    public class SkillBase : MonoBehaviour
    {
        private SkillModel _model;
        private PlayableDirector _director;
        private bool _isPlaying;
        private IDisposable _animatorLease;
        
        public CharBase CharPlayer { get; private set; }
        public SkillParameter SkillParameter;
        public SkillModel SkillModel => _model;
        
        private void Awake()
        {
            _director = GetComponent<PlayableDirector>();
            if (_director == null)
            {
                Debug.LogError($"{transform.name} PlayableDirector is Null");
            }
            else if (_director.playableAsset == null)
            {
                Debug.LogError($"[SkillBase] {transform.name} has no Timeline asset assigned.");
            }
        }

        private bool CanPlayTimeline()
        {
            if (_director && _director.playableAsset) return true;

            Debug.LogError($"[SkillBase] Cannot play skill '{name}': PlayableDirector or Timeline asset is missing.");
            return false;
        }

        private bool BindAnimatorOutputs()
        {
            foreach (PlayableBinding output in _director.playableAsset.outputs)
            {
                System.Type targetType = output.outputTargetType;
                if (targetType == null || !typeof(Animator).IsAssignableFrom(targetType))
                    continue;

                Animator animator = CharPlayer && CharPlayer.Anim
                    ? CharPlayer.Anim.Animator
                    : null;

                if (!animator)
                {
                    Debug.LogError($"[SkillBase] Cannot bind Animation Track for '{name}': caster Animator is missing.");
                    return false;
                }

                if (!output.sourceObject)
                {
                    Debug.LogError($"[SkillBase] Cannot bind Animation Track for '{name}': track source is missing.");
                    return false;
                }

                _director.SetGenericBinding(output.sourceObject, animator);
            }

            return true;
        }

        private void AcquireAnimatorLease()
        {
            ReleaseAnimatorLease();
            if (CharPlayer && CharPlayer.Anim)
                _animatorLease = CharPlayer.Anim.AcquireTimelineLease();
        }

        private void ReleaseAnimatorLease()
        {
            IDisposable lease = _animatorLease;
            _animatorLease = null;
            lease?.Dispose();
        }

        private void OnDisable()
        {
            ReleaseAnimatorLease();
        }

        private async UniTask FinishNonAwaitedPlayAsync(SkillMarkerReceiver receiver)
        {
            try
            {
                if (receiver)
                    await receiver.Completion;
            }
            finally
            {
                SkillParameter = null;
                _isPlaying = false;
            }
        }

        public void SetCharBase(CharBase charBase)
        {
            CharPlayer = charBase;
        }

        public void Init(SkillModel skillModel)
        {
            _model = skillModel;
        }
        
        public void SkillPlay(SkillParameter param)
        {
            // 타임라인 재생
            if (!CanPlayTimeline()) return;
            if (!BindAnimatorOutputs()) return;
            if (_isPlaying)
            {
                Debug.LogWarning($"[SkillBase] : {name} Called While Already Playing");
                return;
            }

            _isPlaying = true;
            SkillParameter = param;
            SkillMarkerReceiver receiver = GetComponent<SkillMarkerReceiver>();
            if (receiver) receiver.Begin(CancellationToken.None);
            AcquireAnimatorLease();
            
            void OnStopped(PlayableDirector d)
            {
                if (d != _director) return;
                _director.stopped -= OnStopped;
                ReleaseAnimatorLease();
                receiver?.End();
                FinishNonAwaitedPlayAsync(receiver).Forget();
            }

            _director.stopped += OnStopped;
            try
            {
                _director.time = 0;
                _director.Play();
            }
            catch
            {
                _director.stopped -= OnStopped;
                ReleaseAnimatorLease();
                receiver?.End();
                SkillParameter = null;
                _isPlaying = false;
                throw;
            }
        }

        public async UniTask<bool> PlaySkillAsync(SkillParameter param, CancellationToken ct)
        {
            if (!CanPlayTimeline()) return false;
            if (!BindAnimatorOutputs()) return false;
            if (_isPlaying)
            {
                Debug.LogWarning($"[SkillBase] : {name} Called While Already Playing");
                return false;
            }
            _isPlaying = true;
            SkillParameter = param;
            SkillMarkerReceiver receiver = GetComponent<SkillMarkerReceiver>();
            if (receiver) receiver.Begin(ct);
            AcquireAnimatorLease();
            
            var tcs = new UniTaskCompletionSource();

            void OnStopped(PlayableDirector d)
            {
                if (d != _director) return;
                _director.stopped -= OnStopped;
                ReleaseAnimatorLease();
                receiver?.End();
                tcs.TrySetResult();
            }
            
            _director.stopped += OnStopped;
            try
            {
                _director.time = 0;
                _director.Play();

                using (ct.Register(() =>
                       {
                           try { _director.stopped -= OnStopped; }
                           catch {/* Silence */ }
                           _director.Stop();
                           ReleaseAnimatorLease();
                           receiver?.End();
                           tcs.TrySetCanceled(ct);
                       }))
                {
                    if (receiver)
                        await UniTask.WhenAll(tcs.Task, receiver.Completion);
                    else
                        await tcs.Task;
                }
            }
            finally
            {
                ReleaseAnimatorLease();
                SkillParameter = null;
                _isPlaying = false;
            }

            return true;
        }
        
    }
}
