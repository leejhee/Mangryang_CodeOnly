using Cysharp.Threading.Tasks;
using GamePlay.Features.Battle.Scripts.Models;
using System.Threading;
using UIs.Runtime;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.UI.KeywordPopup
{
    public class KeywordPopupView : MonoBehaviour, IView
    {
        public GameObject Root { get; }
        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        public UniTask PlayEnterAsync(CancellationToken ct) => UniTask.CompletedTask;
        public UniTask PlayExitAsync(CancellationToken ct) => UniTask.CompletedTask;

        [SerializeField] private Transform _content;
        
        public Transform Content => _content;
        
    }

    public class KeywordPopupPresenter : PresenterBase<KeywordPopupView>
    {
        public KeywordPopupPresenter(IView view) : base(view)
        {
        }

        public override UniTask EnterAction(CancellationToken token)
        {
            
            
            return UniTask.CompletedTask;
        }
    }
}
