using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UIs.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class ExploreHerbView : MonoBehaviour, IView
{
    public GameObject Root { get; }
    public void Show()
    {
        InputManager.Instance.SetUIOnly(true);
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
        InputManager.Instance.SetUIOnly(false);
    }

    public UniTask PlayEnterAsync(CancellationToken ct) => UniTask.CompletedTask;
    public UniTask PlayExitAsync(CancellationToken ct) => UniTask.CompletedTask;



    [SerializeField] private Button gatherButton;
    [SerializeField] private Button quitButton;
    
    public Button GatherButton => gatherButton;
    public Button QuitButton => quitButton;
}

public class ExploreHerbPresenter : PresenterBase<ExploreHerbView>
{
    public ExploreHerbPresenter(IView view) : base(view)
    {
    }
    private readonly PresenterEventBag _eventBag = new();
    public override UniTask EnterAction(CancellationToken token)
    {

        ViewEvents.Subscribe(
            act => View.GatherButton.onClick.AddListener(new UnityAction(act)),
            act => View.GatherButton.onClick.RemoveAllListeners(),
            GatherHerb
        );
        ViewEvents.Subscribe(
            act => View.QuitButton.onClick.AddListener(new UnityAction(act)),
            act => View.QuitButton.onClick.RemoveAllListeners(),
            View.Hide
        );
        
        return UniTask.CompletedTask;
    }

    private void GatherHerb()
    {
        View.Hide();
        
    }
    
}
