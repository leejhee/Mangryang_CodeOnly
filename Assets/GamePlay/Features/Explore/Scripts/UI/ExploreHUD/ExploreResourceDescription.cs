using UnityEngine;
using UnityEngine.EventSystems;

namespace GamePlay.Features.Explore.Scripts.UI.ExploreHUD
{
    public class ExploreResourceDescription : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject descriptionObject;
        public void OnPointerEnter(PointerEventData eventData)
        {
            descriptionObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            descriptionObject.SetActive(false);
        }
    }
}
