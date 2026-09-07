using Modules.PathFinder.Define;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Modules.PathFinder.Grid
{
    /// <summary>
    /// 알고리즘 내부의 Grid 상에서만 사용.
    /// 반환은 해당 그리드의 좌표로만 할거임.
    /// </summary>
    [Serializable]
    public class Node : IComparable<Node>
    {
        public Node(int _x, int _y) { x = _x; y = _y; }
    
        public Node ParentNode;
    
        public bool walkable = false;
        public int x;
        public int y;
        public int G = 0;
        public int H;
        public int F { get { return G + H; } }

        public int CompareTo(Node otherNode)
        {
            if (F == otherNode.F)
                return H - otherNode.H;
            return F - otherNode.F;
        }

        #region Operator Overloading
        public static bool operator ==(Node node1, Node node2)
        {
            if (ReferenceEquals(node1, node2)) return true;
            if (node1 is null || node2 is null) return false;
            return node1.x == node2.x && node1.y == node2.y;
        }

        public static bool operator !=(Node node1, Node node2)
        {
            return !(node1 == node2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            Node other = (Node)obj;
            return this.x == other.x && this.y == other.y;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y);
        }
        #endregion
    }

    /// <summary>
    /// Grid 내의 좌표로 활용. 인덱싱 용도로 활용 예정
    /// </summary>
    [Serializable]
    public class GridPoint: IEquatable<GridPoint>
    {
        public int x, y;

        public GridPoint() { x = 0; y = 0; }
    
        public GridPoint(int x, int y) { this.x = x; this.y = y; }

        public GridPoint SetPoint(int x, int y) { this.x = x; this.y = y; return this; }

        #region Operator Overloading
        public override bool Equals(System.Object o)
        {
            GridPoint p = (GridPoint)o;
            if (ReferenceEquals(null, p)) return false;

            return x == p.x && y == p.y;
        }

        public bool Equals(GridPoint other)
        {
            return x == other.x && y == other.y;
        }

        public override int GetHashCode() { return x ^ y; }

        public static bool operator==(GridPoint p1, GridPoint p2)
        {
            if(ReferenceEquals(null, p1)) return false;
            if(ReferenceEquals(p2, null)) return false;
            return p1.x == p2.x && p1.y == p2.y;
        }

        public static bool operator!=(GridPoint p1, GridPoint p2)
        {
            return !(p1 == p2);
        }
        #endregion
    }

    /// <summary>
    /// Grid 내의 경계가 되는 x, y값 배정
    /// </summary>
    public class GridFrame
    {
        public int LBX, LBY, RTX, RTY;

        public GridFrame() { LBX = 0; LBY = 0; RTX = 0; RTY = 0; }
        public GridFrame(int lbx, int lby, int rtx, int rty)
        {
            LBX = lbx; LBY = lby; RTX = rtx; RTY = rty;
        }

        public void SetGridFrame(int lbx, int lby, int rtx, int rty)
        {
            LBX = lbx; LBY = lby; RTX = rtx; RTY = rty;
        }
    }

    public abstract class GridBase
    {
    }


    /// <summary>
    /// Static한 배열을 사용하기로 함.
    /// 현재 static으로 맵 상에 존재하게 하여, 모든 객체가 상태 공유할 수 있게 함.
    /// </summary>
    public class AStarGrid : GridBase
    {
        GridFrame frame;
        public GridFrame Frame { get { return frame; } }

        static Node[][] mapNodes; //지도의 노드는 모든 유닛이 공유할 수 있도록 한다.

        public AStarGrid()
        {
            frame = new GridFrame();
        }

        public AStarGrid(int maxX, int maxY)
        {
            if (frame != null) frame.SetGridFrame(0, 0, maxX, maxY);
            else { frame = new GridFrame(0, 0, maxX, maxY); }
            InitAStarGrid(frame);
        }
    
        private bool InsideGrid(int x, int y)
        {
            return x >= frame.LBX && x <= frame.RTX && y <= frame.RTY && y >= frame.LBY;
        }

        private void InitAStarGrid(GridFrame frame)
        {
            mapNodes = new Node[frame.RTX + 1][];
            for (int i = frame.LBX; i < frame.RTX + 1; i++)
            { 
                mapNodes[i] = new Node[frame.RTY + 1];
                for (int j = frame.LBY; j < frame.RTY + 1; j++)
                {
                    mapNodes[i][j] = new Node(i, j);
                    if (Physics2D.OverlapPoint(new Vector2Int(i, j), LayerMask.GetMask("Walkable")))
                        mapNodes[i][j].walkable = true;
                }
            }
            
                
        }

        public List<Node> GetNeighbors(Node current, DiagonalConstraint constraint)
        {
            List<Node> neighbors = new List<Node>();

            int x = current.x;
            int y = current.y;
            GridPoint checkPos = new GridPoint(x, y);

            bool downPass = false, upPass = false, leftPass = false, rightPass = false;

            #region Check Straight
            if (IsWalkable(checkPos.SetPoint(x, y - 1)))
            {
                downPass = true; neighbors.Add(GetNode(checkPos));
            }
            if (IsWalkable(checkPos.SetPoint(x, y + 1)))
            {
                upPass = true; neighbors.Add(GetNode(checkPos));
            }
            if (IsWalkable(checkPos.SetPoint(x - 1, y)))
            {
                leftPass = true; neighbors.Add(GetNode(checkPos));
            }
            if (IsWalkable(checkPos.SetPoint(x + 1, y)))
            {
                rightPass = true; neighbors.Add(GetNode(checkPos));
            }
            #endregion

            #region Check Diagonal
            bool pass1 = false, pass3 = false, pass7 = false, pass9 = false;
            if (constraint == DiagonalConstraint.Always)
            {
                pass1 = true; pass3 = true; pass7 = true; pass9 = true;
            }
            else if(constraint == DiagonalConstraint.DontCrossCorner)
            {
                pass1 = downPass && leftPass;
                pass3 = downPass && rightPass;
                pass7 = upPass && leftPass;
                pass9 = upPass && rightPass;
            }

            if (pass1 && IsWalkable(checkPos.SetPoint(x - 1, y - 1)))
            {
                neighbors.Add(GetNode(checkPos));
            }
            if (pass3 && IsWalkable(checkPos.SetPoint(x + 1, y - 1)))
            {
                neighbors.Add(GetNode(checkPos));
            }
            if (pass7 && IsWalkable(checkPos.SetPoint(x - 1, y + 1)))
            {
                neighbors.Add(GetNode(checkPos));
            }
            if (pass9 && IsWalkable(checkPos.SetPoint(x + 1, y + 1)))
            {
                neighbors.Add(GetNode(checkPos));
            }

            #endregion

            return neighbors;
        }
    
        public Node GetNode(GridPoint point)
        {
            return GetNode(point.x, point.y);
        }

        public Node GetNode(int x, int y)
        {
            if (InsideGrid(x, y)) return mapNodes[x][y];
            return null;
        }

        public bool IsWalkable(GridPoint point)
        {       
            return InsideGrid(point.x, point.y) && mapNodes[point.x][point.y].walkable;
        }

        public void SetIsWalkable(GridPoint point, bool set)
        {
            Node checkNode = GetNode(point);
            checkNode.walkable = set;
        }

    }

    public class GridWDic : GridBase
    {
        GridFrame frame = null;
        static Dictionary<GridPoint, Node> mapNodes;

        public GridWDic(GridFrame f, List<GridPoint> walkablePointsOnly)
        {
            frame.LBX = f.LBX;
            frame.RTY = f.RTY;
            frame.LBY = f.LBY;
            frame.RTX = f.RTX;

        }


    }
}