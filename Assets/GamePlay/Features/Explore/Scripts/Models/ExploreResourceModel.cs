using GamePlay.Features.Explore.Scripts.UI;

namespace GamePlay.Features.Explore.Scripts.Models
{
    public class ExploreResourceModel
    {
        public ExploreResourceType ResourceType;
        public int Amount;

        public ExploreResourceModel(ExploreResourceType resourceType, int  amount)
        {
            this.ResourceType = resourceType;
            this.Amount = amount;
        }
    }
}
