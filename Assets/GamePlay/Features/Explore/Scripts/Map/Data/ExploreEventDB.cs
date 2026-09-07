using Core.Scripts.Foundation.Define;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Features.Explore.Scripts.Map.Data
{
    [CreateAssetMenu(fileName = "ExploreEventDB", menuName = "ScriptableObject/ExploreEventDB")]
    public class ExploreEventDB : ScriptableObject
    {
        [Serializable]
        public struct SymbolKeyValue
        {
            public SystemEnum.CellEventType eventType;
            public GameObject symbolPrefab;
        }
        
        public List<SymbolKeyValue> symbols;
    }
}