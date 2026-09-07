using System;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace GamePlay.Features.Battle.Scripts
{
    [Serializable]
    public class BattlePlatformDTO
    {
        public readonly int ID;
        public readonly int PlatformID;
        public readonly bool IsActive;
        public readonly Vector2Int GridPosition;

        public BattlePlatformDTO(
            int id,
            int platformID,
            bool isActive = true, 
            Vector2Int gridPosition = default)
        {
            ID = id;
            PlatformID = platformID;
            IsActive = isActive;
            GridPosition = gridPosition;
        }

        public Vector3 GetWorldPosition(Grid grid)
        {
            Vector3 worldPosition = grid.GetCellCenterWorld(new Vector3Int(GridPosition.x, GridPosition.y, 0));
            return new Vector3(worldPosition.x, worldPosition.y - grid.cellSize.y / 2, 0);
        }
    }
    
    [Serializable]
    public class BattleObjectDTO
    {
        public readonly int ID;
        public readonly int ObjectID;
        public readonly bool IsActive;
        public readonly Vector3 WorldPosition;

        public BattleObjectDTO(
            int id,
            int objectID,
            bool isActive,
            Vector3 worldPosition = default
        )
        {
            ID = id;
            ObjectID = objectID;
            IsActive = isActive;
            WorldPosition = worldPosition;
        }
    }
    
}