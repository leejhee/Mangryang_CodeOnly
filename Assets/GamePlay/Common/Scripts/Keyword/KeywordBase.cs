using Core.Scripts.Data;
using Core.Scripts.Foundation.Define;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Keyword;
using GamePlay.Features.Battle.Scripts.Unit;
using System;
using System.Collections.Generic;
using System.Threading;

namespace GamePlay.Features.Scripts.Keyword
{
    public abstract class KeywordBase
    {
        private readonly IReadOnlyList<KeywordEffectData> _effects;

        protected KeywordBase(
            KeywordData data,
            KeywordGrantData grant,
            IReadOnlyList<KeywordEffectData> effects)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            _effects = effects ?? Array.Empty<KeywordEffectData>();
            ApplyInitialGrant(grant ?? throw new ArgumentNullException(nameof(grant)));
        }

        public KeywordData Data { get; }
        public long KeywordID => Data.index;
        public SystemEnum.eKeyword KeywordType => Data.keywordType;
        public int Stacks { get; private set; }
        public int RemainingTurns { get; private set; }

        public void Refresh(KeywordGrantData grant)
        {
            if (grant == null)
                throw new ArgumentNullException(nameof(grant));

            switch (grant.StackBehavior)
            {
                case SystemEnum.eStackBehavior.Refresh:
                    Stacks = NormalizeStacks(grant.InitStack);
                    RemainingTurns = NormalizeDuration(grant.Duration);
                    break;
                case SystemEnum.eStackBehavior.Cumulative:
                    Stacks = NormalizeStacks(Stacks + grant.InitStack);
                    RemainingTurns = Data.RefreshPolicy == SystemEnum.eRefreshPolicy.Extend
                        ? NormalizeDuration(RemainingTurns + grant.Duration)
                        : Math.Max(RemainingTurns, NormalizeDuration(grant.Duration));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(grant.StackBehavior), grant.StackBehavior, null);
            }
        }

        public async UniTask TriggerAsync(
            CharBase owner,
            SystemEnum.eTriggerCondition condition,
            CancellationToken ct)
        {
            await TriggerAsync(owner, condition, default, ct);
        }

        public async UniTask TriggerAsync(
            CharBase owner,
            SystemEnum.eTriggerCondition condition,
            KeywordEffectContext context,
            CancellationToken ct)
        {
            foreach (KeywordEffectData effect in _effects)
            {
                ct.ThrowIfCancellationRequested();
                if (effect.TriggerCondition != condition)
                    continue;

                IKeywordEffectHandler handler = KeywordEffectFactory.Create(effect.EffectType);
                if (handler == null)
                    continue;

                KeywordDebugLogger.EffectTriggered(owner, this, effect);
                await handler.ApplyAsync(owner, this, effect, context, ct);
            }
        }

        public bool ConsumeTurn()
        {
            if (Data.RemovePolicy != SystemEnum.eRemovePolicy.ByDuration || RemainingTurns < 0)
                return false;

            RemainingTurns = Math.Max(0, RemainingTurns - 1);
            return RemainingTurns == 0;
        }

        private void ApplyInitialGrant(KeywordGrantData grant)
        {
            Stacks = NormalizeStacks(grant.InitStack);
            RemainingTurns = NormalizeDuration(grant.Duration);
        }

        private int NormalizeStacks(int stacks)
        {
            if (!Data.Stackable)
                return 1;

            int maxStack = Data.MaxStack > 0 ? Data.MaxStack : int.MaxValue;
            return Math.Min(Math.Max(1, stacks), maxStack);
        }

        private static int NormalizeDuration(int duration) => Math.Max(-1, duration);
    }
}
