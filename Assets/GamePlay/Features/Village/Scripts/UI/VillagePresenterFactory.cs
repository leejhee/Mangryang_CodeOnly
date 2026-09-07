using UIs.Runtime;
using UnityEngine;

namespace GamePlay.Features.Village.Scripts.UI
{
    [CreateAssetMenu(fileName = "VillagePresenterFactory", menuName = "ScriptableObject/UIPresenter/VillagePresenterFactory")]
    public class VillagePresenterFactory : PresenterFactory
    {
        public override IPresenter Create(ViewID id, IView view, object param = null)
        {
            switch (id)
            {
                case ViewID.VillageToExploreInteractionView:
                    return new VillageToExploreInteractionPresenter(view);
                default:
                    return new NullPresenter(view);
            }
            
        }
    }
}