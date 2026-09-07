using System;
using System.Collections.Generic;

namespace Modules.RoguelikeNodeMap.MapSkeleton
{
    [Serializable]
    public class MapFloor
    {
        private int floorNum;

        private List<MapNode> floorMembers = new List<MapNode>();

        public int FloorNum { get { return floorNum; } }
        public List<MapNode> FloorMembers { get { return floorMembers; } }
        public int MemberCount { get { return floorMembers.Count; } }

        public MapFloor(int floorNum) { this.floorNum = floorNum; }

        public MapNode AddNode(MapNode node)
        {
            if (!ContainsNode(node.GridPoint.x))
            {
                floorMembers.Add(node);
                return node;
            }
            else return GetNode(node.GridPoint.x);
        }

        public void RemoveNode(MapNode node)
        {
            if (ContainsNode(node))
                floorMembers.Remove(node);
        }

        // 필요할까?
        public bool ContainsNode(int x)
        {        
            MapNode node = floorMembers.Find(node => node.GridPoint.x == x);
            return node != null;
        }

        public bool ContainsNode(MapNode node)
        {
            return floorMembers.Contains(node);
        }

        public MapNode GetNode(int x)
        {
            return floorMembers.Find(node => node.GridPoint.x == x);
        }
    }
}