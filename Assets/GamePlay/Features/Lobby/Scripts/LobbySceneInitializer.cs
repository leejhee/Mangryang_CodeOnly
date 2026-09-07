using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace GamePlay.Features.Lobby.Scripts
{
    public static class LobbySceneInitializer
    {
        public static async UniTask LobbyInitialize(CancellationToken ct, IProgress<float> progress)
        {
            //저장하고 세션 클리어하는 거로 해주자.
        }
    }
}