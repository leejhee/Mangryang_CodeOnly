using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Core.Scripts.Managers;
using GamePlay.Features.Explore.Scripts;
using System;
using UnityEngine.Serialization;

namespace GamePlay.Features.Battle.Scripts.UI.CharacterInfoPopup
{
    public class CharacterInfoPopupSkill : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image skillImage;
        [SerializeField] private Image frameImage;
        [SerializeField] private SkillDescription skillDescription;
        [SerializeField] private Sprite selectedFrame;
        [SerializeField] private Sprite nonSelectedFrame;
        [SerializeField] private Sprite inActiveFrame;
        [SerializeField] private Button skillButton;
        [SerializeField] private GameObject iconObject;
        private int _index;
        private bool _isSelected = false;
        private bool _isActivate = false;
        
        public Button SkillButton => skillButton;
        public event Action<int> Selected;
        public event Action<int> Deselected;
        
        
        public async void SetSkillImage(CharacterInfoPresenter.InfoPopupSkillResourceRoot skillResourceRoot, int idx)
        {
            _index = idx;
            Sprite icon = await ResourceManager.Instance.LoadAsync<Sprite>(skillResourceRoot.IconRoot);
            Sprite tooltip = await ResourceManager.Instance.LoadAsync<Sprite>(skillResourceRoot.TooltipRoot);

            _isActivate = true;
            iconObject.SetActive(true);
            skillImage.sprite = icon;
            skillDescription.SetSkillDescription(tooltip);

            _isSelected = skillResourceRoot.IsUsing;
            frameImage.sprite = _isSelected ? selectedFrame : nonSelectedFrame;

        }

        public void InactiveSkillImage()
        {
            iconObject.SetActive(false);
            frameImage.sprite = inActiveFrame;
            _isActivate = false;
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isActivate)
                skillDescription.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {            
            if (_isActivate)
                skillDescription.gameObject.SetActive(false);
        }

        public void SelectSkill()
        {
            // 스킬이 이미 선택되어 있을 경우 -> 스킬 해제
            if (_isSelected)
            {
                Deselected?.Invoke(_index);
                
                // 스킬 해제
                _isSelected = false;
                frameImage.sprite = nonSelectedFrame;
            }
            // 스킬이 미선택일 경우 -> 스킬 선택
            else
            {
                // 스킬 선택 이벤트 발사
                Selected?.Invoke(_index);
            }
        }

        public void SetSelectedSkill()
        {
            _isSelected = true;
            frameImage.sprite = selectedFrame;
        }
        
        public void ActivateInteractable(bool value)
        {
            skillButton.interactable = value;
        }
    }
}
