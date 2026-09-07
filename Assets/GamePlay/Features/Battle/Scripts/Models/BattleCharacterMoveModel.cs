using UnityEngine;
namespace GamePlay.Features.Battle.Scripts.Models
{
    public class BattleCharacterMoveModel
    {
        public long UID;
        public Vector2 Position;
        
        public BattleCharacterMoveModel(long uid, Vector2 position)
        {
            UID = uid;
            Position = position;
        }

    }
}
