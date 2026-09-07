using Cysharp.Threading.Tasks;
using System.Threading;

namespace GamePlay.Common.Scripts.Contracts
{
    public interface IDamageable
    {
        UniTask DamageAsync(long finalDamage, CancellationToken ct);
    }
}