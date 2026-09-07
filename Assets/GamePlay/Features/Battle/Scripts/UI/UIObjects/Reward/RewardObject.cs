using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RewardObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button getButton;

    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite hoverSprite;
    public Button RewardButton => getButton;
    public void OnPointerEnter(PointerEventData eventData)
    {
        GetComponent<Image>().sprite = hoverSprite;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GetComponent<Image>().sprite = normalSprite;
    }
}
