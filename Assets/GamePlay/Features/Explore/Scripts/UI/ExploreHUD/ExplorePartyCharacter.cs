using GamePlay.Features.Explore.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ExplorePartyCharacterUI : MonoBehaviour
{
    [SerializeField] private Image characterPortrait;
    [SerializeField] private Image hpFill;
    [SerializeField] private int index;
    public void InitExplorePartyCharacter(Sprite img, long curHp, long maxHp, int index)
    {
        characterPortrait.sprite = img;
        hpFill.fillAmount = curHp / (float)maxHp;
        this.index = index;
    }

    public void Click()
    {
        ExploreManager.Instance.ShowCharacterInfoPopup(index);
    }
}
