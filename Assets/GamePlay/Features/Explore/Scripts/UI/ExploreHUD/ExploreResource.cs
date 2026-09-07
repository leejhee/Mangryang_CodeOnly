using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GamePlay.Features.Explore.Scripts.UI
{
    public enum ExploreResourceType
    {
        Talisman,
        ReviveTalisman,
        Money
    }
    public class ExploreResource : MonoBehaviour
    {
        [SerializeField] private ExploreResourceType resourceType;
        [SerializeField] private TMP_Text amountText;
        
        public ExploreResourceType ResourceType => resourceType;

        public void SetResourceAmount(int amount)
        {
            amountText.text = amount.ToString();
        }
    }
}
