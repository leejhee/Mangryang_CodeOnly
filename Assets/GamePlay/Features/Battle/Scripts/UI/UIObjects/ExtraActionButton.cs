using System;
using UnityEngine;
using UnityEngine.UI;

namespace GamePlay.Features.Battle.Scripts.UI.UIObjects
{
    public class ExtraActionButton : ToggleButton
    {
        
        [SerializeField] private Button actionButton;
        public Button ActionButton => actionButton;

        public event Action Selected;
        public event Action UnSelected;
        
        private void Start()
        {
            isSelected = false;
        }
        

        public override void OnSelect()
        {
            Selected?.Invoke();
        }

        public override void OnDeselect()
        {
            // 여기서 버튼 해제 일단 해줌
            Debug.Log("추가행동 해제");
            //isSelected = false;
            UnSelected?.Invoke();
        }
    }
}
