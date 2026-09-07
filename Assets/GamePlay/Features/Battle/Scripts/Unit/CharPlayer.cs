using Core.Scripts.Foundation.Define;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Entities.Character;
using GamePlay.Features.Battle.Scripts.BattleAction;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GamePlay.Features.Battle.Scripts.Unit
{
    public class CharPlayer : CharBase, IPointerClickHandler
    {
        protected override SystemEnum.eCharType CharType => SystemEnum.eCharType.Player;
        public override async UniTask CharInit(CharacterModel charModel)
        {
            await base.CharInit(charModel);
            BattleCharManager.Instance.SetChar(this);
        }

        public async void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            if (eventData.clickCount != 2) return;
            
            Debug.Log($"Double Clicked - {name}");
            BattleController bc = BattleController.Instance;
            
            if (bc.FocusChar != this) return;
            if (bc.IsModal)
                bc.CancelPreview();
            else
                await bc.StartPreview(ActionType.Move);
        }
        
    }
}
