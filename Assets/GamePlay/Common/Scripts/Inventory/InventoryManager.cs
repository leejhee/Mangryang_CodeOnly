using Core.Scripts.Foundation.Singleton;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace GamePlay.Features.Scripts.Common.Inventory
{
    /// <summary>
    /// 탐사 & 전투에서 사용되는 인벤토리 시스템
    /// </summary>
    public class InventoryManager : SingletonMono<InventoryManager>
    {

        public override async UniTask InitAsync()
        {
            await UniTask.Yield();
            //인벤토리 데이터만 세이브대로 초기화
            //업데이트는 다른 메서드로, 인벤토리 구조 따라서 진행
        }
    }
}