using System;
using System.Collections;
using System.Collections.Generic;
using UIs.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterPortrait : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private const float TIME_TO_POPUP = 0.7f;
    private bool _isHolding = false;
    private float _pointingTime = 0f;
    public event Action CharacterInfoPopup;
    private void Update()
    {
        if (_isHolding)
        {
            _pointingTime += Time.deltaTime;
            if (_pointingTime >= TIME_TO_POPUP)
            {
                OnCharacterInfoPopup();
                _isHolding = false;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isHolding = true;
        _pointingTime = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isHolding = false;
    }

    private void OnCharacterInfoPopup()
    {
        CharacterInfoPopup?.Invoke();
        
        // UI 팝업시키기 위한 임시코드
        //_ = UIManager.Instance.ShowViewAsync(ViewID.BattleCharacterInfoPopUpView);
    }
}
