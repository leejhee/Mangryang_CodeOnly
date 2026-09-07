using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace Core.Scripts.Boot
{
    public static class GameReady
    {
        private static bool isReady;
        private static readonly SemaphoreSlim InitGate = new(1, 1);

        public static bool IsReady => isReady;

        public static async UniTask InitializeOnceAsync()
        {
            if (isReady) return;

            await InitGate.WaitAsync();
            try
            {
                if (isReady) return;

                await ResourceManager.Instance.InitAsync();
                await DataManager.Instance.InitAsync();

                // SingletonObject<T> 기반 동기 매니저 초기화
                SaveLoadManager.Instance.Init();
                SoundManager.Instance.Init();

                isReady = true;
                Debug.Log("All Managers Initialized to Run");
            }
            finally
            {
                InitGate.Release();
            }
        }
    }
}
