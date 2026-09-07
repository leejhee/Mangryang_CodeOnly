using Core.Scripts.Foundation.Define;
using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Entities.Character;
using GamePlay.Features.Explore.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UIs.Runtime;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GamePlay.Features.Explore.Scripts.UI
{
    public class ExploreHUDView : MonoBehaviour, IView
    {
        public GameObject Root { get; }

        [SerializeField] private Transform partyTransform;
        [SerializeField] private Image lunarPhase;
        [SerializeField] private List<ExploreResource> exploreResources;
        
        public Transform PartyTransform => partyTransform;
        public Image  LunarPhase => lunarPhase;
        public List<ExploreResource> ExploreResources => exploreResources;
        
        
        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        public UniTask PlayEnterAsync(CancellationToken ct) => UniTask.CompletedTask;
        public UniTask PlayExitAsync(CancellationToken ct) => UniTask.CompletedTask;
    }

    public class ExploreHUDPresenter : PresenterBase<ExploreHUDView>
    {
        public ExploreHUDPresenter(IView view) : base(view)
        {
        }
        private const string PartyCharacterAddress = "PartyCharacter";

        //private Dictionary<int, ExplorePartyCharacterUI> _exploreCharacters = new();

        public override UniTask EnterAction(CancellationToken token)
        {
            // 재화 수치 변경 이벤트 구독

            ModelEvents.Subscribe<ExploreResourceModel>(
                act => ExploreManager.Instance.GetExploreResourceEvent += act,
                act => ExploreManager.Instance.GetExploreResourceEvent -= act,
                ChangeResourceAmount
            );
            
            // 파티원 초상화 생성
            InstantiatePartyPortrait(ExploreManager.Instance.playerParty.partyMembers);
            
            
            return UniTask.CompletedTask;
        }

        private void SetResourceAmount()
        {
            int money = ExploreManager.Instance.playerParty.money;
            int talisman = ExploreManager.Instance.playerParty.talisman;
            int revivalTalisman = ExploreManager.Instance.playerParty.revivalTalisman;

            foreach (var resource in View.ExploreResources)
            {
                switch (resource.ResourceType)
                {
                    case ExploreResourceType.Talisman:
                        resource.SetResourceAmount(talisman);
                        break;
                    case ExploreResourceType.ReviveTalisman:
                        resource.SetResourceAmount(revivalTalisman);
                        break;
                    case ExploreResourceType.Money:
                        resource.SetResourceAmount(money);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
        }
        // 보유 재화 수치 변경
        public void ChangeResourceAmount(ExploreResourceModel model)
        {
            foreach (ExploreResource resource in View.ExploreResources.Where(resource => resource.ResourceType == model.ResourceType))
            {
                resource.SetResourceAmount(model.Amount);
                break;
            }
        }
        
        public async void SetLunarPhase(LunarPhaseModel model)
        {
            Sprite lunarSprite = await ResourceManager.Instance.LoadAsync<Sprite>(model.phase.ToString());
            View.LunarPhase.sprite = lunarSprite;
        }
        
        public async void InstantiatePartyPortrait(List<CharacterModel> models)
        {
            for (int i = 0; i < models.Count; i++)
            {
                // UI 오브젝트 생성
                UniTask<GameObject> goTask =  ResourceManager.Instance.InstantiateAsync(PartyCharacterAddress, View.PartyTransform);
                ExplorePartyCharacterUI exploreCharacter = (await goTask).GetComponent<ExplorePartyCharacterUI>();
                
                // 스프라이트 불러오기
                Sprite sprite = await ResourceManager.Instance.LoadAsync<Sprite>($"Explore_{models[i].PrefabRoot}");
                
                // 이미지, 체력 설정
                exploreCharacter.InitExplorePartyCharacter(
                    sprite,
                    models[i].BaseStat.GetStat(SystemEnum.eStats.NHP),
                    models[i].BaseStat.GetStat(SystemEnum.eStats.NMHP),
                    i);
            }
        }

    }
}
