using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.Models
{
    public class PopupKeywordModel
    {
        public List<BattleKeyword> Keywords;
        public PopupKeywordModel(List<BattleKeyword>  keywords)
        {
            Keywords = keywords;
        }
    }
}