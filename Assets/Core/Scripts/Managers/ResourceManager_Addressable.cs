using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core.Scripts.Managers
{
    public partial class ResourceManager
    {
        private Task _initTask;
        
        // 스레드 안전 전용
        private readonly object _handleGate = new();
        
        // 에셋 로드용 핸들(Resources 측면에서 _cache가 그 역할)
        private readonly Dictionary<object, AsyncOperationHandle> _assetHandles = new();
        // 인스턴스용 핸들
        private readonly Dictionary<GameObject, AsyncOperationHandle<GameObject>> _instanceHandles = new();
        
        #region Initialization

        private async Task DoInitAsync()
        {
            var h = Addressables.InitializeAsync();
            await h.Task; // 실패하면 여기서 예외전파 해야할거같은데...?
            
            
        }
        
        // UniTask로 사용할 용도의 래퍼
        public UniTask InitAsync()
        {
            lock (_handleGate)
            {
                if (_initTask != null && !_initTask.IsCanceled && !_initTask.IsFaulted)
                    return _initTask.AsUniTask();

                _initTask = DoInitAsync();
                return _initTask.AsUniTask();
            }
        }

        private UniTask EnsureInitializationAsync()
        {
            var t = InitAsync();
            if (!t.Status.IsCompleted()) return t;
            return UniTask.CompletedTask;
        }
        
        #endregion
        
        #region Load & Release Part
        
        private static object NormalizeKey(object key)
        {
            if (key is AssetReference ar) return ar.RuntimeKey;
            return key;
        }
        
        public async UniTask<T> LoadAsync<T>(object key, CancellationToken ct = default) where T : UnityEngine.Object
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            await EnsureInitializationAsync(); // 확실히 초기화되었는지 확인.
            
            key = NormalizeKey(key);
            // 캐시 조회
            lock (_handleGate)
            {
                if (_assetHandles.TryGetValue(key, out var existing) && existing.IsValid())
                    return existing.Result as T;
            }
            
            // 로드
            var handle = Addressables.LoadAssetAsync<T>(key);
            //while (!handle.IsDone)
            //{
            //    ct.ThrowIfCancellationRequested();
            //    await UniTask.Yield(ct);
            //}
            await handle.Task.AsUniTask().AttachExternalCancellation(ct);

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(handle); //실패하면 핸들 놔야지.
                throw new InvalidOperationException($"[Addressable] Load Failed : {key} | {handle.OperationException}");
            }
            
            lock(_handleGate) _assetHandles.Add(key, handle);
            return handle.Result;
        }
        
        public bool TryGet<T>(object key, out T asset) where T : UnityEngine.Object
        {
            lock (_handleGate)
            {
                key = NormalizeKey(key);
                if (_assetHandles.TryGetValue(key, out var existing) && existing.IsValid())
                {
                    asset = existing.Result as T;
                    return asset != null;
                }
            }

            asset = null;
            return false;
        }
        
        public void Release(object key)
        {
            if (key == null) return;
            lock (_handleGate)
            {
                key = NormalizeKey(key);
                if (_assetHandles.TryGetValue(key, out var existing) && existing.IsValid())
                {
                    Addressables.Release(existing);
                    _assetHandles.Remove(key);
                }
            }
        }

        public void ReleaseAllAssets()
        {
            lock (_handleGate)
            {
                foreach (var handle in _assetHandles.Values)
                {
                    if(handle.IsValid())
                        Addressables.Release(handle);
                }
                _assetHandles.Clear();
            }
        }
        
        #region Load & Release By Label - SO
        
        /// <summary>
        /// 이게 왜 있냐면... 다른 에셋의 캐시와 충돌하게 하고 싶지 않아서 만든거다.
        /// </summary>
        private readonly struct LabelKey : IEquatable<LabelKey>
        {
            private readonly object _label;
            private readonly Type _type;
            public LabelKey(object label, Type type) { _label = label; _type = type; }
            public static LabelKey Of<T>(object label) where T : UnityEngine.Object => new LabelKey(label, typeof(T));
            public bool Equals(LabelKey other) => Equals(_label, other._label) && _type == other._type;
            public override bool Equals(object obj) => obj is LabelKey other && Equals(other);
            public override int GetHashCode()
            {
                unchecked { return ((_label?.GetHashCode() ?? 0) * 397) ^ _type.GetHashCode(); }
            }
            public override string ToString() => $"{_type.Name}@{_label}";
        }

        public async UniTask<IReadOnlyList<T>> LoadSOByLabelAsync<T>(object label, CancellationToken ct = default)
            where T : ScriptableObject
        {
            if(label == null) throw new ArgumentNullException(nameof(label));
            await EnsureInitializationAsync();
            
            var labelKey = LabelKey.Of<T>(label);

            lock (_handleGate)
            {
                if(_assetHandles.TryGetValue(labelKey, out var existing) && existing.IsValid())
                    return existing.Result as IReadOnlyList<T>;
            }
            
            var h = Addressables.LoadAssetsAsync<T>(label, null);

            await h.Task.AsUniTask().AttachExternalCancellation(ct);

            if (h.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(h);
                throw new InvalidOperationException(
                    $"[Addressable] LoadAssets(SO) failed: label={label}, type={typeof(T).Name}");
            }
            
            lock(_handleGate) _assetHandles.Add(labelKey, h);
            return h.Result as IReadOnlyList<T>;
        }

        public bool TryGetSOList<T>(object label, out IReadOnlyList<T> list) where T : ScriptableObject
        {
            var labelKey = LabelKey.Of<T>(label);
            lock (_handleGate)
            {
                if (_assetHandles.TryGetValue(labelKey, out var existing) && existing.IsValid())
                {
                    list = existing.Result as IReadOnlyList<T>;
                    return list != null;

                }
            }
            list = null;
            return false;
        }

        public void ReleaseLabelSO<T>(object label) where T : ScriptableObject
        {
            var labelKey = LabelKey.Of<T>(label);
            lock (_handleGate)
            {
                if (_assetHandles.TryGetValue(labelKey, out var existing) && existing.IsValid())
                {
                    Addressables.Release(existing);
                    _assetHandles.Remove(labelKey);
                }
            }
        }
        
        #endregion
        
        #endregion
        
        /// <summary>
        /// 비동기 인스턴스화. 실패 시 바로 release 한다.
        /// </summary>
        /// <exception cref="InvalidOperationException">키 오류로 인한 throw이므로 해당 오류 시 살펴볼 것</exception>
        public async UniTask<GameObject> InstantiateAsync(
            object key, Transform parent = null, bool worldSpace = false, CancellationToken ct = default)
        {
            await EnsureInitializationAsync(); // 초기화 보장
            var h = Addressables.InstantiateAsync(key, parent, worldSpace);

            while (!h.IsDone)
            {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield(ct);
            }

            if (h.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(h);
                throw new InvalidOperationException($"[Addressable] Instantiate failed: {key}");
            }

            GameObject go = h.Result; // 인스턴스화의 결과니까 게임오브젝트다.
            string goName = go.name;
            goName = goName.Replace("(Clone)", "");
            go.name = goName;
            lock (_handleGate) _instanceHandles[go] = h;
            return go;
        }

        public void ReleaseInstance(GameObject go)
        {
            if (!go) return;
            lock (_handleGate)
            {
                if (_instanceHandles.TryGetValue(go, out var handle))
                {
                    Addressables.ReleaseInstance(handle);
                    _instanceHandles.Remove(go);
                    return;
                }
            }
            //Instantiate된 애를 부수고 release까지 해준다.
            Addressables.ReleaseInstance(go);
        }
        
        /// <summary>
        /// 부모 이하 모든 게임오브젝트들을 release함
        /// </summary>
        public void ReleaseAllInstancesUnder(Transform root)
        {
            if (!root) return;
            var releasing = new List<GameObject>();
            lock (_handleGate)
            {
                foreach (var go in _instanceHandles.Keys)
                {
                    if(!go) 
                    {
                        releasing.Add(go);
                        continue;
                    }
                    if(go.transform.IsChildOf(root))
                        releasing.Add(go);
                }
            }
            foreach(var go in releasing)
                ReleaseInstance(go);
        }
        
    }
}
