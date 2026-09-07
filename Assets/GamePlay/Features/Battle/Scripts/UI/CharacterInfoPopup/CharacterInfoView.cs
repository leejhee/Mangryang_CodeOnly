using Core.Scripts.Foundation.Define;
using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Entities.Character;
using GamePlay.Common.Scripts.Entities.Skills;
using GamePlay.Features.Explore.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UIs.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GamePlay.Features.Battle.Scripts.UI.CharacterInfoPopup
{
    public class CharacterInfoView : MonoBehaviour, IView
    {

        public GameObject Root { get; }
    
        [SerializeField] private PortraitPanel portraitPanel;
        [SerializeField] private PassivePanel passivePanel;
        [SerializeField] private SkillPanel skillPanel;
        [SerializeField] private StatPanel statPanel;
        [SerializeField] private EssencePanel essencePanel;
        public PortraitPanel PortraitPanel => portraitPanel;
        public PassivePanel PassivePanel => passivePanel;
        public SkillPanel SkillPanel => skillPanel;
        public StatPanel StatPanel => statPanel;
        public EssencePanel EssencePanel => essencePanel;
    
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;
        [SerializeField] private Button closeButton;
        public Button LeftButton => leftButton;
        public Button RightButton => rightButton;
        public Button CloseButton => closeButton;
        
        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        public UniTask PlayEnterAsync(CancellationToken ct) => UniTask.CompletedTask;
        public UniTask PlayExitAsync(CancellationToken ct) => UniTask.CompletedTask;
    }

    public class CharacterInfoPresenter : PresenterBase<CharacterInfoView>
    {
        public CharacterInfoPresenter(IView view) : base(view) { }
        private List<CharacterModel> partyMembers = new List<CharacterModel>();
        private CharacterModel currentCharacter;
        public override UniTask EnterAction(CancellationToken token)
        {
            #region Model To View
            
            
            
            if (GameManager.Instance.GameState == SystemEnum.GameState.Explore)
            {
                
                PreloadProcessInExplore();
            }
            else if (GameManager.Instance.GameState == SystemEnum.GameState.Battle)
            {
                PreloadProcessInBattle();
            }

            
            #endregion

            #region View To Model

            ViewEvents.Subscribe(
                act => View.LeftButton.onClick.AddListener(new UnityAction(act)),
                act => View.LeftButton.onClick.RemoveAllListeners(),
                OnClickLeftButton
            );
            ViewEvents.Subscribe(
                act => View.RightButton.onClick.AddListener(new UnityAction(act)),
                act => View.RightButton.onClick.RemoveAllListeners(),
                OnClickRightButton
            );
            ViewEvents.Subscribe(
                act => View.CloseButton.onClick.AddListener(new UnityAction(act)),
                act => View.CloseButton.onClick.RemoveAllListeners(),
                OnClickHideButton
            );
            #endregion
            
            return UniTask.CompletedTask;
        }

        private async void PreloadProcessInBattle()
        {

            // 파티원 LD 미리 다 로드해두기
            await View.PortraitPanel.PreloadPortraits(BattleController.Instance.PlayerParty.partyMembers);

            partyMembers = BattleController.Instance.PlayerParty.partyMembers;
            currentCharacter = BattleController.Instance.FocusChar.CharInfo;
            // 현재 포커스된 캐릭터 다른 정보칸 입력
            SetCharacterInfoPopup(currentCharacter);
        }

        private async void PreloadProcessInExplore()
        {
            try
            {
                await View.PortraitPanel.PreloadPortraits(ExploreManager.Instance.playerParty.partyMembers);

                partyMembers = ExploreManager.Instance.playerParty.partyMembers;
                currentCharacter = ExploreManager.Instance.SelectedCharacter;
                SetCharacterInfoPopup(currentCharacter);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }
        private readonly PresenterEventBag _presenterEventBag = new();

        public struct InfoPopupSkillResourceRoot
        {
            public string SkillName;
            public string IconRoot;
            public string TooltipRoot;
            public bool IsUsing;

            public InfoPopupSkillResourceRoot(string skillName, string iconRoot, string tooltipRoot, bool isUsing = false)
            {
                this.SkillName = skillName;
                this.IconRoot = iconRoot;
                this.TooltipRoot = tooltipRoot;
                this.IsUsing = isUsing;
            }
        }
        
        private void SetCharacterInfoPopup(CharacterModel model)
        {
            View.PortraitPanel.SetPortraitPanel(model);
            View.PassivePanel.SetPassivePanel(model);
            

            List<InfoPopupSkillResourceRoot> skillResourceRoots = model.ActiveSkills.Select(activeSkill => 
                new InfoPopupSkillResourceRoot(activeSkill.SkillName, activeSkill.Icon, activeSkill.TooltipName)).ToList();

            foreach (SkillModel usingSkill in model.UsingSkills)
            {
                for (int i = 0; i < skillResourceRoots.Count; i++)
                {
                    if (usingSkill.SkillName != skillResourceRoots[i].SkillName)
                    {
                        continue;
                    }

                    InfoPopupSkillResourceRoot temp = skillResourceRoots[i];
                    temp.IsUsing = true;
                    skillResourceRoots[i] = temp;
                }
            }



            foreach (CharacterInfoPopupSkill skillButton in View.SkillPanel.SkillList)
            {
                _presenterEventBag.Subscribe<int>(act =>
                    {
                        skillButton.Selected -= act;
                        skillButton.Selected += act;
                    },
                    act => skillButton.Selected -= act,
                    OnSkillSelect
                );
                
                _presenterEventBag.Subscribe<int>(act =>
                    {
                        skillButton.Deselected -= act;
                        skillButton.Deselected += act;
                    },
                    act => skillButton.Deselected -= act,
                    OnSkillDeselect
                );
            }
            
            View.SkillPanel.SetSkills(skillResourceRoots);
            View.StatPanel.SetStats(model);
            View.EssencePanel.SetEssence(model);
        }
        private void SetCharacterInfoPopup()
        {
            SetCharacterInfoPopup(currentCharacter);
        }


        private void OnSkillSelect(int idx)
        {
            // 스킬 선택 되었는지 물어보기
            if (currentCharacter.UseSkill(currentCharacter.ActiveSkills[idx]))
            {
                // 되었으면 해당하는 버튼 설정해줌
                Debug.Log($"{idx}번째 스킬 선택");
                View.SkillPanel.SkillList[idx].SetSelectedSkill();
            }
        }

        private void OnSkillDeselect(int idx)
        {
            Debug.Log($"{idx}번째 스킬 해제");
            
            currentCharacter.NotUseSkill(currentCharacter.ActiveSkills[idx]);
        }

        private long GetSkillIDFromIndex(int idx)
        {
            return currentCharacter.ActiveSkills[idx].SkillIndex;
        }
        private void OnClickLeftButton()
        {
            Debug.Log("왼쪽 버튼 클릭");
            int nextIndex = partyMembers.IndexOf(currentCharacter) - 1;
            if (nextIndex < 0)
            {
                nextIndex += partyMembers.Count;
            }

            currentCharacter = partyMembers[nextIndex];
            SetCharacterInfoPopup();
        }

        private void OnClickRightButton()
        {
            Debug.Log("오른쪽 버튼 클릭");
            int nextIndex = partyMembers.IndexOf(currentCharacter) + 1;
            
            // if -> while 도 가능
            if (nextIndex >= partyMembers.Count)
            {
                nextIndex -= partyMembers.Count;
            }
            currentCharacter = partyMembers[nextIndex];
            SetCharacterInfoPopup();
        }
        
        // 팝업창 닫을때
        private void OnClickHideButton()
        {
            View.PortraitPanel.ReleaseAllPortraits();
            //View.Hide();
            UIManager.Instance.HideTopViewAsync().Forget();
        }
    }
}