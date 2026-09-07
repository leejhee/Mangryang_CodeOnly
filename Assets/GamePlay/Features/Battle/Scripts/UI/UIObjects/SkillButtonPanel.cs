using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.UI.UIObjects
{
    
    public class SkillButtonPanel : MonoBehaviour
    {
        [SerializeField] private List<SkillButton> skillButtons;
        public List<SkillButton> SkillButtons => skillButtons;
        
        public void SetInteractable(bool enable)
        {
            foreach (SkillButton skillButton in skillButtons)
            {
                ToggleButton button = skillButton;
                button.selectable = enable;
            }
        }
        
    } 
}

