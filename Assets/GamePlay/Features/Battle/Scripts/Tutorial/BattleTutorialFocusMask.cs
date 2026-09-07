using UnityEngine;
using UnityEngine.UI;

namespace GamePlay.Features.Battle.Scripts.Tutorial
{
    public class BattleTutorialFocusMask : MonoBehaviour
    {
        public static BattleTutorialFocusMask Instance { get; private set; }

        [Header("필수 참조")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform maskRoot;  // TutorialFocusMaskRoot
        [SerializeField] private Image topImage;
        [SerializeField] private Image bottomImage;
        [SerializeField] private Image leftImage;
        [SerializeField] private Image rightImage;

        [Header("월드 좌표 -> 스크린 변환용 카메라")]
        [SerializeField] private Camera worldCamera;

        private RectTransform _canvasRect;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _canvasRect = maskRoot ?? rootCanvas.transform as RectTransform;
            
            if (worldCamera == null)
                worldCamera = Camera.main;

            if (maskRoot != null)
                maskRoot.gameObject.SetActive(false);
        }

        /// <summary>
        /// Screen 좌표계 픽셀 Rect를 기준으로 구멍을 뚫는다.
        /// padding을 주면 그만큼 사각형이 커진다.
        /// </summary>
        public void ShowHoleForScreenRect(Rect screenRect, float padding = 0f)
        {
            if (rootCanvas == null || maskRoot == null || _canvasRect == null)
                return;

            if (padding > 0f)
            {
                screenRect.xMin -= padding;
                screenRect.xMax += padding;
                screenRect.yMin -= padding;
                screenRect.yMax += padding;
            }

            Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;

            Vector2 localMin;
            Vector2 localMax;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                new Vector2(screenRect.xMin, screenRect.yMin),
                uiCam,
                out localMin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                new Vector2(screenRect.xMax, screenRect.yMax),
                uiCam,
                out localMax);

            float fxMin = localMin.x;
            float fxMax = localMax.x;
            float fyMin = localMin.y;
            float fyMax = localMax.y;

            Rect cRect = _canvasRect.rect;

            float fullWidth = cRect.width;
            float fullHeight = cRect.height;
            float canvasCenterX = (cRect.xMin + cRect.xMax) * 0.5f;

            // --- Bottom: 캔버스 바닥 ~ 구멍 아래 ---
            float bottomHeight = fyMin - cRect.yMin;
            SetupPanel(
                bottomImage.rectTransform,
                center: new Vector2(canvasCenterX, cRect.yMin + bottomHeight * 0.5f),
                size: new Vector2(fullWidth, bottomHeight));

            // --- Top: 구멍 위 ~ 캔버스 천장 ---
            float topHeight = cRect.yMax - fyMax;
            SetupPanel(
                topImage.rectTransform,
                center: new Vector2(canvasCenterX, fyMax + topHeight * 0.5f),
                size: new Vector2(fullWidth, topHeight));

            // --- Left / Right: 구멍의 좌우 ---
            float middleHeight = fyMax - fyMin;

            float leftWidth = fxMin - cRect.xMin;
            SetupPanel(
                leftImage.rectTransform,
                center: new Vector2(cRect.xMin + leftWidth * 0.5f, fyMin + middleHeight * 0.5f),
                size: new Vector2(leftWidth, middleHeight));

            float rightWidth = cRect.xMax - fxMax;
            SetupPanel(
                rightImage.rectTransform,
                center: new Vector2(fxMax + rightWidth * 0.5f, fyMin + middleHeight * 0.5f),
                size: new Vector2(rightWidth, middleHeight));

            maskRoot.gameObject.SetActive(true);
        }

        private void SetupPanel(RectTransform rt, Vector2 center, Vector2 size)
        {
            if (rt == null) return;

            if (size.x <= 0f || size.y <= 0f)
            {
                rt.gameObject.SetActive(false);
                return;
            }

            rt.gameObject.SetActive(true);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = center;
            rt.sizeDelta = size;
        }

        /// <summary>
        /// 마스크 전체 숨기기.
        /// </summary>
        public void Hide()
        {
            if (maskRoot != null)
                maskRoot.gameObject.SetActive(false);
        }

        /// <summary>
        /// UI RectTransform 기준 구멍 뚫기
        /// </summary>
        public void ShowHoleForRectTransform(RectTransform target, float padding = 0f)
        {
            if (target == null) return;

            Vector3[] worldCorners = new Vector3[4];
            target.GetWorldCorners(worldCorners);

            Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;

            Vector2 min = RectTransformUtility.WorldToScreenPoint(uiCam, worldCorners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(uiCam, worldCorners[2]);

            Rect r = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            ShowHoleForScreenRect(r, padding);
        }

        /// <summary>
        /// SpriteRenderer가 가진 월드상의 Bounds 기준으로 구멍 뚫기
        /// </summary>
        public void ShowHoleForSprite(SpriteRenderer sr, float padding = 0f)
        {
            if (sr == null || worldCamera == null)
                return;

            Bounds b = sr.bounds;

            Vector3 min = worldCamera.WorldToScreenPoint(
                new Vector3(b.min.x, b.min.y, b.center.z));
            Vector3 max = worldCamera.WorldToScreenPoint(
                new Vector3(b.max.x, b.max.y, b.center.z));

            Rect r = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            ShowHoleForScreenRect(r, padding);
        }

        /// <summary>
        /// 월드 좌표 하나를 기준으로 임의의 크기를 가진 사각형 구멍
        /// </summary>
        public void ShowHoleAroundWorldPosition(Vector3 worldPos, Vector2 worldSize, float padding = 0f)
        {
            if (!worldCamera) return;

            //Vector3 screen = worldCamera.WorldToScreenPoint(worldPos);
            //Rect r = new Rect(
            //    screen.x - size.x * 0.5f,
            //    screen.y - size.y * 0.5f,
            //    size.x,
            //    size.y);
            Vector3 worldMin = worldPos - new Vector3(worldSize.x * 0.5f, worldSize.y * 0.5f, 0f);
            Vector3 worldMax = worldPos + new Vector3(worldSize.x * 0.5f, worldSize.y * 0.5f, 0f);

            Vector3 screenMin = worldCamera.WorldToScreenPoint(worldMin);
            Vector3 screenMax = worldCamera.WorldToScreenPoint(worldMax);

            Rect r = Rect.MinMaxRect(screenMin.x, screenMin.y, screenMax.x, screenMax.y);
            ShowHoleForScreenRect(r, padding);
        }
    }
}