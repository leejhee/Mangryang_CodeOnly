using Core.Scripts.Foundation.Define;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace Core.Scripts.Boot
{
    /// <summary>
    /// 씬의 부트스트랩에 대한 인터페이스
    /// 왜 만들었냐? 
    /// </summary>
    public interface ISceneBootstrap
    {
        UniTask InitializeAsync(ISceneContext context, CancellationToken ct, IProgress<float> progress);

        void StartScene();
        
        void CleanUpScene();
    }

    public interface ISceneContext
    {
        SystemEnum.eScene Scene { get; }
    }
}