using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace UIs.Runtime
{
    /// <summary>
    /// 생성할 수 있는 UI 프리팹들의 풀을 key - value로 표현한 형태
    /// </summary>
    [CreateAssetMenu(menuName = "ScriptableObject/ViewCatalog")]
    public class ViewCatalog : ScriptableObject//, ISerializationCallbackReceiver
    {
        #region Entry struct
        [Serializable]
        public class ViewEntry
        {
            public ViewID viewID;
            public AssetReferenceGameObject viewReference;
        }
        #endregion
        
        [SerializeField] private List<ViewEntry> viewTable = new();

        [SerializeField] private PresenterFactory presenterFactory;
        
        public IReadOnlyList<ViewEntry> ViewTable => viewTable;

        [NonSerialized] private Dictionary<ViewID, ViewEntry> _map;
        [NonSerialized] private bool _cacheDirty = true; // validate 용도였으나, 인스펙터 오류때문에 잠시 미사용. 무시 권장

        private void OnEnable()
        {
            _cacheDirty = true;
        }

        public bool TryGet(ViewID id, out ViewEntry entry)
        {
            EnsureCache();
            return _map.TryGetValue(id, out entry);
        }

        public IPresenter GetPresenter(ViewID id, IView view)
        {
            return presenterFactory.Create(id, view);
        }
        
        private void EnsureCache()
        {
            if (_map != null && !_cacheDirty) return;

            Dictionary<ViewID, ViewEntry> d = new(viewTable.Count);
            foreach (ViewEntry e in viewTable)
            {
                d[e.viewID] = e;
            }
            _map = d;
            _cacheDirty = false;
        }
    }
}
