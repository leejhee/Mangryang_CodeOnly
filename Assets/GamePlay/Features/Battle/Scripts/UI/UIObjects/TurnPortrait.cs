using UnityEngine;
using UnityEngine.UI;

namespace GamePlay.Features.Battle.Scripts.UI.UIObjects
{
    public class TurnPortrait : MonoBehaviour
    {
        [SerializeField] private Image character;
        [SerializeField] private Image background;
        [SerializeField] private long charUID;
        [SerializeField] private Sprite  selectedFrame;
        [SerializeField] private Sprite nonSelectedFrame;
        public long CharUID => charUID;
        
        public void OnCharacterDie()
        {
            // 캐릭터 사망시 배경화면 어둡게 해주기
            Debug.Log("캐릭터 사망");
        }

        public void SetCurrentTurn(bool isTurn)
        {
            background.sprite = isTurn ? selectedFrame : nonSelectedFrame;
        }

        public void SetPortraitImage(Sprite sprite, long uid)
        {
            character.sprite = sprite;
            charUID = uid;
        }
    }
}
