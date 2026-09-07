using Core.Scripts.Foundation.Define;
using GamePlay.Common.Scripts.Entities.Character;
using TMPro;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.UI.CharacterInfoPopup
{
    public class CharacterInfoPopupStat : MonoBehaviour
    {
        [SerializeField] private TMP_Text statText;
        [SerializeField] private SystemEnum.eStats statType;
        
        public void SetStatText(CharacterModel model)
        {
            statText.text = $"{model.BaseStat.GetStat(statType)}";
        }
    }
}
