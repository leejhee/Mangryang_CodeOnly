using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GamePlay.Features.Battle.Scripts
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraPanClamp : MonoBehaviour
    {
        public enum BoundsSource { RendererBounds, ColliderBounds, ManualRect }

        [Header("Bounds")]
        public BoundsSource boundsSource = BoundsSource.RendererBounds;
        public Renderer boundsRenderer;          // Renderer.bounds 사용
        public Collider2D boundsCollider2D;      // Collider2D.bounds 사용
        public Vector2 manualMin = new(-50, -50);
        public Vector2 manualMax = new(50, 50);
        public Vector2 padding = new(0.5f, 0.5f); // 여백(월드 유닛)

        [Header("Move / Drag")]
        [Tooltip("키보드 팬 속도(유닛/초). 줌 배율에 따라 자동 스케일됨")]
        public float moveSpeed = 12f;
        [Tooltip("마우스 드래그 시 픽셀당 이동량 계수(월드/픽셀)")]
        public float dragWorldPerPixel = 1.0f;   // 1.0이면 '현재 줌에서 화면 픽셀-월드 환산'만큼 그대로 사용
        [Tooltip("가속 시간(0이면 즉시). 키보드 팬에만 적용")]
        public float accelTime = 0.08f;
        [Tooltip("감속 시간(0이면 즉시). 키보드 팬에만 적용")]
        public float decelTime = 0.10f;
        [Tooltip("UI 위에 포인터가 있을 땐 드래그/휠 무시")]
        public bool ignoreWhenPointerOverUI = true;

        [Header("Zoom (Orthographic)")]
        [Tooltip("휠 한 번(120 단위)당 줄어드는 orthographicSize")]
        public float wheelZoomStep = 1.0f;
        [Tooltip("키(Zoom Keys) 1.0 입력당 초당 변화량")]
        public float keyZoomPerSec = 4.0f;
        public float minOrtho = 3f;
        public float maxOrtho = 20f;
        [Tooltip("줌 부드럽게")]
        public float zoomSmoothTime = 0.06f;

        // ==== Input (새 Input System) ====
        [Header("Input Actions (optional). 비워두면 기본 바인딩 자동 생성")]
        public InputActionReference moveActionRef;         // Vector2: ↑↓←→, WASD 등
        public InputActionReference dragButtonActionRef;   // Button: MMB/RMB 등
        public InputActionReference pointerDeltaActionRef; // Vector2: <Pointer>/delta
        public InputActionReference scrollActionRef;       // Vector2: <Mouse>/scroll
        public InputActionReference zoomKeysActionRef;     // Axis: Q/E 등

        Camera _cam;

        InputAction _move;          // Vector2
        InputAction _dragButton;    // Button
        InputAction _pointerDelta;  // Vector2
        InputAction _scroll;        // Vector2(y 사용)
        InputAction _zoomKeys;      // float (-1~+1)

        bool _ownActions;           // 우리가 생성한 액션인지
        Vector2 _vel;               // 키보드 팬 속도(유닛/초)
        float _sx, _sy;             // SmoothDamp용
        float _zoomVel;             // 줌 SmoothDamp용

        void Awake()
        {
            _cam = GetComponent<Camera>();
            if (!_cam.orthographic)
                Debug.LogWarning("[CameraPanClamp] Orthographic 카메라 기준입니다. 퍼스펙티브면 동작이 다를 수 있어요.");
        }

        void OnEnable()
        {
            SetupActions();
            EnableActions(true);
        }

        void OnDisable()
        {
            EnableActions(false);
            DisposeOwnActions();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime; // 메뉴에서도 자연스럽게

            // 1) 입력 읽기
            Vector2 moveAxis = ReadMove();                // 키보드/패드 팬
            bool dragging    = ReadDragHeld();            // 드래그 중?
            Vector2 deltaPix = dragging ? ReadPointerDelta() : Vector2.zero; // 픽셀 단위
            float zoomDelta  = ReadZoomDelta(dt);         // +면 줌인(orthoSize 감소)

            // 2) 키보드 팬 속도 업데이트(부드럽게)
            Vector2 targetVel = moveAxis * ScaledMoveSpeed();
            _vel.x = Smooth(_vel.x, targetVel.x, ref _sx, targetVel.x != 0 ? accelTime : decelTime, dt);
            _vel.y = Smooth(_vel.y, targetVel.y, ref _sy, targetVel.y != 0 ? accelTime : decelTime, dt);

            // 3) 드래그 → 픽셀을 월드로 변환 (현재 줌에서 픽셀:월드 환산)
            Vector3 pos = transform.position;
            if (dragging && deltaPix.sqrMagnitude > 0f && AllowPointer())
            {
                float worldPerPixel = (_cam.orthographicSize * 2f) / Screen.height; // 1px이 월드에서 얼마?
                Vector2 worldDelta = -deltaPix * (worldPerPixel * dragWorldPerPixel); // 드래그 방향대로 카메라 역이동
                pos += (Vector3)worldDelta;
            }

            // 4) 키보드 팬 적용
            pos += (Vector3)(_vel * dt);

            // 5) 줌(orthoSize)
            if (Mathf.Abs(zoomDelta) > Mathf.Epsilon && AllowPointer())
            {
                float target = Mathf.Clamp(_cam.orthographicSize - zoomDelta, minOrtho, maxOrtho);
                _cam.orthographicSize = Smooth(_cam.orthographicSize, target, ref _zoomVel, zoomSmoothTime, dt);
            }

            // 6) 경계 클램프
            pos = ClampPosToBounds(pos);

            transform.position = pos;
        }

        // === Read Inputs ===
        Vector2 ReadMove() => _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;

        bool ReadDragHeld() => _dragButton != null && _dragButton.IsPressed();

        Vector2 ReadPointerDelta() => _pointerDelta != null ? _pointerDelta.ReadValue<Vector2>() : Vector2.zero;

        float ReadZoomDelta(float dt)
        {
            float delta = 0f;

            // 휠 (보통 한 틱에 y=±120)
            if (_scroll != null)
            {
                float wheelY = _scroll.ReadValue<Vector2>().y;
                if (Mathf.Abs(wheelY) > 0.01f)
                    delta += wheelZoomStep * Mathf.Sign(wheelY) * Mathf.Min(Mathf.Abs(wheelY) / 120f, 5f);
            }

            // 키 (Q/E 기본) → 초당 변화
            if (_zoomKeys != null)
            {
                float axis = _zoomKeys.ReadValue<float>(); // +면 줌인
                if (Mathf.Abs(axis) > 0.01f)
                    delta += axis * keyZoomPerSec * dt;
            }

            return delta;
        }

        bool AllowPointer()
        {
            if (!ignoreWhenPointerOverUI) return true;
            if (EventSystem.current == null) return true;
#if UNITY_EDITOR || UNITY_STANDALONE
            return !EventSystem.current.IsPointerOverGameObject();
#else
        // 터치 등 멀티 포인터 대응 (필요시 개선)
        return !EventSystem.current.IsPointerOverGameObject();
#endif
        }

        // === Helpers ===
        float ScaledMoveSpeed()
        {
            // 줌에 따라 일정한 체감 속도를 주기 위해 orthoSize에 비례
            return moveSpeed * (_cam.orthographicSize / 10f + 0.5f);
        }

        static float Smooth(float cur, float target, ref float vel, float smoothTime, float dt)
        {
            if (smoothTime <= 0f) { vel = 0f; return target; }
            return Mathf.SmoothDamp(cur, target, ref vel, smoothTime, Mathf.Infinity, dt);
        }

        Vector3 ClampPosToBounds(Vector3 pos)
        {
            Bounds b = GetWorldBounds();
            if (b.size == Vector3.zero) return pos;

            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;

            halfW += padding.x;
            halfH += padding.y;

            float minX = b.min.x + halfW;
            float maxX = b.max.x - halfW;
            float minY = b.min.y + halfH;
            float maxY = b.max.y - halfH;

            pos.x = (minX <= maxX) ? Mathf.Clamp(pos.x, minX, maxX) : b.center.x;
            pos.y = (minY <= maxY) ? Mathf.Clamp(pos.y, minY, maxY) : b.center.y;
            return pos;
        }

        Bounds GetWorldBounds()
        {
            switch (boundsSource)
            {
                case BoundsSource.RendererBounds:
                    if (boundsRenderer != null) return boundsRenderer.bounds;
                    break;
                case BoundsSource.ColliderBounds:
                    if (boundsCollider2D != null) return boundsCollider2D.bounds;
                    break;
                case BoundsSource.ManualRect:
                    Vector2 min = manualMin;
                    Vector2 max = manualMax;
                    Vector2 size = new(Mathf.Max(0, max.x - min.x), Mathf.Max(0, max.y - min.y));
                    Vector2 center = (min + max) * 0.5f;
                    return new Bounds(center, new Vector3(size.x, size.y, 1f));
            }
            return new Bounds();
        }

        // === Input wiring ===
        void SetupActions()
        {
            if (moveActionRef && moveActionRef.action != null)            _move = moveActionRef.action;
            if (dragButtonActionRef && dragButtonActionRef.action != null) _dragButton = dragButtonActionRef.action;
            if (pointerDeltaActionRef && pointerDeltaActionRef.action != null) _pointerDelta = pointerDeltaActionRef.action;
            if (scrollActionRef && scrollActionRef.action != null)        _scroll = scrollActionRef.action;
            if (zoomKeysActionRef && zoomKeysActionRef.action != null)    _zoomKeys = zoomKeysActionRef.action;

            if (_move == null || _dragButton == null || _pointerDelta == null || _scroll == null || _zoomKeys == null)
            {
                // 기본 바인딩 자동 생성
                _ownActions = true;

                // Move (Vector2) : WASD + Arrows + Gamepad left stick
                _move = new InputAction("Move", InputActionType.Value, null, null, null, "Vector2");
                _move.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
                _move.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
                _move.AddBinding("<Gamepad>/leftStick");

                // Drag button: MMB + RMB
                _dragButton = new InputAction("DragButton", InputActionType.Button);
                _dragButton.AddBinding("<Mouse>/middleButton");
                _dragButton.AddBinding("<Mouse>/rightButton");

                // Pointer delta
                _pointerDelta = new InputAction("PointerDelta", InputActionType.Value, "<Pointer>/delta");

                // Mouse scroll
                _scroll = new InputAction("Scroll", InputActionType.Value, "<Mouse>/scroll");

                // Zoom keys: Q(-), E(+)
                _zoomKeys = new InputAction("ZoomKeys", InputActionType.Value);
                _zoomKeys.AddCompositeBinding("1DAxis")
                    .With("Negative", "<Keyboard>/q")
                    .With("Positive", "<Keyboard>/e");
            }
        }

        void EnableActions(bool enable)
        {
            if (enable)
            {
                _move?.Enable();
                _dragButton?.Enable();
                _pointerDelta?.Enable();
                _scroll?.Enable();
                _zoomKeys?.Enable();
            }
            else
            {
                _move?.Disable();
                _dragButton?.Disable();
                _pointerDelta?.Disable();
                _scroll?.Disable();
                _zoomKeys?.Disable();
            }
        }

        void DisposeOwnActions()
        {
            if (!_ownActions) return;
            _move?.Dispose();
            _dragButton?.Dispose();
            _pointerDelta?.Dispose();
            _scroll?.Dispose();
            _zoomKeys?.Dispose();
            _move = _dragButton = _pointerDelta = _scroll = _zoomKeys = null;
            _ownActions = false;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Bounds b = GetWorldBounds();
            if (b.size != Vector3.zero) Gizmos.DrawWireCube(b.center, b.size);
        }
#endif
    }
}
