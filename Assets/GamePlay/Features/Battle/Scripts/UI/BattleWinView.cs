using Core.Scripts.Foundation.Define;
using Core.Scripts.Foundation.SceneUtil;
using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Entities.Skills;
using GamePlay.Features.Battle.Scripts;
using GamePlay.Features.Battle.Scripts.UI.UIObjects;
using GamePlay.Features.Battle.Scripts.UI.UIObjects.Reward;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UIs.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using ResourceManager = Core.Scripts.Managers.ResourceManager;

namespace AngelBeat.UI
{
    
    public class BattleWinView : MonoBehaviour, IView
    {



        [SerializeField] private GameObject rewardTypeObject;
        [SerializeField] private GameObject skillRewardUIPrefab;
        [SerializeField] private List<RewardTable> rewardList;
        [SerializeField] private Transform skillRewardPanel;
        [SerializeField] private Transform skillRewardObjectParent;
        [SerializeField] private Transform rewardPanel;
        [SerializeField] private Transform rewardObjectParent;
        [SerializeField] private Button rewardSkipButton;
        [SerializeField] private Button getSkillButton;
        
        
        [SerializeField] private List<ToggleButton> buttons = new List<ToggleButton>();

        public void AddToggleButton(ToggleButton toggleButton)
        {
            if (toggleButton is SkillRewardObject reward)
            {
                reward.InterActionButton.onClick.AddListener(()=> Toggling(reward));
            }
            buttons.Add(toggleButton);
        }
        public void Toggling(ToggleButton selectedButton)
        {
            // 이미 선택된 버튼을 클릭시
            if (selectedButton.isSelected)
            {
                selectedButton.isSelected = false;
                selectedButton.Frame.sprite = selectedButton.NonSelectedFrame;
                selectedButton.OnDeselect();
            }
            else
            {
                foreach (var button in buttons)
                {
                    if (button.isSelected)
                    {
                        button.isSelected = false;
                        button.Frame.sprite = button.NonSelectedFrame;
                        button.OnDeselect();
                    }

                }
                selectedButton.isSelected = true;
                selectedButton.Frame.sprite = selectedButton.SelectedFrame;
                selectedButton.OnSelect();
            }
        }
        
        
        public Button RewardSkipButton => rewardSkipButton;
        public Button GetSkillButton => getSkillButton;
        public List<RewardTable>  RewardList => rewardList;
        public GameObject RewardTypeObject => rewardTypeObject;
        public GameObject SkillRewardUIPrefab => skillRewardUIPrefab;
        public Transform SkillRewardPanel => skillRewardPanel;
        public Transform SkillRewardObjectParent => skillRewardObjectParent;
        public Transform RewardPanel => rewardPanel;
        public Transform RewardObjectParent => rewardObjectParent;
        

        public GameObject Root { get; }
        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        public UniTask PlayEnterAsync(CancellationToken ct) => UniTask.CompletedTask;
        public UniTask PlayExitAsync(CancellationToken ct) => UniTask.CompletedTask;
        
        
        
        public void ToLobby()
        {
            SceneLoader.LoadSceneWithLoading(SystemEnum.eScene.LobbyScene);
        }
    }
    public class BattleWinPresenter : PresenterBase<BattleWinView>
    {
        private long _selectedSkillId;
        private List<long> _rewardList = new();
        
        public BattleWinPresenter(IView view) : base(view)
        {
            
        }

        public override UniTask EnterAction(CancellationToken token)
        {
            InstantiateRewardObjects();
            
            ViewEvents.Subscribe(
                act => View.RewardSkipButton.onClick.AddListener(new UnityAction(act)),
                act => View.RewardSkipButton.onClick.RemoveAllListeners(),
                SkipReward
            );
            ViewEvents.Subscribe(
                act => View.GetSkillButton.onClick.AddListener(new UnityAction(act)),
                act => View.GetSkillButton.onClick.RemoveAllListeners(),
                GetSkillReward
            );
            
            
            return UniTask.CompletedTask;
        }

        private readonly PresenterEventBag _eventBag = new();

