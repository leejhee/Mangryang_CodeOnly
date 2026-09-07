using UIs.Runtime;
using UnityEngine;

namespace GamePlay.Features.Explore.Scripts.Symbol.Encounter
{
    public class HerbEncounter : EncounterSymbol
    {
        protected override void OnEncounter(ExploreController player)
        {
            _ = UIManager.Instance.ShowViewAsync(ViewID.ExploreHerbPopup);
            
            Destroy(gameObject);
        }
    }
}
