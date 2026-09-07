using Cysharp.Threading.Tasks;
using GamePlay.Features.Battle.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UIs.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace GamePlay.Features.Battle.Scripts.UI.IngameUI
{
    public class CharHoveringUI : MonoBehaviour
    {
        [SerializeField] private Transform keywordPanel;
        [SerializeField] private Dictionary<BattleKeyword, FloatingKeyword> _keywords = new();
        [SerializeField] private Image hpFill;
        [SerializeField] private Button keywordButton;
        public Transform KeywordPanelTransform => keywordPanel;
        public Dictionary<BattleKeyword, FloatingKeyword> Keywords => _keywords;
        //public Image HPFill => hpFill;
        public Button KeywordButton => keywordButton;

        public event Action<PopupKeywordModel> KeywordPopup;
        
        
        public async void ShowKeywordPopup()
        {
            // 키워드 팝업 뷰 켜주기
            UniTask task = UIManager.Instance.ShowViewAsync(ViewID.KeywordPopUpView);
            await task;
            // 켜질때까지 기다리기
            
            // 다 켜지면 해당하는 키워드 리스트를 담아서 이벤트 발생
            List<BattleKeyword> keywordList = _keywords.Keys.ToList();
            KeywordPopup?.Invoke(new PopupKeywordModel(keywordList));
            
        }
        
        public void InitHpFill()
        {
            hpFill.fillAmount = 1f;
        }
        
        public async void SetHpFill(long curHp, long maxHp)
        {
            await HpFillChanged(curHp, maxHp, hpFill);
        }

        private async UniTask HpFillChanged(long curHp, long maxHp, Image fill)
        {
            const float timeToEnd = 0.5f;
            float counter = 0f;
            float startValue = fill.fillAmount;
            float targetValue = (float)curHp/(float)maxHp;
            while (counter < timeToEnd)
            {
                counter += Time.deltaTime;
                float t = counter/timeToEnd;
                float curValue = Mathf.Lerp(startValue, targetValue, t);
                fill.fillAmount = curValue;
                await UniTask.Yield();
            }
            fill.fillAmount = targetValue;
        }
        
    }
    // public class IngameCharacterPresenter
    // {
    //     
    //     public void UniTask EnterAction(CancellationToken token)
    //     {
    //         // 어떤 캐릭터이든 체력이 바꼈을때
    //         // ModelEvents.Subscribe<CharacterModel>(
    //         //     act => BattleController.Instance.,
    //         //     act => BattleController.Instance,
    //         //     OnHpChanged
    //         // );
    //         ModelEvents.SubscribeAsync<FloatingKeywordModel>(
    //             act => View.OnKeywordGet += act,
    //             act => View.OnKeywordGet -= act,
    //             InstantiateNewKeword
    //             );
    //         ViewEvents.Subscribe(
    //             act => View.KeywordButton.onClick.AddListener(new UnityAction(act)),
    //             act => View.KeywordButton.onClick.RemoveAllListeners(),
    //             OnClickKeyWordButton
    //         );
    //         
    //         return UniTask.CompletedTask;
    //     }
    //     // private void OnHpChanged(CharacterModel charModel)
    //     // {
    //     //     View.SetHpFill(charModel.BaseStat.GetStat(SystemEnum.eStats.NHP), charModel.BaseStat.GetStat(SystemEnum.eStats.NMHP));
    //     // }
    //
    //     // private async UniTask InstantiateNewKeword(FloatingKeywordModel model)
    //     // {
    //     //     UniTask<GameObject> task =
    //     //         ResourceManager.Instance.InstantiateAsync(floatingKeywordAddress, View.KeywordPanelTransform, true);
    //     //
    //     //     FloatingKeyword keyword = (await task).GetComponent<FloatingKeyword>();
    //     //     keyword.OnInstantiated(model);
    //     //     View.Keywords.Add(keyword);
    //     // }
    //     //
    //     
    //     private void OnClickKeyWordButton()
    //     {
    //         UIManager.Instance.ShowViewAsync(ViewID.KeywordPopUpView);
    //     }
    // }
} 
