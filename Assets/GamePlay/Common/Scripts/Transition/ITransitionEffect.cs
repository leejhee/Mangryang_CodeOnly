using Cysharp.Threading.Tasks;
using System.Threading;

namespace GamePlay.Common.Scripts.Transition
{
    public interface ITransitionEffect
    {
        UniTask PlayOutAsync(float duration, CancellationToken ct = default);
        UniTask PlayInAsync(float duration, CancellationToken ct = default);
    }
}