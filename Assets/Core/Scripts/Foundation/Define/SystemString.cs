using System.Text;

namespace Core.Scripts.Foundation.Define
{
    public class SystemString
    {
    
        public const string SkillIconPath = "Sprites/SkillIcon/";
        public const string KeywordIconPath = "Sprites/KeywordIcon/";
        public const string PlayerHitCollider = "PlayerHitCollider";
        public const string MonsterHitCollider = "MonsterHitCollider";
        public const string MapConfigDBPath = "ScriptableObjects/ExploreMapConfigs/ConfigDB";
        
        public const string JsonExtension = ".json";
        
        public const string GlobalSaveDataPath = "SaveData";
        public const string SlotPrefix = "GameSlot_";

        public const string ExploreKeyFeature = "Explore";
        public const string BattleKeyFeature = "Battle";
        public const string VillageKeyFeature = "Village";
        public const string GlobalKeyFeature = "Global";
        
        
        public static string GetSlotName(int slotIndex)
        {
            return new StringBuilder(SlotPrefix).Append(slotIndex.ToString()).ToString();
        } 
    }
}