using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace UIs.Runtime
{
    /// <summary>
    /// View 오브젝트에 대응하는 ID로 간주.
    /// State에 따라 갈려야 하므로 식별이 용이하게 각 View의 ID를 기입할 것.
    /// </summary>
    public enum ViewID
    {
        None = 0,
        
        LobbySceneView = 1,
        
        VillageSceneView = 101,
        VillageToExploreView,
        VillageToExploreInteractionView,
        
        ExploreSceneView = 201,
        ExploreHerbPopup,
        ExploreChestPopup,
        
        BattleSceneView = 301,
        CharacterInfoPopUpView,
        BattleQuitQueryView,
        
        BattleHoveringView = 351,
        KeywordPopUpView,
        
        GameWinView,
        GameOverView,
        
        CharacterView,
    }

    public enum ViewLayer
    {
        Main,
        Modal,
    }
    
    
    public interface IPresenter : IDisposable
    {
        //프리젠터는 게임이 멈췄을 때 View를 통제할 책임이 있다.
        void OnPause();
        
        //프리젠터는 게임이 재개될 때 View를 통제할 책임이 있다.
        void OnResume();
        
        //프리젠터는 비동기적으로 View를 보여줄 수 있다.
        UniTask OnEnterAsync(CancellationToken token);
        
        //프리젠터는 비동기적으로 View를 닫을 수있다.
        UniTask OnExitAsync(CancellationToken token);
        
    }


    public interface IModel { }

    public interface IView
    {
        GameObject Root { get; }
        void Show();
        void Hide();
        UniTask PlayEnterAsync(CancellationToken ct);
        UniTask PlayExitAsync(CancellationToken ct);
    }


    /// <summary>
    /// Presenter 기능 구현이 필요 없을 시 사용 가능한 디폴트 Presenter
    /// </summary>
    public sealed class NullPresenter : IPresenter
    {
        private readonly IView _view;
        public NullPresenter(IView view) { _view = view; }

        public async UniTask OnEnterAsync(CancellationToken ct)
        {
            _view.Show();
            await _view.PlayEnterAsync(ct);
        }

        public async UniTask OnExitAsync(CancellationToken ct)
        {
            await _view.PlayExitAsync(ct);
            _view.Hide();
        }
        public void OnPause() { } public void OnResume() { } public void Dispose() { }
    }
}