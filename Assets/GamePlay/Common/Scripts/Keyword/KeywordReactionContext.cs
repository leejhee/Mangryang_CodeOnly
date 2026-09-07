using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Entities.Character.Components;
using GamePlay.Features.Battle.Scripts.Unit;
using System.Threading;

namespace GamePlay.Common.Scripts.Keyword
{
    public readonly struct KeywordEffectContext
    {
        public KeywordEffectContext(
            CharBase source,
            IKeywordTriggeredSkillExecutor skillExecutor)
        {
            Source = source;
            SkillExecutor = skillExecutor;
        }

        public CharBase Source { get; }
        public IKeywordTriggeredSkillExecutor SkillExecutor { get; }
    }

    public interface IKeywordTriggeredSkillExecutor
    {
        UniTask<bool> ExecuteAsync(
            long skillID,
            CharBase caster,
            CharBase target,
            CancellationToken ct);
    }

    public interface IIncomingHitInterceptor
    {
        bool CanIntercept(CharBase owner, DamageParameter damage);
    }
}
