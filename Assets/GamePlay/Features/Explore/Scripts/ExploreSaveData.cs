using Core.Scripts.Foundation.Define;
using GamePlay.Character;
using GamePlay.Common.Scripts.Entities.Character;
using GamePlay.Features.Explore.Scripts.Map.Logic;
using System;
using UnityEngine.Serialization;

namespace GamePlay.Explore
{
    [Serializable]
    public class ExploreSaveData
    {
        public SystemEnum.Dungeon dungeon;
        public Party playerParty;
        public ExploreMapSkeleton MapSkeletonData;
        
        public ExploreSaveData(
            Party playerParty,
            SystemEnum.Dungeon dungeon
            //TODO : 추후 인벤토리 넣어야 한다. 레벨스케일링 데이터도!
        )
        {
            this.dungeon = dungeon;
            this.playerParty = playerParty;
        }
        
        // TODO : 탐사에서의 저장 포인트를 알아올 것. 호출 타이밍을 정해야 한다.
        public void WriteMap(ExploreMapSkeleton mapSkeleton)
        {
            MapSkeletonData = mapSkeleton;
        }
    }
}