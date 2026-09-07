using Core.Scripts.Foundation.Define;
using GamePlay.Common.Scripts.Entities.Character;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GamePlay.Features.Battle.Scripts.UI.CharacterInfoPopup
{
    public class StatPanel : MonoBehaviour
    {
        [SerializeField] private List<CharacterInfoPopupStat> stats = new();

        public void SetStats(CharacterModel model)
        {
            foreach (CharacterInfoPopupStat stat in stats)
            {
                stat.SetStatText(model);
            }
        }
    }
}
