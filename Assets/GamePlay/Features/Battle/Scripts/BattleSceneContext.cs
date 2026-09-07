using Core.Scripts.Boot;
using Core.Scripts.Foundation.Define;
using GamePlay.Common.Scripts.Entities.Character;

namespace GamePlay.Features.Battle.Scripts
{
    public class BattleSceneContext : ISceneContext
    {
        public SystemEnum.eScene Scene => SystemEnum.eScene.BattleScene;
        
        public Party PlayerParty { get; }
        
        public SystemEnum.Dungeon Dungeon { get; }
        
        public string StageName { get; }

        public BattleSceneContext(Party playerParty, SystemEnum.Dungeon dungeon, string stageName)
        {
            PlayerParty = playerParty;
            Dungeon = dungeon;
            StageName = stageName;
        }
        
        /// <summary>
        /// BattleScene 안에서 즉각 테스트 시 사용. TestMap 및 던전 enum을 조작해서 테스트할 것.
        /// </summary>
        /// <returns></returns>
        public static BattleSceneContext CreateDebug()
        {
            var party = new Party();
            party.InitParty();
            return new BattleSceneContext(party, SystemEnum.Dungeon.MOUNTAIN_BACK, "TestMap");
        }
    }
    
    
}