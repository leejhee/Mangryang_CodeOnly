using Core.Scripts.Foundation.Define;
using Core.Scripts.Foundation.Singleton;
using GamePlay.Common.Scripts.Entities.Character;
using GamePlay.Features.Explore.Scripts.Map.Logic;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Features.Explore.Scripts
{
    /// <summary>
    /// 씬 전환 시 탐사 초기화에 필요한 정보를 전달하는 싱글턴 페이로드
    /// </summary>
    public class ExploreSession : SingletonObject<ExploreSession>
    {
        private ExploreSession() { }
        
        public SystemEnum.Dungeon TargetDungeon { get; private set; }
        public int TargetFloor { get; private set; }
        public Party PlayerParty { get; private set; }
        public bool IsNewExplore { get; private set; }
        
        //전투 같은 곳에서 돌아오는 경우 skeleton과 그 맵에서의 본래 위치를 기억해야한다.
        public Vector3 PlayerRecentPosition {get; private set;}
        public ExploreMapSkeleton CurrentSkeleton { get; private set; }
        public ulong RandomSeed { get; private set; }
        
        public List<long> ClearedSymbol { get; private set; }
        
        /// <summary>
        /// 새 탐사 시작 
        /// </summary>
        public void SetNewExplore(SystemEnum.Dungeon dungeon, int floor, Party party)
        {
            TargetDungeon = dungeon;
            TargetFloor = floor;
            PlayerParty = party;
            IsNewExplore = true;
            PlayerRecentPosition = Vector3.zero;
            
            CurrentSkeleton = null;
            ClearedSymbol = new List<long>();
        }

        /// <summary>
        /// 탐사 스켈레톤 생성 이후 세션에 저장(떠났다가 돌아와서 탐사하는 경우도 있으므로)  
        /// </summary>
        public void SetCurrentSkeleton(ExploreMapSkeleton skeleton, ulong seed)
        {
            CurrentSkeleton = skeleton;
            RandomSeed = seed;
        } 
        
        /// <summary>
        /// 탐사 이어하기
        /// </summary>
        public void SetContinueExplore(SystemEnum.Dungeon dungeon, int floor, Party party, Vector3 playerRecentPosition)
        {
            TargetDungeon = dungeon;
            TargetFloor = floor;
            PlayerParty = party;
            IsNewExplore = false;
            PlayerRecentPosition = playerRecentPosition;
        }
    
        public void AddClearedSymbol(long cellIndex)
        {
            if (ClearedSymbol == null)
                ClearedSymbol = new List<long>();

            if (!ClearedSymbol.Contains(cellIndex))
            {
                ClearedSymbol.Add(cellIndex);
                Debug.Log($"[ExploreSession] Cleared symbol at {cellIndex}");
            }
        }
        
        public bool IsSymbolCleared(long cellIndex)
        {
            return ClearedSymbol?.Contains(cellIndex) == true;
        }
        
        /// <summary>
        /// 탐사 종료 및 필요 없을 때 날리는 용도
        /// </summary>
        public void Clear()
        {
            TargetDungeon = SystemEnum.Dungeon.None;
            TargetFloor = 0;
            PlayerParty = null;
            IsNewExplore = false;
            PlayerRecentPosition = Vector3.zero;
            CurrentSkeleton = null;
            ClearedSymbol?.Clear();

        }
    }
}
