using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using GamePlay.Features.Explore.Scripts;
using GamePlay.Features.Explore.Scripts.Models;
using GamePlay.Features.Explore.Scripts.UI;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UIs.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class ExploreChestView : MonoBehaviour, IView
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

public class ExploreChestPresenter : PresenterBase<ExploreChestView>
{
    public ExploreChestPresenter(IView view) : base(view)
    {
    }
    private readonly PresenterEventBag _eventBag = new();
    public override UniTask EnterAction(CancellationToken token)
    {

        ViewEvents.Subscribe(
            act => View.GatherButton.onClick.AddListener(new UnityAction(act)),
            act => View.GatherButton.onClick.RemoveAllListeners(),
            GetTalisman
        );
        ViewEvents.Subscribe(
            act => View.QuitButton.onClick.AddListener(new UnityAction(act)),
            act => View.QuitButton.onClick.RemoveAllListeners(),
            View.Hide
        );
        
        return UniTask.CompletedTask;
    }

    private void GetTalisman()
    {
        ExploreManager.Instance.GetExploreResource(new ExploreResourceModel(ExploreResourceType.Talisman, 1));
        View.Hide();
    }
    
}