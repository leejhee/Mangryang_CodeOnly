using GamePlay.Features.Battle.Scripts;
using GamePlay.Features.Battle.Scripts.Models;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class FloatingKeyword : MonoBehaviour
{
    [SerializeField] private SpriteRenderer icon;
    [SerializeField] private TMP_Text count;
    [SerializeField] private TMP_Text amount;
    // 키워드 타입 이넘으로 받아오기 <= 동일 키워드 중복 부여시 amount, count 수정
    [SerializeField] private BattleKeyword keyword;

    public void OnInstantiated(FloatingKeywordModel model)
    {
        keyword = model.Keyword;
        icon.sprite = model.Icon;
        count.text = model.Count.ToString();
        amount.text = model.Amount.ToString();
    }

    public void ChangeKewordCount(int value)
    {
        count.text = value.ToString();
    }

    public void ChangeKewordAmount(int value)
    {
        amount.text = value.ToString();
    }
}
