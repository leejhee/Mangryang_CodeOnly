using Cysharp.Threading.Tasks;
using GamePlay.Features.Battle.Scripts.Unit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamePlay.Features.Battle.Scripts.Tutorial
{
    public class BattleTutorialGuideUI : MonoBehaviour
    {
        public static BattleTutorialGuideUI Instance { get; private set; }
        
        [Header("공통 패널")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TextMeshProUGUI guideText;
        [SerializeField] private Image arrowImage; // 있으면 사용, 없으면 무시
        
        [Header("위치 설정")]
        [SerializeField] private float leftMargin = 40f;
        
        [Header("조작 버튼")]
        [SerializeField] private Button nextButton;
        
        private Camera _cam;
        private UniTaskCompletionSource<bool> _waitTcs;

        private void Awake()
        {
            Instance = this;
            if (panel)
                panel.gameObject.SetActive(false);

            if (nextButton)
                nextButton.onClick.AddListener(OnClickNext);
            else
                Debug.LogWarning("[BattleTutorialGuideUI] Next button is not assigned. Button-driven tutorial pages are disabled.", this);
        }

        private void OnDestroy()
        {
            if (nextButton)
                nextButton.onClick.RemoveListener(OnClickNext);
            if (Instance == this) Instance = null;
        }

        private void OnClickNext()
        {
            _waitTcs?.TrySetResult(true);
        }

        public void ShowFixedLeft(string text)
        {
            if (!panel) return;

            guideText.text = text;

            // 화면 왼쪽 중앙에 고정
            panel.anchorMin = panel.anchorMax = new Vector2(0f, 0.5f); // Left + Center
            panel.pivot = new Vector2(0f, 0.5f);
            panel.anchoredPosition = new Vector2(leftMargin, 0f);

            panel.gameObject.SetActive(true);
        }

        public async UniTask<bool> WaitForNextAsync()
        {
            _waitTcs = new UniTaskCompletionSource<bool>();
            return await _waitTcs.Task;
        }

        public void Hide()
        {
            if (panel)
                panel.gameObject.SetActive(false);
            _waitTcs?.TrySetResult(false);
            _waitTcs = null;
        }
    }
}
