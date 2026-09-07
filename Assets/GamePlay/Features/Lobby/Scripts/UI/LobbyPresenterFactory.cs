using UIs.Runtime;
using UnityEngine;

namespace GamePlay.Features.Lobby.Scripts.UI
{
    [CreateAssetMenu(fileName = "LobbyPresenterFactory", menuName = "ScriptableObject/UIPresenter/LobbyPresenterFactory")]
    public class LobbyPresenterFactory : PresenterFactory
    {
        public override IPresenter Create(ViewID id, IView view, object param = null)
        {
            switch (id)
            {
                default:
                    return new NullPresenter(view);
            }
        }
    }
}