using Core.Scripts.Foundation.Define;
using Cysharp.Threading.Tasks;
using System.Threading;
using UIs.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GamePlay.Features.Battle.Scripts.UI
{
    // TODO : 간단한 선택 UI의 경우 상속으로 처리해서 재사용할 지 결정할 것
    public class BattleQuitQueryView : PopupView
    {
        public Button confirmButton;
        public Button cancelButton;
    }

    public class BattleQuitQueryPresenter : PresenterBase<BattleQuitQueryView>
    {
        public BattleQuitQueryPresenter(IView view) : base(view)
        {
        }

        public override UniTask EnterAction(CancellationToken token)
        {
            ViewEvents.Subscribe(
                act => View.confirmButton.onClick.AddListener(new UnityAction(act)),
                act => View.confirmButton.onClick.RemoveAllListeners(),
                OnConfirm
            );
            
            ViewEvents.Subscribe(
                act => View.cancelButton.onClick.AddListener(new UnityAction(act)),
                act => View.cancelButton.onClick.RemoveAllListeners(),
                OnCancel
            );
            
            return UniTask.CompletedTask;
        }

        private void OnConfirm()
        {
            BattleController.Instance.EndBattle(SystemEnum.eCharType.None);
        }

        private void OnCancel()
        {
            UIManager.Instance.HideTopViewAsync().Forget();
        }
    }
}