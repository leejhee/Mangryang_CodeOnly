using Modules.PathFinder.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.PathFinder.Define
{
    /// <summary>
    /// 추상 좌표에서의 결과 변환. 
    /// 좌표계를 어떻게 설정하냐에 따라, 그리드 포인트를 매개로 하여 변환해주는 메서드를 여기에 추가해준다. 
    /// </summary>
    public static class PointConverter
    {
        public static List<Vector2Int> ToVector2Int(List<GridPoint> points)
        {
            if(points is null) return null;
            var result = new List<Vector2Int>();
            foreach(var point in points)
            {
                result.Add(new Vector2Int(point.x, point.y));
            }
            return result;
        }

        public static List<Vector2> ToIsoVec2(List<GridPoint> points, GridPoint pivot)
        {
            return null;
        }
    }
}