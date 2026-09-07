using Core.Scripts.Foundation.Define;
using System;

namespace GamePlay.Features.Explore.Scripts.Map.Data
{
    [Serializable]
    public class SymbolConfigEntry
    {
        public SystemEnum.MapSymbolType symbolType;
        public int symbolCount;

        public SymbolConfigEntry(SystemEnum.MapSymbolType symbolType, int symbolCount)
        {
            this.symbolType = symbolType;
            this.symbolCount = symbolCount;
        }
    }
    
    [Serializable]
    public class EventConfigEntry
    {
        public SystemEnum.CellEventType eventType;
        public int probability;

        public EventConfigEntry(SystemEnum.CellEventType eventType, int probability)
        {
            this.eventType = eventType;
            this.probability = probability;
        }
    }
    
    [Serializable]
    public class ItemConfigEntry
    {
        public long itemIndex;
        public int dropProbability;

        public ItemConfigEntry(long itemIndex, int dropProbability)
        {
            this.itemIndex = itemIndex;
            this.dropProbability = dropProbability;
        }
    }
}