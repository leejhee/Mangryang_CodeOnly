using Modules.PathFinder.Define;
using Modules.PathFinder.Grid;
using Modules.PathFinder.Parameter;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.PathFinder.Service
{
    public static class AStarPathFinder
    {
        private const int STRAIGHT_COST = 10;
        private const int DIAGONAL_COST = 14;

        /// <summary> Astar 알고리즘 실행 메서드. 외부에서 파라미터 생성 후, 이 메서드로 실행 </summary>
        /// <param name="param">
        /// 실행 배경 그리드, 시작/최종 노드, 대각 이동 제한을 포함한다.
        /// 알고리즘을 사용할 객체에서 선언 후 적용할 것.
        /// </param>
        public static List<GridPoint> AStarPath(AStarParameter param)
        {
            #region INIT
            var openList =                  new Heap<Node>();
            var closedList =                new List<Node>();
            var grid =                      param.BaseGrid;
            var startNode =                 param.StartNode;
            var targetNode =                param.TargetNode;
            var constraint =                param.Constraint;
            #endregion

            openList.Add(startNode);

            while (openList.Count > 0)
            {
                var current = openList.Pop();
                if(current == targetNode)
                {
                    return ReconstructPath(current);
                }
                closedList.Add(current);

                var neighbors = grid.GetNeighbors(current, constraint);
                foreach (var neighbor in neighbors)
                {
                    if (closedList.Contains(neighbor)) continue;

                    int tentativeG = current.G + 
                                      (((Mathf.Abs(current.x - neighbor.x) == 1)&&(Mathf.Abs(current.y - neighbor.y) == 1)) ? 
                                          DIAGONAL_COST : STRAIGHT_COST);
                    if (!openList.Contains(neighbor) || tentativeG < neighbor.G)
                    {
                        neighbor.ParentNode = current;
                        neighbor.G = tentativeG;
                        neighbor.H = OctileHeuristic(current, targetNode);
                        if(!openList.Contains(neighbor)) 
                            openList.Add(neighbor);
                    }
                }
            }

            return null;
        }

        public static List<GridPoint> ReconstructPath(Node targetNode)
        {
            var FinalPoints = new List<GridPoint> { new GridPoint(targetNode.x, targetNode.y) };
            while (targetNode.ParentNode is not null)
            {
                targetNode = targetNode.ParentNode;
                FinalPoints.Add(new GridPoint(targetNode.x, targetNode.y));
            }
            FinalPoints.Reverse();
            return FinalPoints;
        }

        public static int OctileHeuristic(Node current, Node targetNode)
        {
            int x = Mathf.Abs(current.x - targetNode.x);
            int y = Mathf.Abs(current.y - targetNode.y);

            int diagonal = DIAGONAL_COST * Mathf.Max(x, y);
            int straight = STRAIGHT_COST * Mathf.Abs(x - y);

            return diagonal + straight;
        }

        //public static Node FindNearestWalkable(Node target)
        //{

        //}
    }
}