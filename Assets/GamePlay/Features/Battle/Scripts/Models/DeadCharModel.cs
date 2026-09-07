using Core.Scripts.Foundation.Define;

namespace GamePlay.Features.Battle.Scripts.Models
{
    public class DeadCharModel
    {
        public long charUID;
        public SystemEnum.eCharType charType;

        public DeadCharModel(long charUID, SystemEnum.eCharType charType)
        {
            this.charUID = charUID;
            this.charType = charType;
        }
    }
}