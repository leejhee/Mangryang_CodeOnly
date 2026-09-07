using Core.Scripts.Data;
using Core.Scripts.Foundation.Define;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Keyword;
using GamePlay.Features.Battle.Scripts.Unit;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GamePlay.Features.Scripts.Keyword
{
    public static class KeywordFactory
    {
        public static KeywordBase CreateKeyword(
            KeywordData data,
            KeywordGrantData grant,
            IReadOnlyList<KeywordEffectData> effects)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            switch (data.keywordType)
            {
                case SystemEnum.eKeyword.None:
                case SystemEnum.eKeyword.eMax:
                    Debug.LogWarning(
                        $"[KeywordFactory] 생성할 수 없는 키워드 타입: {data.keywordType}({data.index})");
                    return null;
                case SystemEnum.eKeyword.ParryReady:
                    WarnUnsupportedEffects(data, effects);
                    return new ParryReadyKeyword(data, grant, effects);
                default:
                    WarnUnsupportedEffects(data, effects);
                    return new DataDrivenKeyword(data, grant, effects);
            }
        }

        /// <summary>
        /// 구현 아직 안된 / 미기획 데이터들이 많아서 추후 추가 까먹을 시 경고
        /// </summary>
        private static void WarnUnsupportedEffects(
            KeywordData data,
            IReadOnlyList<KeywordEffectData> effects)
        {
            if (effects == null)
                return;

            foreach (KeywordEffectData effect in effects)
            {
                if (effect == null ||
                    KeywordEffectFactory.IsSupported(effect.EffectType) ||
                    IsHandledAsPassiveState(data, effect))
                    continue;

                Debug.LogWarning(
                    $"[KeywordFactory] {data.keywordType}({data.index})의 " +
                    $"{effect.EffectType} 효과({effect.index})는 아직 구현되지 않았으므로, 키워드 상태만 생성.");
            }
        }

        private static bool IsHandledAsPassiveState(
            KeywordData data,
            KeywordEffectData effect)
        {
            return data.keywordType == SystemEnum.eKeyword.BanMove &&
                   effect.EffectType == SystemEnum.eEffectType.ControlMove;
        }
    }
    
    /// <summary>
    /// 키워드 발동 시 무슨 효과가 발생하는지
    /// </summary>
    public static class KeywordEffectFactory
    {
        public static bool IsSupported(SystemEnum.eEffectType effectType)
        {
            return effectType == SystemEnum.eEffectType.Dot ||
                   effectType == SystemEnum.eEffectType.TriggerSkill;
        }

        public static IKeywordEffectHandler Create(SystemEnum.eEffectType effectType)
        {
            switch (effectType)
            {
                case SystemEnum.eEffectType.Dot:
                    return DotKeywordEffectHandler.Instance;
                case SystemEnum.eEffectType.TriggerSkill:
                    return TriggerSkillKeywordEffectHandler.Instance;
                default:
                    Debug.LogWarning($"[KeywordEffectFactory] 아직 지원하지 않는 키워드 효과입니다: {effectType}");
                    return null;
            }
        }
    }

    public interface IKeywordEffectHandler
    {
        UniTask ApplyAsync(
            CharBase owner,
            KeywordBase keyword,
            KeywordEffectData effect,
            KeywordEffectContext context,
            CancellationToken ct);
    }

    internal sealed class DotKeywordEffectHandler : IKeywordEffectHandler
    {
        public static readonly DotKeywordEffectHandler Instance = new();

        private DotKeywordEffectHandler() { }

        public UniTask ApplyAsync(
            CharBase owner,
            KeywordBase keyword,
            KeywordEffectData effect,
            KeywordEffectContext context,
            CancellationToken ct)
        {
            if (!owner || owner.IsDead || keyword.Stacks <= 0)
                return UniTask.CompletedTask;

            return owner.DamageAsync(keyword.Stacks, ct);
        }
    }

    internal sealed class TriggerSkillKeywordEffectHandler : IKeywordEffectHandler
    {
        public static readonly TriggerSkillKeywordEffectHandler Instance = new();

        private TriggerSkillKeywordEffectHandler() { }

        public async UniTask ApplyAsync(
            CharBase owner,
            KeywordBase keyword,
            KeywordEffectData effect,
            KeywordEffectContext context,
            CancellationToken ct)
        {
            if (!owner || owner.IsDead || effect.ActionID <= 0)
                return;
            if (!context.Source || context.SkillExecutor == null)
            {
                Debug.LogWarning(
                    $"[KeywordEffectFactory] TriggerSkill({effect.index}) 실행 문맥이 없습니다.",
                    owner);
                return;
            }

            bool played = await context.SkillExecutor.ExecuteAsync(
                effect.ActionID,
                owner,
                context.Source,
                ct);
            if (!played)
            {
                Debug.LogWarning(
                    $"[KeywordEffectFactory] TriggerSkill({effect.index})의 스킬 " +
                    $"{effect.ActionID}을 실행하지 못했습니다.",
                    owner);
            }
        }
    }
}
