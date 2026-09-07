using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace UIs.Runtime
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PopupView : MonoBehaviour, IView
    {
        [Header("Popup Fade Settings")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeDuration = 0.15f;   // 0이면 즉시 표시

        public GameObject Root => gameObject;

        private void Reset()
        {
            if (!_canvasGroup)
                _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            if (!_canvasGroup)
                _canvasGroup = GetComponent<CanvasGroup>();

            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            // Exit 후 최종 정리
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        public async UniTask PlayEnterAsync(CancellationToken ct)
        {
            // 대충 fade 해놓음
            if (_fadeDuration <= 0f || !_canvasGroup)
            {
                if (_canvasGroup)
                {
                    _canvasGroup.alpha = 1f;
                    _canvasGroup.interactable = true;
                    _canvasGroup.blocksRaycasts = true;
                }
                return;
            }

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = true;   // 클릭은 잡되, 아직 눌리지는 않게 할 수도 있음
            _canvasGroup.alpha = 0f;

            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                if (ct.IsCancellationRequested) return;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeDuration);
                _canvasGroup.alpha = t;

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
        }

        public async UniTask PlayExitAsync(CancellationToken ct)
        {
            if (_fadeDuration <= 0f || !_canvasGroup)
            {
                if (_canvasGroup)
                    _canvasGroup.alpha = 0f;
                return;
            }

            _canvasGroup.interactable = false;   // 더 이상 클릭 못 하게
            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                if (ct.IsCancellationRequested) return;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeDuration);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false; // 입력 뒤로 통과
        }
    }
}