using Modules.PathFinder;
using Modules.PathFinder.Grid;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.RoguelikeNodeMap.MapSkeleton
{
    [Serializable]
    public class MapNode : IComparable<MapNode>, IEquatable<MapNode>
    { 
        [SerializeField] 
        private int _nodeID; //혹시 몰라서
        private GridPoint _gridPoint;
        private List<MapNode> _parents;
        private List<MapNode> _children;
        //private BaseMapNodeData _nodeData = null;

        public int NodeID {  get { return _nodeID; } }
        public GridPoint GridPoint { get { return _gridPoint; } }
        //각각 맵 설정 시 외부에서 넣는다.
        public List<MapNode> Parents { get { return _parents; } }
        public List<MapNode> Children { get { return _children; } }
        //public BaseMapNodeData NodeData { get { return _nodeData; } }

        public MapNode(int ID, int x, int y)
        {
            _nodeID = ID;
            _gridPoint = new GridPoint(x, y);
            _children = new List<MapNode>();
            _parents = new List<MapNode>();
        }

        public void SetParents(MapNode node, bool isAdd)
        {
            if (isAdd) { if (!Parents.Contains(node)) Parents.Add(node); }
            else        { if (Parents.Contains(node)) Parents.Remove(node); }
        }

        public void SetChildren(MapNode node, bool isAdd)
        {
            if (isAdd) { if (!Children.Contains(node)) Children.Add(node); }
            else        { if (Children.Contains(node)) Children.Remove(node); }
        }

        //public void SetNodeData(BaseMapNodeData nodeData) => _nodeData = nodeData;

        #region operator overloading
        public bool Equals(MapNode other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            if(obj == null || GetType() != obj.GetType())
                return false;

            MapNode other = (MapNode)obj;
            return _gridPoint == other._gridPoint;
        }

        public override int GetHashCode()
        {
            return _gridPoint.GetHashCode();
        }

        public int CompareTo(MapNode other)
        {
            return this.GridPoint.x - other.GridPoint.x;
        }

        public static bool operator ==(MapNode first, MapNode second)
        {
            if (ReferenceEquals(first, second)) return true;
            if (first is null || second is null) return false;
            return first.GridPoint.x == second.GridPoint.x && first.GridPoint.y == second.GridPoint.y;
        }

        public static bool operator !=(MapNode first, MapNode second)
        {
            return !(first == second);
        }
        #endregion
    }
}



