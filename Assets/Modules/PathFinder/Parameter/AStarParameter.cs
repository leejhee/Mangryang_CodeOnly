using Modules.PathFinder.Define;
using Modules.PathFinder.Grid;

namespace Modules.PathFinder.Parameter
{
    /// <summary> AStar 알고리즘 실행 시 필요한 정보. 외부에서 만들어서 전달할 것. </summary>
    public class AStarParameter
    {
        /// <summary>알고리즘이 작동할 Grid </summary>
        public readonly AStarGrid        BaseGrid;
        /// <summary>시작 노드</summary>
        public readonly Node             StartNode;
        /// <summary>목표 노드</summary>
        public readonly Node             TargetNode;
        /// <summary>대각 이동시 제약</summary>
        public readonly DiagonalConstraint   Constraint;
        
        public AStarParameter(AStarGrid pGrid, Node pStart, Node pTarget, DiagonalConstraint constraint)
        {
            BaseGrid = pGrid;
            StartNode = pStart;
            TargetNode = pTarget;
            Constraint = constraint;
        }
    }
}