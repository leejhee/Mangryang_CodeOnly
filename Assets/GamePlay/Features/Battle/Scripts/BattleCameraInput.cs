using Core.Scripts.Managers;
using GamePlay.Features.Battle.Scripts.BattleMap;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GamePlay.Features.Battle.Scripts
{
    public class BattleCameraInput : MonoBehaviour
    {
        [Header("Refs")]
        public BattleCameraDriver driver;
        public StageField stage;

        [Header("Tuning")]
        public float dragSensitivity = 1.0f;     // 드래그 속도
        public float wheelStep      = 1.0f;      // 줌 1칸당 OrthoSize 변화량
        public bool  enableDuringTurn = true;
        
        [Header("Wheel Tuning")]
        [SerializeField] private bool  normalizeWheel   = true;  // OS/장치 차이를 줄이기
        [SerializeField] private float wheelDetent      = 120f;  // Windows 기본 한 칸(=120)
        [SerializeField] private bool  multiplicative   = true;  // 비율(권장) vs 선형
        [SerializeField] private float zoomMulPerStep   = 0.9f;  // 한 칸마다 0.9배 (줌 인). 0.95~0.98도 무난
        [SerializeField] private float zoomLinearStep   = 0.5f;  // 선형일 때 한 칸당 사이즈 변화량
        
        private Camera _cam;
        private bool _panning;

        private void OnEnable()
        {
            _cam ??= Camera.main;
            _panning = false;
        }

        private void OnDisable()
        {
            _panning = false;
        }

        private void Update()
        {
            if (!enableDuringTurn || !driver || !stage) return;
            if (BattleInputGate.Instance != null && BattleInputGate.Instance.InputLocked)
                return;

            // 전투 액션(프리뷰/실행) 중일 때도 카메라 막기
            if (BattleController.Instance != null && BattleController.Instance.IsModal)
                return;

            var input = InputManager.Instance;
            if (input == null)
                return;

            _cam ??= Camera.main;

            // ===== Pan 버튼 상태 업데이트 =====
            if (input.GetCameraPanButtonDown())
            {
                if (!driver.IsLockedToTarget)
                {
                    _panning = true;
                }
                else
                {
                    // 타겟에 잠겨있다면 첫 드래그 때 Free 모드 진입
                    driver.EnterFree(stage);
                    _panning = true;
                }
            }
            else if (input.GetCameraPanButtonUp())
            {
                _panning = false;
            }

            // ===== Zoom =====
            float rawZoom = input.GetCameraZoomDelta();
            if (Mathf.Abs(rawZoom) > 0.01f && driver.IsOrtho)
            {
                float steps = normalizeWheel ? (rawZoom / Mathf.Max(1f, wheelDetent)) : rawZoom;

                float cur = driver.CurrentOrthoSize;
                float next;

                if (multiplicative)
                {
                    next = cur * Mathf.Pow(zoomMulPerStep, steps);
                }
                else
                {
                    next = cur - steps * zoomLinearStep;
                }

                driver.SetZoom(next);
            }

            // ===== Pan =====
            if (_panning)
            {
                Vector2 pxDelta = input.GetCameraPanDelta(); // 픽셀 단위
                if (pxDelta.sqrMagnitude > 0.0001f && driver.IsOrtho)
                {
                    float worldPerPixel = WorldUnitsPerPixel();
                    Vector2 worldDelta  = -pxDelta * worldPerPixel * dragSensitivity;
                    driver.PanFree(worldDelta);
                }
            }
        }

        public void Bind(StageField s, BattleCameraDriver d = null)
        {
            stage = s;
            if (!driver)
            {
#if UNITY_2023_1_OR_NEWER
                driver = d ? d : Object.FindFirstObjectByType<BattleCameraDriver>(FindObjectsInactive.Exclude);
#else
                driver = d ? d : FindObjectOfType<BattleCameraDriver>();
#endif
            }
        }

        float WorldUnitsPerPixel()
        {
            if (!driver.IsOrtho) return 0.01f;
            _cam ??= Camera.main;
            float h = _cam ? _cam.pixelHeight : Screen.height;
            // orthoSize는 화면 세로의 절반에 해당
            return (driver.CurrentOrthoSize * 2f) / Mathf.Max(1f, h);
        }
    }
}