using Core.Scripts.Foundation.Define;
using Core.Scripts.Foundation.Singleton;
using GamePlay.Common.Scripts.Entities.Character;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts
{
    public class BattleSession : SingletonObject<BattleSession>{
        public Party PlayerParty { get; private set; }
        public SystemEnum.Dungeon DungeonName { get; private set; }
        public int DungeonFloor {get; private set;}
        public List<string> StageNames { get; private set; }
        public int RandomStageCount { get; private set; }
        public SystemEnum.eScene ReturningScene { get; private set; }
        public bool IsEndExploreBattle { get; private set; }
        public int EncounterCellIndex { get; private set; } = -1;
        
        public int CurrentStageIndex { get; private set; }
        public string CurrentStageName =>
            (StageNames != null &&
             CurrentStageIndex >= 0 &&
             CurrentStageIndex < StageNames.Count)
                ? StageNames[CurrentStageIndex]
                : null;
        
        public int StageCount => StageNames?.Count ?? RandomStageCount;
        public bool HasAnyStage => StageCount > 0;
        public bool HasNextStage => HasAnyStage && CurrentStageIndex + 1 < StageCount;
        
        #region Tutorial Section

        public const int TutorialMaxCount = 3;
        public int CurrentTutorialIndex { get; private set; }
        public bool NeedTutorial => CurrentTutorialIndex < TutorialMaxCount;
        #endregion
        
        private BattleSession() { }

        public void SetBattleData(
            Party party,
            SystemEnum.Dungeon dungeon, 
            int dungeonFloor,
            List<string> stageNames = null,
            SystemEnum.eScene returningScene = SystemEnum.eScene.ExploreScene,
            bool isEndExploreBattle = false,
            int encounterCellIndex = -1)
        {
            PlayerParty = party;
            DungeonName = dungeon;
            DungeonFloor = dungeonFloor;
            StageNames ??= new List<string>();
            StageNames.Clear();
            if (stageNames != null)
                StageNames.AddRange(stageNames);
            
            ReturningScene = returningScene;
            CurrentStageIndex = 0;
            IsEndExploreBattle = isEndExploreBattle;
            EncounterCellIndex = encounterCellIndex;
            Debug.Log($"{party}");
        }
        
        public void UpdateParty(Party party) => PlayerParty = party;
        
        public bool MoveToNextStage()
        {
            if (!HasNextStage)
                return false;

            CurrentStageIndex++;
            //if(NeedTutorial)
            //    CurrentTutorialIndex++;
            return true;
        }
        
        public void MarkTutorialBattleCompleted()
        {
            if (CurrentTutorialIndex < TutorialMaxCount)
                CurrentTutorialIndex++;
        }
        
        
        public void Clear()
        {
            PlayerParty = null;
            DungeonName = default;
            DungeonFloor = 0;
            StageNames?.Clear();
            CurrentStageIndex = 0;
            ReturningScene = SystemEnum.eScene.ExploreScene;
            IsEndExploreBattle = false;
            EncounterCellIndex = -1;
        }
    }
}
