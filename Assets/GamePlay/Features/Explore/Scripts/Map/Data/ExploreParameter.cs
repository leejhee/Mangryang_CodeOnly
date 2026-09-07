using Core.Scripts.Foundation.Define;
using GamePlay.Common.Scripts.Entities.Character;

namespace GamePlay.Features.Explore.Scripts.Map.Data
{
    /// <summary>
    /// 탐사를 시작시키기 위한 파라미터
    /// </summary>
    public class ExploreParameter
    {
        public Party PlayerParty;
        public SystemEnum.Dungeon DungeonName;
        public int FloorNum;

        public ExploreParameter(Party playerParty, SystemEnum.Dungeon dungeonName, int FloorNum)
        {
            PlayerParty = playerParty;
            this.DungeonName = dungeonName;
            this.FloorNum = FloorNum;
        }
    }
}