using Core.Scripts.Foundation.Define;
using System;
using UnityEngine;

namespace Core.Scripts.Managers
{
    public class GameManager : MonoBehaviour
    {
        #region 싱글턴.

        private static GameManager instance;
        private static bool isShuttingDown;

        public static bool HasInstance => instance;
        public static bool IsShuttingDown => isShuttingDown;

        public static GameManager Instance
        {
            get
            {
                if (instance)
                    return instance;

                // Play Mode 종료/앱 종료 중에는 파괴 순서가 보장되지 않는다.
                // 이 시점에 새 매니저를 만들면 씬 정리 검증에서 누수로 판정된다.
                if (isShuttingDown)
                    return null;

                Init();
                return instance;
            }
        }

        public static bool TryGetInstance(out GameManager manager)
        {
            manager = instance;
            return manager;
        }

        GameManager() { }
        #endregion
        
        #region Game State Management
        
        // 필요하다! - 저장 구조를 위해서 필요함.
        private SystemEnum.GameState _state;
        public SystemEnum.GameState GameState
        {
            get => _state;
            set
            {
                BeforeGameStateChange?.Invoke(_state);
                _state = value;
                OnGameStateChanged?.Invoke(value);
            }
        }
        
        public event Action<SystemEnum.GameState> BeforeGameStateChange;
        public event Action<SystemEnum.GameState> OnGameStateChanged;
        
        #endregion
        
        #region Initialization
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            isShuttingDown = false;
        }

        private static void Init()
        {
            if (isShuttingDown)
                return;

            GameObject go = GameObject.Find("@GameManager");
            if (!go)
                go = new GameObject { name = "@GameManager" };

            instance = go.GetComponent<GameManager>();
            if (!instance)
                instance = go.AddComponent<GameManager>();

            if (Application.isPlaying)
                DontDestroyOnLoad(go);
        }
        #endregion
        
        #region Events responsible with GameManager
        
        public event Action<QuitParam> OnQuit;
        public event Action<PauseParam> OnPause;
        public event Action OnUpdate;
        
        #endregion
        
        #region Unity Events
        private void Awake()
        {
            if (instance && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            isShuttingDown = false;
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            OnUpdate?.Invoke();
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
            OnQuit?.Invoke(new QuitParam());
            //강종 대비
            SaveLoadManager.Instance.OnApplicationQuit();
            DataManager.Instance.ClearCache();
            ResourceManager.Instance.Clear();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            OnPause?.Invoke(new PauseParam());
            //모바일 사례 : 갑자기 내린다면
            SaveLoadManager.Instance.OnApplicationPause(pauseStatus);
        }

        private void OnDestroy()
        {
            if (instance != this)
                return;

            instance = null;
            if (!Application.isPlaying)
                isShuttingDown = true;
        }
        #endregion
    }
    
    public class QuitParam {}
    public class PauseParam{}
}

