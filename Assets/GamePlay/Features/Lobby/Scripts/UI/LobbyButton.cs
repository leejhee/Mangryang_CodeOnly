using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class LobbyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite highlightedSprite;
    private Button _button;
    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_button.interactable)
        {
            GetComponent<Image>().sprite = highlightedSprite;
        }

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_button.interactable)
            GetComponent<Image>().sprite = normalSprite;
    }
}
