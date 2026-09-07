using Cysharp.Threading.Tasks;
using System.Threading;
using UIs.Runtime;
using UnityEngine;

namespace GamePlay.Features.Village.Scripts.UI
{
    /// <summary>
    /// Village UI : 탐사로 이어지는 팝업을 띄우기 위한 상호작용 팝업
    /// </summary>
    public class VillageToExploreInteractionView : MonoBehaviour, IView
    {
        public GameObject Root { get; }

        
        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        public UniTask PlayEnterAsync(CancellationToken ct) => UniTask.CompletedTask;
        public UniTask PlayExitAsync(CancellationToken ct) => UniTask.CompletedTask;

        public async UniTask ShowExploreGuideOnClick()
        {
            await UIManager.Instance.ShowViewAsync(ViewID.VillageToExploreView);
        }
    }
    
    public class VillageToExploreInteractionPresenter : PresenterBase<VillageToExploreInteractionView>
    {
        public VillageToExploreInteractionPresenter(IView view) : base(view)
        { }
        
        public override UniTask EnterAction(CancellationToken token)
        {
            //모델 = InputAction.
            return UniTask.CompletedTask;
        }

        public override UniTask ExitAction(CancellationToken token)
        {
            return UniTask.CompletedTask;
        }
    }
}