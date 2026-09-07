namespace Modules.PathFinder.Define
{
    /// <summary> 대각 이동 제한 </summary>
    public enum DiagonalConstraint
    {
        None,
        Always,
        DontCrossCorner
    }

    /// <summary> 타겟 노드 자유도 제한 </summary>
    public enum MovableConstraint
    {
        None,
        OnlyWalkable,
        AllowWallClick
    }
}