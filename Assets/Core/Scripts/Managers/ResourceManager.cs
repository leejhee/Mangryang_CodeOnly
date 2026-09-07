using Core.Scripts.Foundation.Singleton;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Scripts.Managers
{
    public partial class ResourceManager : SingletonObject<ResourceManager>
    {
        Dictionary<string, Object> _cache = new Dictionary<string, Object>();

        #region 생성자
        ResourceManager () { }
        #endregion
        
        private string GetPrefabPath(string path) => $"Prefabs/{path}";

        public T Load<T>(string path) where T : Object
        {
            string name = path;

            Object obj;

            if (_cache == null)
            {
                _cache = new Dictionary<string, Object>();
            }

            //캐시에 존재 -> 캐시에서 반환
            if (_cache.TryGetValue(name, out obj))
                return obj as T;

            //캐시에 없음 -> 로드하여 캐시에 저장 후 반환
            obj = Resources.Load<T>(path);
            _cache.Add(name, obj);

            return obj as T;
        }
    
        public Sprite LoadSprite(string path) => Load<Sprite>("Sprites/" + path);
    
        public GameObject Instantiate(string path, Transform parent = null) => Instantiate<GameObject>(path, parent);

        public GameObject Instantiate(GameObject go, Transform parent = null) => Object.Instantiate(go, parent);

        public AudioClip LoadAudioClip(string path) => Instance.Load<AudioClip>($"Sounds/{path}");

        public T Instantiate<T>(string path, Transform parent = null) where T : UnityEngine.Object
        {
            T prefab = Load<T>(GetPrefabPath(path));
            if (prefab == null)
            {
                Debug.LogError($"Failed to load prefab : {path}");
                return null;
            }

            T instance = null;
            if (parent == null)
            {

                instance = UnityEngine.Object.Instantiate<T>(prefab);
            }
            else
            {
                instance = UnityEngine.Object.Instantiate<T>(prefab, parent);
            }
            instance.name = prefab.name;


            return instance;
        }

        public void Clear()
        {
            if (_cache == null)
                _cache = new Dictionary<string, Object>();

            foreach (var kvp in _cache)
            {
                Resources.UnloadAsset(kvp.Value);
            }
            _cache.Clear();
        }

        public bool Clear(Object clearObject)
        {
            if (clearObject == null)
            {
                Debug.LogError("ClearObject is null.");
                return false;
            }

            string keyToRemove = null;

            // 사전을 순회하며 값이 일치하는 항목을 찾음
            foreach (var kvp in _cache)
            {
                if (kvp.Value == clearObject)
                {
                    keyToRemove = kvp.Key;
                    break; // 일치하는 항목을 찾으면 루프를 중단
                }
            }

            if (keyToRemove != null || keyToRemove == "")
            {
                return Clear(keyToRemove);
            }
            return false;
        }

        public bool Clear(string clearKey)
        {
            if (!clearKey.StartsWith("Prefabs/"))
            {
                clearKey = GetPrefabPath(clearKey);
            }
            if (_cache.TryGetValue(clearKey, out Object obj))
            {
                Resources.UnloadAsset(obj);
                _cache.Remove(clearKey);
                return true;
            }
            else
            {
                Debug.LogWarning($"No resource found with name: {clearKey}");
                return false;
            }
        }

        public void Destroy(GameObject go)
        {
            if (go == null) return;
            Object.Destroy(go);
        }
    }
}