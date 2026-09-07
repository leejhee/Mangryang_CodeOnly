using Core.Scripts.Foundation.Define;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.BattleMap
{
    //굳이 여기서 Addressable을 안쓰는 이유:
    //DB로써 사용할 것이기 때문에, 참조를 유지해도 괜찮고, 즉 Resources나 Addressable로부터 자유로워도 괜찮기 때문.
    [CreateAssetMenu(fileName = "BattleFieldDB", menuName = "ScriptableObject/BattleFieldDB")]
    public class BattleFieldDB : ScriptableObject
    {
        [Serializable]
        public struct BattleFieldEntry
        {
            public SystemEnum.Dungeon dungeon;
            public BattleFieldGroup group;
        }
        
        [SerializeField]
        private List<BattleFieldEntry> db;
        
        private Dictionary<SystemEnum.Dungeon, BattleFieldGroup> _map;

        public IReadOnlyList<BattleFieldEntry> DB => db;

        private void OnEnable()
        {
            BuildMap();
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            BuildMap();
        }
#endif
        
        private void BuildMap()
        {
            _map = new Dictionary<SystemEnum.Dungeon, BattleFieldGroup>(db?.Count ?? 0);
            if (db == null)
                return;

            foreach (var d in db)
            {
                if(!_map.ContainsKey(d.dungeon))
                    _map.Add(d.dungeon, d.group);
                else
                    Debug.LogWarning($"[BattleFieldDB] Duplicate dungeon {d.dungeon}");
            }
        }

        public BattleFieldGroup ResolveOrNull(SystemEnum.Dungeon dungeon)
        {
            if (_map == null || _map.Count != (db?.Count ?? 0)) BuildMap();
            return _map.GetValueOrDefault(dungeon);
        }
        
        public BattleFieldGroup Resolve(SystemEnum.Dungeon dungeon)
        {
            var group = ResolveOrNull(dungeon);
            if(!group) throw new System.Exception($"Cannot resolve dungeon {dungeon}");
            return group;
        }
    }
}
