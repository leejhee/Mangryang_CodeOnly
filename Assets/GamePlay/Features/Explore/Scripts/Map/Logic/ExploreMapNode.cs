using Core.Scripts.Foundation.Define;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Features.Explore.Scripts.Map.Logic
{
    /// <summary>
    /// 탐사 맵에서의 단위 타일을 의미함. 단순한 C# 클래스이며 여기서 프리팹을 Instantiate된다. 
    /// </summary>
    [Serializable]
    public class ExploreMapNode
    {
        public Vector2Int Pos { get; }
        public SystemEnum.eNodeType NodeType { get; }

        private readonly List<ExploreMapNode> _neighbors;
        public IReadOnlyList<ExploreMapNode> Neighbors => _neighbors.AsReadOnly();

        public ExploreMapNode(Vector2Int pos, SystemEnum.eNodeType nodeType)
        {
            Pos = pos;
            NodeType = nodeType;
            _neighbors = new();
        }
        
        public void Connect(ExploreMapNode node)
        {
            _neighbors.Add(node);
            node.Connect(this);
        }
        
    }
    
    
    
    
    
}