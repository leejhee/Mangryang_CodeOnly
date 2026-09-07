using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace GamePlay.Common.Scripts.Transition
{
    /// <summary>
    /// 전역 트랜지션 관리용
    /// 페이드 말고도 더 들어갈 수 있음.
    /// </summary>
    public class FadeTransition : MonoBehaviour, ITransitionEffect
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float defaultDuration = 0.3f;

        void Reset()
        {
            canvasGroup = GetComponentInChildren<CanvasGroup>();
        }

        void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponentInChildren<CanvasGroup>();

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        public UniTask PlayOutAsync(float duration, CancellationToken ct = default)
        {
            float d = duration > 0f ? duration : defaultDuration;
            return FadeAsync(targetAlpha: 1f, d, ct);
        }

        public UniTask PlayInAsync(float duration, CancellationToken ct = default)
        {
            float d = duration > 0f ? duration : defaultDuration;
            return FadeAsync(targetAlpha: 0f, d, ct);
        }

        private async UniTask FadeAsync(float targetAlpha, float duration, CancellationToken ct)
        {
            canvasGroup.blocksRaycasts = true; // 트랜지션 중에는 입력 막기

            float start = canvasGroup.alpha;
            float time  = 0f;

            while (time < duration)
            {
                if (ct.IsCancellationRequested) return;

                time += Time.unscaledDeltaTime; // 타임스케일 영향 X
                float t = Mathf.Clamp01(time / duration);
                canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            canvasGroup.alpha = targetAlpha;

            // 다 걷힌 상태(=밝게 보이는 상태)라면 입력 다시 통과
            if (Mathf.Approximately(targetAlpha, 0f))
                canvasGroup.blocksRaycasts = false;
        }
    }
}