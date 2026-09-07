using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Core.Scripts.GameSave
{
    [Serializable]
    public class SlotProgressData
    {
        [Flags]
        public enum TutorialFlag
        {
            None = 0,
            MapControlGuide = 1,
            GatherGuide = 1 << 1,
            SymbolGuide = 1 << 2,
            BattleGuide1 = 1 << 3,
            BattleGuide2 = 1 << 4,
            
            All = MapControlGuide | 
                  GatherGuide | 
                  SymbolGuide | 
                  BattleGuide1 | BattleGuide2
        }

        [Flags]
        public enum MainStreamFlag
        {
            BackMountainBossCleared = 1,
        }
        
        public TutorialFlag tutorialProgress = TutorialFlag.None;
        public MainStreamFlag mainStreamProgress = MainStreamFlag.BackMountainBossCleared;
        [JsonProperty]
        public Dictionary<long, int> characterRelationship = new();

        #region Tutorial Part
        public bool TutorialDone => (tutorialProgress & TutorialFlag.All) == TutorialFlag.All;
        
        public bool HasAll(TutorialFlag flags) => (tutorialProgress & flags) == flags;
        public bool HasAny(TutorialFlag flags) => (tutorialProgress & flags) != 0;
        public void Set(TutorialFlag flags)    => tutorialProgress |= flags;
        public void Clear(TutorialFlag flags)  => tutorialProgress &= ~flags;
        #endregion
        
    }
}