using Core.Scripts.Foundation.Define;
using GamePlay.Common.Scripts.Entities.Character;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts
{
    public struct CharBattleParameter
    {
        public Vector3 GeneratePos;
        public CharacterModel model;

        public CharBattleParameter(CharacterModel model, Vector3 pos)
        {
            this.model = model;
            GeneratePos = pos;
        }
    }
    
    public struct CharParameter
    {
        public SystemEnum.eScene Scene;
        public Vector3 GeneratePos;
        public long CharIndex;

        public CharParameter(SystemEnum.eScene scene, Vector3 Pos, long index)
        {
            Scene = scene;
            GeneratePos = Pos;
            CharIndex = index;
        }
    }
}