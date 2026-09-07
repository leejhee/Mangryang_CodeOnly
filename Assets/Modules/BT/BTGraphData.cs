using System;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.BT
{
    #region Graph Data
    [Serializable]
    public class BTNodeData
    {
        public string guid;
        public string nodeType;
        public string parameter;
        public Vector2 position;
    }

    [Serializable]
    public class BTNodeLinkData
    {
        public string outputNodeGuid;
        public string inputNodeGuid;
    }
    #endregion
    
    [CreateAssetMenu(menuName = "ScriptableObject/BehaviourTreeAsset")]
    public class BTGraphData : ScriptableObject
    {
        public List<BTNodeData> nodes = new();
        public List<BTNodeLinkData> links = new();
    }
}