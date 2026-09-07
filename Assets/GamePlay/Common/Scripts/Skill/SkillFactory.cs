using Core.Scripts.Data;
using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Skill;
using UnityEngine;

namespace GamePlay.Common.Scripts.Entities.Skills
{
    public static class SkillFactory
    {
        
        public static SkillBase CreateSkill(string skillName)
        {
            SkillBase skillBase = null;
            skillBase = ResourceManager.Instance.Instantiate<SkillBase>($"Skill/{skillName}");
            return skillBase;
        }

        public static async UniTask<SkillBase> CreateSkill(SkillModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.PrefabName))
            {
                Debug.LogError("[SkillFactory] SkillModel 또는 Prefab Address가 없습니다.");
                return null;
            }

            GameObject go = await ResourceManager.Instance.InstantiateAsync(model.PrefabName);
            if (!go)
            {
                Debug.LogError($"[SkillFactory] 스킬 프리팹 생성 실패: {model.PrefabName}");
                return null;
            }

            SkillBase skillBase = go.GetComponent<SkillBase>();
            if (!skillBase)
            {
                Debug.LogError(
                    $"[SkillFactory] '{model.PrefabName}' 프리팹에 SkillBase가 없습니다.",
                    go);
                ResourceManager.Instance.ReleaseInstance(go);
                return null;
            }

            skillBase.Init(model);
            return skillBase;
        } 
        
        public static SkillBase CreateSkill(long skillIndex)
        {
            SkillBase skillBase = null;
            var _skillData = DataManager.Instance.GetData<SkillData>(skillIndex);

            if (_skillData == null)
            {
                Debug.LogWarning($"CreateSkill : {skillIndex} 스킬 생성 실패.");
                return null;
            }

            skillBase = ResourceManager.Instance.Instantiate<SkillBase>($"Skill/{_skillData.skillTimeLine}");
            skillBase.Init(new SkillModel(_skillData));

            return skillBase;
        }
        
        // GetData를 많이 하는 것보다 나을 거 같아서 사용
        public static SkillBase CreateSkill(SkillData skillData)
        {
            if (skillData == null)
            {
                Debug.LogError("왜 매개변수로 null 넣으세요? : CreateSkill(SkillData)");
                return null;
            }
            SkillBase skillBase = ResourceManager.Instance.Instantiate<SkillBase>($"Skill/{skillData.skillTimeLine}");
            skillBase.Init(new SkillModel(skillData));
            return skillBase;
        }
        
    }
}
