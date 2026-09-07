using GamePlay.Common.Scripts.Entities.Skills;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillDescription : MonoBehaviour
{
    [SerializeField] private TMP_Text skillName;
    [SerializeField] private TMP_Text skillDescription;
    [SerializeField] private Sprite skillDescriptionSprite;

    public void SetSkillDescription(Sprite description)
    {
        Image tooltipImage = this.GetComponent<Image>();
        skillDescriptionSprite = description;
        
        tooltipImage.sprite = description;
        tooltipImage.SetNativeSize();
    }
}
