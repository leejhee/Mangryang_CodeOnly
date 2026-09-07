using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace GamePlay.Features.Battle.Scripts
{
    public enum FX
    {
        DamageFX
    }
    
    [Serializable]
    public class FXEntry
    {
        public FX fx;
        public AssetReferenceGameObject fxReference; // 반드시 게임오브젝트의 어드레서블.
    }
    
    public class BattleFXManager : MonoBehaviour
    {
        #region Singleton
        private static BattleFXManager instance;
        public static BattleFXManager Instance
        {
            get
            {
                GameObject go = GameObject.Find("FXController");
                if (!go)
                {
                    go = new GameObject("FXController");
                    instance = go.AddComponent<BattleFXManager>();
                }
                return instance;
            }
            private set => instance = value;
        }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }
        #endregion

        [SerializeField] private List<FXEntry> entries = new();
        
        // duration -1이면 영구
        public async UniTask<GameObject> PlayFX(
            FX fx,
            Transform parent,
            Vector2 offset,
            CancellationToken token)
        {
            FXEntry reference = entries.Find(x => x.fx == fx);
            if (reference == null)
            {
                Debug.LogError("[FXManager] : FX not found");
                return null;
            }
            
            // 풀링 하지 않고 그냥 연출하는 식으로 함.
            GameObject go = await ResourceManager.Instance.InstantiateAsync(reference.fxReference, parent, false, token);
            go.transform.position += (Vector3)offset;

            return go;
        }
        
    }
}