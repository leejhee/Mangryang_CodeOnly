using Core.Scripts.Foundation.Define;
using Newtonsoft.Json;
using System;

namespace GamePlay.Character.Save
{
    [Serializable]
    public class CharacterProgressSaveData
    {
        #region Skill Locker Part
        [JsonProperty("skillLocker")]
        public SystemEnum.eUnlockCondition eLocker;
        
        public void SetSkillFlag(SystemEnum.eUnlockCondition changingLocker, bool flag)
        {
            if (flag)
            {
                eLocker |= changingLocker;
            }
            else
            {
                eLocker &= ~changingLocker;
            }
        }
        public bool GetSkillFlag(SystemEnum.eUnlockCondition target) => eLocker.HasFlag(target);
        #endregion
    }
}