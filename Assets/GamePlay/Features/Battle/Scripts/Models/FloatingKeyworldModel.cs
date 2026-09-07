using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.Models
{
    public enum BattleKeyword
    {
        None,
    }

    public class FloatingKeywordModel
    {
        public long UID;
        public BattleKeyword Keyword;
        public Sprite Icon;
        public int Count;
        public int Amount;

        public FloatingKeywordModel(long uid, BattleKeyword keyword ,Sprite icon, int count, int amount)
        {
            UID = uid;
            Keyword = keyword;
            Icon = icon;
            Count = count;
            Amount = amount;
            
        }

        public FloatingKeywordModel(long uid, BattleKeyword keyword ,int count, int amount)
        {
            UID = uid;
            Keyword = keyword;
            Count = count;
            Amount = amount;
        }
    }
}
