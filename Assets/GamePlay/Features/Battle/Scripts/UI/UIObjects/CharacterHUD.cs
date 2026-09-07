using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamePlay.Features.Battle.Scripts.UI.UIObjects
{
    public abstract class ToggleButton : MonoBehaviour
    {
        public bool selectable;
        public bool isSelected;
        [SerializeField] protected Image frame;
        [SerializeField] protected Sprite selectedFrame;
        [SerializeField] protected Sprite nonSelectedFrame;

        public Image Frame => frame;
        public Sprite SelectedFrame => selectedFrame;
        public Sprite NonSelectedFrame => nonSelectedFrame;

        public void SetSelectable(bool value)
        {
            selectable = value;
            if (!value)
                SetSelectedWithoutNotify(false);
        }

        public void SetSelectedWithoutNotify(bool value)
        {
            isSelected = value;
            if (frame)
                frame.sprite = value ? selectedFrame : nonSelectedFrame;
        }

        public abstract void OnSelect();
        public abstract void OnDeselect();
    }

    public class CharacterHUD : MonoBehaviour
    {
        [SerializeField] private Image characterPortraitImage;
        [SerializeField] private TMP_Text characterName;
        [SerializeField] private SkillButtonPanel skillPanel;
        [SerializeField] private CharacterPortrait characterPortrait;

        [Header("Bars")]
        [SerializeField] private Image hpBarFill;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Image actionBarFill;
        [SerializeField] private TMP_Text actionText;
        [SerializeField] private Slider actionSlider;

        [Header("Extra Actions")]
        [SerializeField] private ExtraActionButton jumpButton;
        [SerializeField] private ExtraActionButton pushButton;
        [SerializeField] private ExtraActionButton invenButton;
        [SerializeField] private List<ToggleButton> buttons = new();

        private int _hpAnimationGeneration;
        private int _apAnimationGeneration;

        public ExtraActionButton JumpButton => jumpButton;
        public ExtraActionButton PushButton => pushButton;

        public event Action CharacterInfoRequested;
        public event Action<int> SkillSelected;
        public event Action<int> SkillDeselected;
        public event Action JumpSelected;
        public event Action JumpDeselected;
        public event Action PushSelected;
        public event Action PushDeselected;

        [Serializable]
        public readonly struct SkillInfo
        {
            public Sprite SkillIcon { get; }
            public Sprite SkillDescription { get; }

            public SkillInfo(Sprite icon, Sprite description)
            {
                SkillIcon = icon;
                SkillDescription = description;
            }
        }

        private void Awake()
        {
            foreach (ToggleButton toggle in buttons)
            {
                ToggleButton captured = toggle;
                captured.GetComponent<Button>().onClick.AddListener(() => Toggling(captured));
            }

            characterPortrait.CharacterInfoPopup += HandleCharacterInfoRequested;
            foreach (SkillButton skillButton in skillPanel.SkillButtons)
            {
                skillButton.Selected += HandleSkillSelected;
                skillButton.Deselected += HandleSkillDeselected;
            }

            jumpButton.Selected += HandleJumpSelected;
            jumpButton.UnSelected += HandleJumpDeselected;
            pushButton.Selected += HandlePushSelected;
            pushButton.UnSelected += HandlePushDeselected;
        }

        private void OnDestroy()
        {
            characterPortrait.CharacterInfoPopup -= HandleCharacterInfoRequested;
            foreach (SkillButton skillButton in skillPanel.SkillButtons)
            {
                skillButton.Selected -= HandleSkillSelected;
                skillButton.Deselected -= HandleSkillDeselected;
            }

            jumpButton.Selected -= HandleJumpSelected;
            jumpButton.UnSelected -= HandleJumpDeselected;
            pushButton.Selected -= HandlePushSelected;
            pushButton.UnSelected -= HandlePushDeselected;
        }

        public void Toggling(ToggleButton selectedButton)
        {
            if (selectedButton.isSelected)
            {
                selectedButton.SetSelectedWithoutNotify(false);
                selectedButton.OnDeselect();
                return;
            }

            if (!selectedButton.selectable)
                return;

            foreach (ToggleButton button in buttons)
            {
                if (!button.isSelected)
                    continue;

                button.SetSelectedWithoutNotify(false);
                button.OnDeselect();
            }

            selectedButton.SetSelectedWithoutNotify(true);
            selectedButton.OnSelect();
        }

        public void ClearSelection()
        {
            foreach (ToggleButton button in buttons)
                button.SetSelectedWithoutNotify(false);
        }

        public void ShowCharacterHUD(
            string charName,
            long curHp,
            long maxHp,
            long curAp,
            long maxAp,
            Sprite portrait = null)
        {
            characterPortraitImage.sprite = portrait;
            characterName.text = charName;
            hpText.text = $"{curHp}/{maxHp}";
            hpSlider.maxValue = maxHp;
            hpSlider.value = curHp;
            actionText.text = $"{curAp}/{maxAp}";
            actionSlider.maxValue = maxAp;
            actionSlider.value = curAp;
        }

        public void SetSkillButtons(IReadOnlyList<SkillInfo> skills)
        {
            IReadOnlyList<SkillButton> skillButtons = skillPanel.SkillButtons;
            for (int i = 0; i < skillButtons.Count; i++)
            {
                SkillButton button = skillButtons[i];
                button.BindSlot(i);
                if (i < skills.Count)
                {
                    SkillInfo skill = skills[i];
                    button.SetButton(skill);
                }
                else
                {
                    button.InactiveButton();
                }
            }
        }

        public void SetHp(long result)
        {
            AnimateBarTo(result, hpSlider, hpText, true, ++_hpAnimationGeneration).Forget();
        }

        public void SetAp(long result)
        {
            AnimateBarTo(result, actionSlider, actionText, false, ++_apAnimationGeneration).Forget();
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        public void SetSkillInteractable(bool enabled)
        {
            if (!skillPanel || skillPanel.SkillButtons.Count == 0)
                return;

            skillPanel.SetInteractable(enabled);
        }

        public void SetExtraInteractable(bool enabled)
        {
            jumpButton.SetSelectable(enabled);
            pushButton.SetSelectable(enabled);
            invenButton.SetSelectable(enabled);
        }

        private async UniTask AnimateBarTo(
            long target,
            Slider slider,
            TMP_Text text,
            bool isHp,
            int generation)
        {
            const float duration = 0.5f;
            float elapsed = 0f;
            float startValue = slider.value;
            while (elapsed < duration)
            {
                if (!this || generation != (isHp ? _hpAnimationGeneration : _apAnimationGeneration))
                    return;

                elapsed += Time.deltaTime;
                float current = Mathf.Lerp(startValue, target, elapsed / duration);
                slider.value = current;
                text.text = $"{(int)current}/{(int)slider.maxValue}";
                await UniTask.Yield();
            }

            if (!this || generation != (isHp ? _hpAnimationGeneration : _apAnimationGeneration))
                return;

            slider.value = target;
            text.text = $"{target}/{(int)slider.maxValue}";
        }

        private void HandleCharacterInfoRequested() => CharacterInfoRequested?.Invoke();
        private void HandleSkillSelected(int slot) => SkillSelected?.Invoke(slot);
        private void HandleSkillDeselected(int slot) => SkillDeselected?.Invoke(slot);
        private void HandleJumpSelected() => JumpSelected?.Invoke();
        private void HandleJumpDeselected() => JumpDeselected?.Invoke();
        private void HandlePushSelected() => PushSelected?.Invoke();
        private void HandlePushDeselected() => PushDeselected?.Invoke();
    }
}
