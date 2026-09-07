using System;
using UnityEngine;
using UnityEngine.UI;

namespace GamePlay.Features.Battle.Scripts.UI.UIObjects.Reward
{
    public class SkillRewardObject : ToggleButton
    {
        //[SerializeField] private TMP_Text rewardText;
        [SerializeField] private Button interactionButton;

        [SerializeField] private int slotIndex;

        public Button InterActionButton => interactionButton;
        
        public event Action<int> Selected;
        public event Action<int> Deselected;
        
        public void SetReward(int idx, Sprite deselected = null, Sprite selected = null)
        {
            GameObject buttonObject = transform.GetChild(0).gameObject;
            frame = buttonObject.GetComponent<Image>();
            interactionButton =  buttonObject.GetComponent<Button>();
            selectable = true;
            isSelected = false;
            slotIndex = idx;
            selectedFrame =  selected;
            nonSelectedFrame = deselected;
            
            
            Frame.sprite = deselected;
        }


        public override void OnSelect()
        {
            Selected?.Invoke(slotIndex);
        }

        public override void OnDeselect()
        {
            Deselected?.Invoke(slotIndex);
        }
    }
}