using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Modules.RoguelikeNodeMap.MapSkeleton
{
    [Serializable]
    public class Map
    {
        private List<MapFloor> _mapNodes;
        private List<MapPath> _mapPaths;
        public List<MapFloor> MapNodes { get { return _mapNodes; } }
        public List<MapPath> MapPaths { get { return _mapPaths; } }

        // 플레이어 현재 위치 용도.
        public MapNode Current = null;

        public Map()
        {
            _mapNodes = new List<MapFloor>();
            _mapPaths = new List<MapPath>();
        }

        public Map(List<MapFloor> mapNodes, List<MapPath> mapPaths)
        {
            _mapNodes = mapNodes;
            _mapPaths = mapPaths;
        }

        #region Add or Delete
        // 맵 생성 시에는 아직 쓰지 않는 메서드. 맵 내의 층단위로 관리를 하기 때문.

        public void AddNode(int floor, MapNode target)
        {
            _mapNodes[floor].AddNode(target);
        }

        public void DeleteNode(int floor, MapNode target)
        {
            _mapNodes[floor].RemoveNode(target);
        }

        public void AddPath(MapPath path)
        {
            if (!_mapPaths.Contains(path))
                _mapPaths.Add(path);
        }

        public void DeletePath(MapPath path)
        {
            if (_mapPaths.Contains(path))
                _mapPaths.Remove(path);
        }
        #endregion


        /// <summary>
        /// 맵 형태 살펴보는 용도(디버깅 용도)
        /// </summary>
        public void DebugMap()
        {
            var debugMap = new StringBuilder("\n생성된 맵입니다. 생성 일시 : " + DateTime.Now.ToString("MM/dd/yyyy") + '\n');
            _mapNodes.Reverse();
            foreach(var mapfloor in _mapNodes)
            {
                var debugFloor = new StringBuilder(mapfloor.FloorNum.ToString() + '\t');
                int xInterval = 0;
                int xCurrent = 0;
            
                foreach(var node in mapfloor.FloorMembers)
                {
                    xInterval = node.GridPoint.x - xCurrent;
                    xCurrent = node.GridPoint.x;

                    if(xInterval == 0 && node.GridPoint.x != 0) continue;
                    debugFloor.Append('\t', xInterval).Append(node.NodeID.ToString());
                }            
                debugMap.AppendLine(debugFloor.ToString());
            }

            Debug.Log(debugMap.ToString());

            var debugString = new StringBuilder().AppendLine("맵에 사용된 패스는 이렇습니다");
            foreach (var path in _mapPaths)
            {
            
            }
        }

        public void ClearMap()
        {
            _mapNodes.Clear();
            _mapPaths.Clear();
        }
    }
}