using Core.Scripts.Foundation.Define;
using System;

namespace Core.Scripts.GameSave
{
    [Serializable]
    public class NarrativeStatData
    {
        private int[] _narrativeStats;

        public NarrativeStatData()
        {
            _narrativeStats = new int[(int)SystemEnum.NarrativeStatType.Length];
            for (int i = 0; i < _narrativeStats.Length; i++)
            {
                _narrativeStats[i] = 5;
            }
        }
        
    }
}