        private void InstantiateRewardObjects()
        {
            foreach (RewardTable table in View.RewardList)
            {
                
                // 스킬 5개 이상 보유시
                if (BattleController.Instance.PlayerParty.partyMembers[0].ActiveSkills.Count >= 5)
                {
                    continue;
                }
                
                
                GameObject rewardTypeObject = Object.Instantiate(View.RewardTypeObject, View.RewardObjectParent, true);
                RewardObject rewardObject = rewardTypeObject.GetComponent<RewardObject>();
                Debug.Log(table.name);
                rewardObject.RewardButton.onClick.AddListener(() => OnClickRewardButton(table.name, rewardTypeObject));
            }

        }
        // 이건 나아아아중에 바꿀거임
        private void OnClickRewardButton(string rewardType, GameObject rewardTypeObject, int amount = 0)
        {
            Debug.Log(rewardType);
            
            switch (rewardType)
            {
                case "Skill":
                    {
                        Debug.Log("보상 패널 띄우기");
                        View.RewardPanel.gameObject.SetActive(false);
                        View.SkillRewardPanel.gameObject.SetActive(true);
                        InstantiateSkillRewardObjects();
                    }
                    break;
            }
            Object.Destroy(rewardTypeObject);
        }
        private async void InstantiateSkillRewardObjects()
        {
            _rewardList = new();
            
            // 이거 나중에 보상 테이블에서 가져오도록 설정할것 일단 뷰에서 가져오는거로 함
            
            // 리워드 테이블에서 Skill 태그 있는것
            foreach (RewardTable table in View.RewardList.Where(table => table.name == "Skill"))
            {
                // 현재 도깨비가 가지고 있는 스킬 개수
                int curDokSkillCount = BattleController.Instance.PlayerParty.partyMembers[0].ActiveSkills.Count;
                
                // 튜토리얼 기준 도깨비의 최대 스킬 개수는 5개
                // 가지고 있는 스킬 개수가 3보다 작으면 보상은 3개, 3보다 크거나 같으면 5에서 보유스킬 개수 빼줌
                int maxReward = curDokSkillCount < 3 ? 3 : 5 - curDokSkillCount;

                List<long> ownedSKills = new();
                Debug.Log("현재 보유중인 스킬 id");
                foreach (var skill in BattleController.Instance.PlayerParty.partyMembers[0].ActiveSkills)
                {
                    Debug.Log(skill.SkillIndex);
                    ownedSKills.Add(skill.SkillIndex);
                }
                
                // 보상 뽑기
                for (int i = 0; i < maxReward; i++)
                {
                    // 테이블에 있는 스킬 개수에서 랜덤으로 정수 뽑음
                    int randIndex =  UnityEngine.Random.Range(0, table.rewardRange.Count);
                    SkillReward reward = table.rewardRange[randIndex];
                    long skillId = table.rewardRange[randIndex].id;

                    // 이미 보유중인 스킬
                    if (ownedSKills.Contains(skillId) || _rewardList.Contains(skillId))
                    {
                        Debug.Log($"{skillId} : 이미 가지고 있거나 뽑힌 스킬");
                        i--;
                    }
                    else
                    {
                        _rewardList.Add(skillId);
                        
                        GameObject rewardObject = Object.Instantiate(View.SkillRewardUIPrefab, View.SkillRewardObjectParent, true);
                        SkillRewardObject rewardObj = rewardObject.GetComponent<SkillRewardObject>();
                        
                        // 보상 인덱스 및 스프라이트 설정
                        rewardObj.SetReward(i, reward.deSelected, reward.selected);

                        // 토글 리스트에 추가
                        View.AddToggleButton(rewardObj);
                        
                        // 단순한 index만 전달하자.
                        _eventBag.Subscribe<int>(
                            act =>
                            {
                                rewardObj.Selected -= act;
                                rewardObj.Selected += act;
                            },
                            act=> rewardObj.Selected -= act,
                            SelectRewardSkill
                        );
                        _eventBag.Subscribe<int>(
                            action =>
                            {
                                rewardObj.Deselected -= action;
                                rewardObj.Deselected += action;
                            },
                            act => rewardObj.Deselected -= act,
                            DeselectRewardSkill);
                    }
                }
            }
            Debug.Log("뽑힌 스킬들");
            foreach (var ids in _rewardList)
            {
                Debug.Log(ids);
            }
        }
        private void SelectRewardSkill(int idx)
        {
            _selectedSkillId = _rewardList[idx];
            View.GetSkillButton.interactable = true;
        }

        private void DeselectRewardSkill(int idx)
        {
            View.GetSkillButton.interactable = false;
        }
        private void GetSkillReward()
        {
            //selectedSkillId << 이 아이디의 스킬을 넣어라
            Debug.Log(_selectedSkillId);
            BattleController.Instance.GetSkill(_selectedSkillId);
            
            View.SkillRewardPanel.gameObject.SetActive(false);
            View.RewardPanel.gameObject.SetActive(true);
            BattleController.Instance.OnWinRewardClosed();
        }

        private void SkipReward()
        {
            Debug.Log("보상 받기 완료");
            BattleController.Instance.OnWinRewardClosed();
            //SceneLoader.LoadSceneWithLoading(BattleController.Instance.ReturningScene);
        }
        
    }
}