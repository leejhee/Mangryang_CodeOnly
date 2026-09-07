using Core.Scripts.Data;
using Core.Scripts.Foundation.Define;
using Core.Scripts.Managers;
using System;
using UnityEngine;

namespace GamePlay.Common.Scripts.Entities.Skills
{
    [Serializable]
    public class SkillModel
    {
        public readonly long                    SkillIndex; // 데이터상의 인덱스
        public readonly string                  SkillName;
        public readonly long                    SkillOwnerID;
        public readonly SystemEnum.eSkillType   SkillType;
        public readonly string                  Icon;
        public readonly int                     CritCalibration;
        public readonly int                     SkillAccuracy;
        public readonly string                  PrefabName;
        public readonly string                  TooltipName;
        
        public SkillRangeData                   SkillRange;
        public SkillDamageData                  SkillDamage;
        public readonly SystemEnum.eSkillUnlock Unlock;
        public bool                             locked = true;
            
        public SkillModel(SkillData skillData)
        {
            SkillIndex = skillData.index;
            SkillName = skillData.skillName;
            SkillOwnerID = skillData.characterID;
            SkillType = skillData.skillType;
            
            CritCalibration = skillData.skillCritical;
            SkillAccuracy = skillData.skillAccuracy;

            Icon = skillData.skillIconImage;
            SkillRange = DataManager.Instance.GetData<SkillRangeData>(skillData.skillRangeID);
            SkillDamage = skillData.skillDamage > 0
                ? DataManager.Instance.GetData<SkillDamageData>(skillData.skillDamage)
                : null;
            Unlock = skillData.unlockCondition;
            PrefabName = skillData.skillTimeLine;
            TooltipName = skillData.SkillToolTip;
        }

        public SkillModel(DokkaebiSkillData skillData)
        {
            SkillIndex = skillData.index;
            SkillName =  skillData.skillName;
            SkillOwnerID = SystemConst.DokkaebiID;
            SkillType = skillData.skillType;
            
            CritCalibration = skillData.skillCritical;
            SkillAccuracy = skillData.skillAccuracy;
            
            Icon = skillData.skillIconImage;
            SkillRange = DataManager.Instance.GetData<SkillRangeData>(skillData.skillRange);
            SkillDamage = skillData.skillDamage > 0
                ? DataManager.Instance.GetData<SkillDamageData>(skillData.skillDamage)
                : null;
            Unlock = SystemEnum.eSkillUnlock.None;
            locked = false;
            PrefabName = skillData.skillTimeLine;
            TooltipName = skillData.SkillToolTip;
        }
        
    }
    
}
