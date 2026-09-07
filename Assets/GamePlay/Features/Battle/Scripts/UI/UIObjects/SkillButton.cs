using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GamePlay.Features.Battle.Scripts.UI.UIObjects
{
    public class SkillButton : ToggleButton, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text skillName;
        [SerializeField] private SkillDescription skillDescription;

        [SerializeField] private Sprite inactiveFrame;
        // 어떻게 하든 상관은 없을거같음.
        public int SlotIndex { get; private set; }
        public void BindSlot(int idx) => SlotIndex = idx;
        public event Action<int> Selected;
        public event Action<int> Deselected;

        private bool isActivate = false;
        
        private void Start()
        {
            isSelected = false;
        }
        
        public void SetButton(CharacterHUD.SkillInfo info)
        {
            this.GetComponent<Image>().sprite = nonSelectedFrame;
            
            icon.gameObject.SetActive(true);
            icon.sprite = info.SkillIcon;
            icon.color = Color.white;
            
            this.GetComponent<Button>().interactable = true;
            selectable = true;
            isActivate = true;
            skillDescription.SetSkillDescription(info.SkillDescription);
        }

        public void InactiveButton()
        {
            icon.gameObject.SetActive(false);
            this.GetComponent<Button>().interactable = false;
            selectable = false;
            isActivate = false;
            this.GetComponent<Image>().sprite = inactiveFrame;
            
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isActivate)
                skillDescription.gameObject.SetActive(true);
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            if (isActivate)
                skillDescription.gameObject.SetActive(false);
        }
        
        public override void OnSelect()
        {
            Debug.Log($"스킬 선택 - {skillName}");
            Selected?.Invoke(SlotIndex);
        }

        public override void OnDeselect()
        {
            Debug.Log("스킬 해제");
            Deselected?.Invoke(SlotIndex);
        }
    }
}

