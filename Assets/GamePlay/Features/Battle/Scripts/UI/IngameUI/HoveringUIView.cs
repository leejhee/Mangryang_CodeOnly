using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Entities.Character;
using GamePlay.Features.Battle.Scripts.Models;
using System.Collections.Generic;
using System.Threading;
using UIs.Runtime;
using UnityEngine;
using Core.Scripts.Managers;
using UnityEngine.Events;

namespace GamePlay.Features.Battle.Scripts.UI.IngameUI
{
    public class HoveringUIView : MonoBehaviour, IView
    {
        public GameObject Root { get; }


        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        public UniTask PlayEnterAsync(CancellationToken ct) => UniTask.CompletedTask;
        public UniTask PlayExitAsync(CancellationToken ct) => UniTask.CompletedTask;

    }

    public class HoveringUIPresenter : PresenterBase<HoveringUIView>
    {
        public HoveringUIPresenter(IView view) : base(view) {}
        
        private const string floatingKeywordAddress = "KeywordFloatingObject";
        private const string hoveringUIAddress = "CharacterHoveringUI";
        private Dictionary<long, CharHoveringUI> _hoverDict = new();
        
        
        public override UniTask EnterAction(CancellationToken toke)
        {
            // 캐릭터 생성 이벤트
            // 캐릭터 사망 이벤트
            // 체력 변경 이벤트
            
            
            // 키워드 획득 및 제거 이벤트
            // ModelEvents.SubscribeAsync<FloatingKeywordModel>(
            //     act => View.OnKeywordGet += act,
            //     act => View.OnKeywordGet -= act,
            //     InstantiateNewKeword
            // );
            // ViewEvents.Subscribe(
            //     act => View.KeywordButton.onClick.AddListener(new UnityAction(act)),
            //     act => View.KeywordButton.onClick.RemoveAllListeners(),
            //     OnClickKeyWordButton
            // );

            
            return UniTask.CompletedTask;
        }
        
        // 플로팅 ui 오브젝트 만들어서 해당하는 캐릭터에 달아줌
        private async UniTask CreateHoveringUI(CharacterModel model)
        {
            // Hovering UI 생성
            UniTask<GameObject> task =
                ResourceManager.Instance.InstantiateAsync(hoveringUIAddress, UIManager.Instance.WorldRoot, true);
            
            CharHoveringUI hover = (await task).GetComponent<CharHoveringUI>();
            
            // 딕셔너리에 추가해줌
            _hoverDict.Add(model.Index, hover);
            
            
            // 해당하는 캐릭터 위치에 띄워줌
        }
        
        // 캐릭터 사망시 플로팅 UI 없애주기
        private void RemoveHoveringUI(CharacterModel model)
        {
            // 삭제할 HoveringUI 선택
            CharHoveringUI hover =  _hoverDict[model.Index];
            
            // 키워드들 제거
            foreach (FloatingKeyword keyword in hover.Keywords.Values)
            {
                ResourceManager.Instance.ReleaseInstance(keyword.gameObject);
            }
            
            // Hovering 오브젝트 제거
            ResourceManager.Instance.ReleaseInstance(hover.gameObject);
            // 딕셔너리에서 삭제
            _hoverDict.Remove(model.Index);
        }
        
        // 캐릭터 위치 변경시
        private void MoveCharPosition(BattleCharacterMoveModel model)
        {
            GameObject hoverObject = _hoverDict[model.UID].gameObject;
            
            // 위에 게임 오브젝트 캐릭터 이동에 맞춰서 옮겨줌
        }
        
        // 체력 변경시
        private void OnHpChanged(CharacterModel character)
        {
            //_hoverDict[character.Index].SetHpFill();
        }
        
        // 새 키워드 부여
        private async UniTask InstantiateNewKeword(FloatingKeywordModel model)
        {
            // uid에 맞는 hover 찾기
            CharHoveringUI hover = _hoverDict[model.UID];
            
            
            // 키워드 오브젝트 생성
            UniTask<GameObject> task =
                ResourceManager.Instance.InstantiateAsync(floatingKeywordAddress, hover.KeywordPanelTransform, true);
            
            FloatingKeyword keyword = (await task).GetComponent<FloatingKeyword>();
            keyword.OnInstantiated(model);
            
            hover.Keywords.Add(model.Keyword, keyword);
        }
        // 키워드 수치 변화
        private void SetKeyword()
        {
            
        }
        // 키워드 제거
        private void RemoveKeyword(FloatingKeywordModel model)
        {
            // 해당하는 캐릭터의 해당하는 키워드 제거
            GameObject keywordObject = _hoverDict[model.UID].Keywords[model.Keyword].gameObject;
            ResourceManager.Instance.ReleaseInstance(keywordObject.gameObject);
        }
        
        
        
        private void MoveHoveringUI(CharacterModel model)
        {
            // uid에 맞는 hover 찾기
            CharHoveringUI hover = _hoverDict[model.Index];
        }
    }
}
