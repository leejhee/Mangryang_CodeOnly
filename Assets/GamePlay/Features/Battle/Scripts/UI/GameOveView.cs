using Core.Scripts.Foundation.Define;
using Core.Scripts.Foundation.SceneUtil;
using Cysharp.Threading.Tasks;
using System.Threading;
using UIs.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GamePlay.Features.Battle.Scripts.UI
{
    public class GameOverView : MonoBehaviour, IView
    {
        [SerializeField] private Button restartButton;
        public Button RestartButton => restartButton;

        public GameObject Root { get; }
        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        public UniTask PlayEnterAsync(CancellationToken ct) => UniTask.CompletedTask;
        public UniTask PlayExitAsync(CancellationToken ct) => UniTask.CompletedTask;
    }

    public class GameOverPresenter : PresenterBase<GameOverView>
    {
        public GameOverPresenter(IView view) : base(view)
        {
        }

        public override UniTask EnterAction(CancellationToken token)
        {
            ViewEvents.Subscribe(
                act => View.RestartButton.onClick.AddListener(new UnityAction(act)),
                act => View.RestartButton.onClick.RemoveAllListeners(),
                Restart
            );
            
            return UniTask.CompletedTask;
        }

        private void Restart()
        {
            View.Hide();
            BattleController.Instance.RestartCurrentBattle();
        }
    }
}
