using Core.Scripts.Foundation.Define;
using GamePlay.Common.Scripts.Entities.Skills;
using System.Collections.Generic;
using UnityEngine;
using Core.Scripts.Managers;

namespace GamePlay.Features.Battle.Scripts.UI.CharacterInfoPopup
{
    public class SkillPanel: MonoBehaviour
    {
        [SerializeField] private List<CharacterInfoPopupSkill> skillList = new();
        public List<CharacterInfoPopupSkill> SkillList => skillList;
        public void SetSkills(List<CharacterInfoPresenter.InfoPopupSkillResourceRoot> skillResourceRoots)
        {
            int idx = 0;
            foreach (CharacterInfoPopupSkill skill in skillList)
            {
                // 매개변수 리스트 크기만큼만 스킬 활성화 해줌
                if (idx < skillResourceRoots.Count)
                {
                    skill.SetSkillImage(skillResourceRoots[idx], idx);
                }
                else
                {
                    // 활성화 되지 않는 스킬
                    skill.InactiveSkillImage();
                }

                if (GameManager.Instance.GameState == SystemEnum.GameState.Battle)
                {
                    skill.ActivateInteractable(false);
                }
                idx++;
            }
        }
    }
}
