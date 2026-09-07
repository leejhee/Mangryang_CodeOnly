using Core.Scripts.Foundation.Define;
using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UIs.Runtime
{
    /// <summary>
    /// GameManager와 같은, DDOL Manager
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Singleton
        private static UIManager instance;

        public static UIManager Instance => instance;
        
        #endregion
        
        #region DB
        #region Temporary Entry
        
        /// <summary>
        /// SerializableDict의 Inspector상 오류 수정 전까지 사용
        /// </summary>
        [Serializable]
        private class CatalogEntry
        {
            public SystemEnum.GameState keyState;
            public ViewCatalog catalog;
        }
        
        #endregion
        
        /// <summary>
        /// View에 대한 데이터베이스
        /// </summary>
        [SerializeField] private List<CatalogEntry> entries = new();

        [SerializeField, ReadOnly] private ViewCatalog focusingCatalog;
        
        private readonly Dictionary<SystemEnum.GameState, ViewCatalog> _catalogDict = new();
        
        private void ChangeCatalog(SystemEnum.GameState state) => focusingCatalog = _catalogDict.GetValueOrDefault(state);
        
        #endregion
        
        #region Canvas Scaler Settings
        [Header("Canvas Scaler Settings")]
        [SerializeField] private Vector2 referenceResolution = new(1920, 1080);

        [SerializeField, Range(0f, 1f)] private float match = 0.5f;

        [SerializeField] private CanvasScaler.ScreenMatchMode screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        [SerializeField] private CanvasScaler.ScaleMode scaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        
        private void SetupScaler(Canvas canvas)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (!scaler) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = scaleMode;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = screenMatchMode;
            scaler.matchWidthOrHeight = match;
        }       
        
        #endregion
        
        #region Stack & Cache
        private struct OpenedUI
        {
            public ViewID ID; 
            public IPresenter Presenter;
            public GameObject Go;
            public bool isPopup;
        }
        
        private readonly Stack<OpenedUI> _stack = new();
        
        
        /// <summary>
        /// View의 캐싱을 위한 딕셔너리.
        /// </summary>
        private Dictionary<ViewID, Stack<GameObject>> _cache = new();

        [SerializeField] private int cacheCapacity = 5;
        private int _popupCount = 0;
        
        private async UniTask<GameObject> GetViewFromCache(ViewID id, ViewCatalog.ViewEntry entry, Transform parent)
        {
            if (_cache.TryGetValue(id, out Stack<GameObject> st) && st.Count > 0)
            {
                GameObject go = st.Pop();
                go.transform.SetParent(parent, false);
                go.SetActive(true);
                go.transform.SetAsLastSibling();
                return go;
            }

            GameObject inst =
                await ResourceManager.Instance.InstantiateAsync(entry.viewReference, parent, false, _uiCts.Token);
            inst.transform.SetAsLastSibling();
            return inst;
        }

        private void ReturnViewToCache(ViewID id, GameObject go)
        {
            if (!go) return;
            go.SetActive(false);
            go.transform.SetParent(_cacheRoot ? _cacheRoot : _uiRoot, false);

            if (!_cache.TryGetValue(id, out Stack<GameObject> st))
            {
                _cache[id] = st = new Stack<GameObject>();
            }

            if (st.Count < cacheCapacity) st.Push(go);
            else ResourceManager.Instance.ReleaseInstance(go);
        }
        
        #endregion
        
        // layer root도 전부 handle을 미보유. view만 release할 것.
        #region Root & Layer Transform
        [SerializeField] private AssetReferenceGameObject rootPrefabReference;

        private Transform _uiRoot;
        private Transform _mainRoot, _modalRoot, _systemRoot, _worldRoot;
        private Transform _bgRoot;
        private Transform _cacheRoot;

        public Transform WorldRoot => _worldRoot;
        public Transform BGRoot => _bgRoot;
        
        private async UniTask EnsureRoot(CancellationToken token)
        {
            if (_uiRoot) return;

            var go = await ResourceManager.Instance.InstantiateAsync(rootPrefabReference, null, false, token);
            _uiRoot = go.transform;
            
            _mainRoot   = _uiRoot.Find("@MainRoot")   ?? CreateLayer("@MainRoot",   0);
            _modalRoot  = _uiRoot.Find("@ModalRoot")  ?? CreateLayer("@ModalRoot",  100);
            _systemRoot = _uiRoot.Find("@SystemRoot") ?? CreateLayer("@SystemRoot", 1000);
            _cacheRoot  = _uiRoot.Find("@UICache")    ?? CreateCache("@UICache");
            // 이거 월드스페이스 캔버스 order 변경해줘야함
            _worldRoot = _uiRoot.Find("@WorldRoot") ?? CreateLayer("@WorldRoot", 1000);
            
            // 배경화면 전용 캔버스 불필요시 나중에 제거
            if(GameManager.Instance.GameState == SystemEnum.GameState.Battle)
                _bgRoot = _uiRoot.Find("@BGRoot") ?? CreateBackgroundUILayer("@BGRoot");
        }
        
        private Transform CreateLayer(string layerName, int order)
        {
            GameObject g = new(layerName);
            g.transform.SetParent(_uiRoot, false);
            Canvas c = g.AddComponent<Canvas>();
            c.overrideSorting = true; c.sortingOrder = order;
            g.AddComponent<GraphicRaycaster>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            SetupScaler(c);
            return g.transform;
        }

        private Transform CreateCache(string cacheName)
        {
            GameObject g = new(cacheName);
            g.transform.SetParent(_uiRoot, false);
            return g.transform;
        }

        private Transform CreateBackgroundUILayer(string layerName)
        {
            GameObject g = new(layerName);
            g.layer = 13;
            g.transform.SetParent(_uiRoot, false);
            Canvas c = g.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceCamera;
            
            Camera uiCam = GameObject.Find("BackgroundCamera").GetComponent<Camera>();
            c.worldCamera = uiCam;
            
            return g.transform;
        }
        #endregion
        
        /// <summary>
        /// UI 전용 CTS
        /// </summary>
        private CancellationTokenSource _uiCts;
        
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            GameManager.Instance.OnGameStateChanged += ChangeCatalog;
            foreach (CatalogEntry pair in entries)
            {
                _catalogDict[pair.keyState] = pair.catalog; //빠른 인덱싱을 위해
            }
            focusingCatalog = _catalogDict[SystemEnum.GameState.Lobby]; // 초기 포커싱 설정
            _uiCts = new CancellationTokenSource();
        }
        
        public async void InstantiateBackgroundObject(GameObject prefab)
        {
            await EnsureRoot(_uiCts.Token);
            
            // 이때 handle 생성 안하므로 release 별도 필요 없음
            GameObject bgObject = Instantiate(prefab, _bgRoot);

            if (bgObject.TryGetComponent<RectTransform>(out var rt))
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.localPosition = Vector3.zero;
            }
            if (bgObject.TryGetComponent<Image>(out var img))
            {
                img.preserveAspect = false; 
                img.raycastTarget = false;
            }
        }
        
        public async UniTask ShowViewAsync(ViewID viewID)
        {
            if (!focusingCatalog) return;

            await EnsureRoot(_uiCts.Token);

            if (!focusingCatalog.TryGet(viewID, out ViewCatalog.ViewEntry viewRef))
            {
                Debug.LogWarning($"[UIManager] No Entry for {viewID}");
                return;
            }

            GameObject go = null;
            try
            {
                Transform parent = _mainRoot;
                go = await GetViewFromCache(viewID, viewRef, parent);
                if (!go.TryGetComponent(out IView view))
                {
                    ResourceManager.Instance.ReleaseInstance(go);
                    Debug.LogError($"[UIManager] No IView in Prefab for {viewID}");
                    return;
                }

                bool isPopup = view is PopupView;
                if (isPopup && _modalRoot)
                {
                    go.transform.SetParent(_modalRoot, false);
                    go.transform.SetAsLastSibling();
                }
                
                IPresenter presenter = focusingCatalog.GetPresenter(viewID, view);
                _stack.Push(new OpenedUI { ID = viewID, Presenter = presenter, Go = go, isPopup = isPopup });
                if (isPopup)
                {
                    _popupCount++;
                    if(_popupCount == 1)
                        InputManager.Instance.SetUIOnly(true);
                }
                
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(_uiCts.Token);
                await presenter.OnEnterAsync(linked.Token);

                go = null;
            }
            catch (OperationCanceledException)
            {
                // 씬 교체로 UI 수명이 끝난 경우에는 정상적인 취소다.
                if (go) ResourceManager.Instance.ReleaseInstance(go);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (go) ResourceManager.Instance.ReleaseInstance(go);
                throw;
            }
        }

        

        public async UniTask HideTopViewAsync()
        {
            if (_stack.Count == 0) return;
            OpenedUI current = _stack.Pop();
            
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_uiCts.Token);
            try
            {
                await current.Presenter.OnExitAsync(linked.Token);
            }
            finally
            {
                current.Presenter.Dispose();
                ReturnViewToCache(current.ID, current.Go);
            }

            if (current.isPopup)
            {
                _popupCount--;
                if (_popupCount <= 0)
                {
                    _popupCount = 0;
                    InputManager.Instance.SetUIOnly(false);
                }
            }
        }
        
        #region Unity Events
        
        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }
        
        /// <summary>
        /// 씬이 교체될 때 UI 스택을 모두 정리시켜준다.
        /// </summary>
        /// <param name="oldScene"></param>
        /// <param name="newScene"></param>
        private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            _uiCts?.Cancel(); _uiCts?.Dispose(); _uiCts = new CancellationTokenSource();

            while (_stack.Count > 0)
            {
                var x = _stack.Pop();
                try { x.Presenter?.Dispose(); } catch {}
                if (x.Go) ResourceManager.Instance.ReleaseInstance(x.Go);
            }

            foreach (var kv in _cache)
            {
                var st = kv.Value;
                while (st.Count > 0)
                {
                    var go = st.Pop();
                    if (go) ResourceManager.Instance.ReleaseInstance(go);
                }
            }
            _cache.Clear();

            if (_uiRoot)
            {
                ResourceManager.Instance.ReleaseInstance(_uiRoot.gameObject);
                _uiRoot = _mainRoot = _modalRoot = _systemRoot = _worldRoot = _bgRoot = _cacheRoot = null;
            }
            
            _popupCount = 0;
            if (InputManager.Instance)
                InputManager.Instance.SetUIOnly(false);
        }
        
        /// <summary>
        /// 게임이 종료될 때긴 하지만, 만일을 위해 묶인 모든 리소스와 이벤트를 해제한다.
        /// </summary>
        private void OnDestroy()
        {
            if (GameManager.TryGetInstance(out GameManager manager))
                manager.OnGameStateChanged -= ChangeCatalog;

            while (_stack.Count > 0)
            {
                OpenedUI top = _stack.Pop();
                try { top.Presenter?.Dispose(); }
                catch { }
                
                if(top.Go) ResourceManager.Instance.ReleaseInstance(top.Go);
            }

            foreach (var kv in _cache)
            {
                Stack<GameObject> st = kv.Value;
                while (st.Count > 0)
                {
                    GameObject go = st.Pop();
                    if(go) ResourceManager.Instance.ReleaseInstance(go);
                }
            }
            _cache.Clear();
            
            _popupCount = 0;
            if (InputManager.Instance)
                InputManager.Instance.SetUIOnly(false);
            
            if (_uiRoot)
            {
                ResourceManager.Instance.ReleaseInstance(_uiRoot.gameObject);
                _uiRoot = null;
            }
            
            _uiCts?.Cancel();
            _uiCts?.Dispose();

            if (instance == this)
                instance = null;
        }
        
        #endregion
    }
}
