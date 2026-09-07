using Core.Scripts.Foundation.Define;
using UnityEngine;

namespace GamePlay.Features.Explore.Scripts.Map
{
    public class ExploreSymbolAuthoring : MonoBehaviour
    {
        [Header("기본 심볼 타입")]
        public SystemEnum.MapSymbolType symbolType;

        [Header("이벤트 심볼용 옵션")]
        public bool useEventType;
        public SystemEnum.CellEventType eventType;

        [Header("아이템 심볼용 옵션")]
        public bool useItemIndex;
        public long itemIndex;

        [Header("노벨용 옵션")]
        public string novelId;
    }
}