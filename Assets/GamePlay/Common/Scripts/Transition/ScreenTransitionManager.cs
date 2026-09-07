using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace GamePlay.Common.Scripts.Transition
{
    public enum ScreenTransitionType
    {
        Fade,
    }
    
    public class ScreenTransitionManager : MonoBehaviour
    {
        public static ScreenTransitionManager Instance { get; private set; }

        [SerializeField] private FadeTransition fadeTransition;
        //TODO : 필요한 트랜지션 behavior 추가
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (fadeTransition == null)
                fadeTransition = GetComponentInChildren<FadeTransition>();
        }

        private ITransitionEffect GetEffect(ScreenTransitionType type)
        {
            return type switch
            {
                ScreenTransitionType.Fade => fadeTransition,
                _                         => fadeTransition,
            };
        }

        public UniTask PlayOutAsync(
            ScreenTransitionType type = ScreenTransitionType.Fade,
            float duration = -1f,
            CancellationToken ct = default)
        {
            return GetEffect(type).PlayOutAsync(duration, ct);
        }

        public UniTask PlayInAsync(
            ScreenTransitionType type = ScreenTransitionType.Fade,
            float duration = -1f,
            CancellationToken ct = default)
        {
            return GetEffect(type).PlayInAsync(duration, ct);
        }
        
        #region Fade Util Part
        public async UniTask RunWithFadeAsync(
            Func<UniTask> action,
            float outDuration = 0.25f,
            float inDuration  = 0.25f,
            ScreenTransitionType type = ScreenTransitionType.Fade,
            CancellationToken ct = default)
        {
            await PlayOutAsync(type, outDuration, ct);
            try
            {
                if (action != null)
                    await action();
            }
            finally
            {
                await PlayInAsync(type, inDuration, ct);
            }
        }
        
        public async UniTask FlashAsync(
            float outDuration = 0.15f,
            float inDuration  = 0.15f,
            ScreenTransitionType type = ScreenTransitionType.Fade,
            CancellationToken ct = default)
        {
            await PlayOutAsync(type, outDuration, ct);
            await PlayInAsync(type, inDuration, ct);
        }
        
        public async UniTask RunEnterFadeAsync(
            Func<UniTask> action,
            float outDuration = 0.2f,
            float inDuration  = 0.2f,
            ScreenTransitionType type = ScreenTransitionType.Fade,
            CancellationToken ct = default)
        {
            await PlayOutAsync(type, outDuration, ct);

            UniTask actionTask = action?.Invoke() ?? UniTask.CompletedTask;

            await UniTask.Delay(1000);
            
            PlayInAsync(type, inDuration, ct).Forget();

            await actionTask;
        }
        
        public async UniTask RunEnterFade(
            Action action,
            float outDuration = 0.2f,
            float inDuration  = 0.2f,
            ScreenTransitionType type = ScreenTransitionType.Fade,
            CancellationToken ct = default)
        {
            await PlayOutAsync(type, outDuration, ct);
            
            PlayInAsync(type, inDuration, ct).Forget();
            
            action.Invoke();
        }
        #endregion
    }
}