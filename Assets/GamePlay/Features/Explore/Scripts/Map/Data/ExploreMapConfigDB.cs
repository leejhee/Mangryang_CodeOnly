using Core.Scripts.Foundation.Define;
using Core.Scripts.Foundation.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GamePlay.Features.Explore.Scripts.Map.Data
{
    [Serializable, CreateAssetMenu(fileName = "ExploreMapConfigDB", menuName = "ScriptableObject/ExploreMapConfigDB")]
    public class ExploreMapConfigDB : ScriptableObject
    {
        [SerializeField]
        public SerializableDict<ExploreMapKey, ExploreMapConfig> mapConfigs;

        public ExploreMapConfig GetConfig(SystemEnum.Dungeon dungeonName, int floorNum)
        {
            var key = new ExploreMapKey(dungeonName, floorNum);
            if (!mapConfigs.ContainsKey(key))
            {
                Debug.LogError($"MapConfig DB doesn't contains [dungeon : {dungeonName}, floor {floorNum}]");
                return null;
            }
            return mapConfigs[key];
                
        }

        public bool TryGetConfig(SystemEnum.Dungeon dungeonName, int floorNum, out ExploreMapConfig config)
        {
            return mapConfigs.TryGetValue(new ExploreMapKey(dungeonName, floorNum), out config);
        }
        
        public bool HasConfig(SystemEnum.Dungeon dungeonName, int floorNum)
        {
            return mapConfigs.ContainsKey(new ExploreMapKey(dungeonName, floorNum));
        }
        
        #region 디버깅 용도로??
        
        public List<ExploreMapConfig> GetConfigsByDungeon(SystemEnum.Dungeon dungeonName)
        {
            var result = new List<ExploreMapConfig>();
            foreach (var pair in mapConfigs.pairs)
            {
                if (pair.Key.dungeon == dungeonName)
                    result.Add(pair.value);
            }
            return result;
        }
        
#if UNITY_EDITOR
        [ContextMenu("Print All MapConfigs")]
        public void PrintAllKeys()
        {
            StringBuilder sb = new(); 
            foreach (var pair in mapConfigs.pairs)
            {
                sb.AppendLine($"[ExploreMapConfig] Dungeon: {pair.Key.dungeon}, Floor: {pair.Key.floor}");
            }
            Debug.Log(sb.ToString());
        }
#endif
        
        #endregion
    }
}