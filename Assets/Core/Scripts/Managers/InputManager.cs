using Core.Scripts.Foundation.Define;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Scripts.Managers
{
    public class InputManager : MonoBehaviour
    {
        #region Singleton
        public static InputManager Instance { get; private set; }
        private InputManager() { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;
        #endregion

        [Header("Unity PlayerInput 컴포넌트")]
        [SerializeField] private PlayerInput input;

        // 액션맵 캐시
        private InputActionMap _commonMap;
        private InputActionMap _exploreMap;
        private InputActionMap _battleMap;
        private InputActionMap _uiMap;
        private InputActionMap _cameraMap;

        // UI만 입력 받을 때 true (노벨, 설정창 등)
        private bool _uiOnly;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (!input)
                input = GetComponent<PlayerInput>();

            SetupActionMaps();

            // GameState 바뀔 때마다 입력 모드 갱신 :contentReference[oaicite:2]{index=2}
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;

            // 처음 상태 반영
            OnGameStateChanged(GameManager.Instance.GameState);
        }

        private void OnDestroy()
        {
            if (GameManager.TryGetInstance(out GameManager manager))
                manager.OnGameStateChanged -= OnGameStateChanged;

            if (Instance == this)
                Instance = null;
        }

        #region 초기 셋업

        private void SetupActionMaps()
        {
            var asset = input.actions;

            _commonMap  = asset.FindActionMap("Common",        false);
            _exploreMap = asset.FindActionMap("ExploreVillage",false);
            _battleMap  = asset.FindActionMap("Battle",        false);
            _uiMap      = asset.FindActionMap("UI",            false);
            _cameraMap  = asset.FindActionMap("Camera",        false);

            _commonMap?.Enable();
        }

        #endregion

        #region Mapping

        private void OnGameStateChanged(SystemEnum.GameState state)
        {
            if (_uiOnly) return;

            ApplyStateMaps(state);
        }

        private void ApplyStateMaps(SystemEnum.GameState state)
        {
            _exploreMap?.Disable();
            _battleMap?.Disable();
            _cameraMap?.Disable();
            _uiMap?.Disable(); 

            switch (state)
            {
                case SystemEnum.GameState.Explore:
                _exploreMap?.Enable();
                _cameraMap?.Enable();
                break;

                case SystemEnum.GameState.Battle:
                _battleMap?.Enable();
                _cameraMap?.Enable();
                break;

            }
        }

        #endregion

        #region UI 전용 모드 토글 (노벨 / 메뉴 등)

        /// <summary>
        /// true  : Explore/Battle/Camera 끄고 UI 액션맵만 켠다.
        /// false : UI 끄고, 현재 GameState에 맞는 액션맵 다시 켠다.
        /// </summary>
        public void SetUIOnly(bool uiOnly)
        {
            _uiOnly = uiOnly;

            if (uiOnly)
            {
                _exploreMap?.Disable();
                _battleMap?.Disable();
                _cameraMap?.Disable();

                _uiMap?.Enable();
            }
            else
            {
                _uiMap?.Disable();
                if (GameManager.TryGetInstance(out GameManager manager))
                    ApplyStateMaps(manager.GameState);
            }
        }

        #endregion

        #region [Explore & Village] Input Wrapper

        // 이동
        public Vector2 GetExploreMove()
        {
            if (_exploreMap == null) return Vector2.zero;
            InputAction action = _exploreMap.FindAction("ControllerMove");
            return action?.ReadValue<Vector2>() ?? Vector2.zero;
        }

        // 상호작용
        public bool GetExploreInteractDown()
        {
            if (_exploreMap == null) return false;
            InputAction action = _exploreMap.FindAction("Interact");
            return action != null && action.WasPressedThisFrame();
        }

        #endregion
        
        #region Battle Input Wrapper

        public void EnableBattleInput()
        {
            _battleMap?.Enable();
            _cameraMap?.Enable();
        }
        
        public void DisableBattleInput()
        {
            _battleMap?.Disable();
            _cameraMap?.Disable();
        }

        public bool GetBattleQuitQuery()
        {
            if(_battleMap == null) return false;
            InputAction action =  _battleMap.FindAction("EndBattleCheat");
            return action != null && action.WasPressedThisFrame();
        }
        
        #endregion
        
        #region Camera Input Wrapper

        private InputAction GetCameraAction(string actionName)
        {
            if (_cameraMap == null) return null;
            return _cameraMap.FindAction(actionName);
        }

        /// <summary>카메라 드래그 버튼 (PanButton) 다운</summary>
        public bool GetCameraPanButtonDown()
        {
            InputAction action = GetCameraAction("PanButton");
            return action != null && action.WasPressedThisFrame();
        }

        /// <summary>카메라 드래그 버튼 업</summary>
        public bool GetCameraPanButtonUp()
        {
            InputAction action = GetCameraAction("PanButton");
            return action != null && action.WasReleasedThisFrame();
        }

        /// <summary>마우스 드래그 델타 (Pan)</summary>
        public Vector2 GetCameraPanDelta()
        {
            InputAction action = GetCameraAction("Pan");
            return action != null ? action.ReadValue<Vector2>() : Vector2.zero;
        }

        /// <summary>휠 스크롤 값</summary>
        public float GetCameraZoomDelta()
        {
            InputAction action = GetCameraAction("Zoom");
            if (action == null) return 0f;

            Vector2 v = action.ReadValue<Vector2>();
            return v.y;
        }

        #endregion
    }
}
