using GamePlay.Features.Battle.Scripts.Unit;
using System;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.BattleMap
{
    [Serializable]
    public class CoverEntry
    {
        public Vector2Int cell;
        public FieldCover cover;
    }
    
    [Serializable]
    public class ObstacleEntry
    {
        public Vector2Int cell;
        public FieldObstacle obstacle;
    }
}