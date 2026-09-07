using Core.Scripts.Data;
using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Entities.Character.Components;
using GamePlay.Features.Battle.Scripts.Unit;
using GamePlay.Features.Scripts.Keyword;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static Core.Scripts.Foundation.Define.SystemEnum;

namespace GamePlay.Common.Scripts.Keyword
{
    public readonly struct KeywordSnapshot
    {
        public long KeywordID { get; }
        public eKeyword KeywordType { get; }
        public string Name { get; }
        public string IconKey { get; }
        public int Stacks { get; }
        public int RemainingTurns { get; }

        public KeywordSnapshot(KeywordBase keyword)
        {
            KeywordID = keyword.KeywordID;
            KeywordType = keyword.KeywordType;
            Name = keyword.Data.KeywordName;
            IconKey = keyword.Data.keywordIcon;
            Stacks = keyword.Stacks;
            RemainingTurns = keyword.RemainingTurns;
        }
    }

    public sealed class KeywordInfo
    {
        private readonly CharBase _owner;
        private readonly Dictionary<long, KeywordBase> _keywords = new();
        private readonly List<KeywordSnapshot> _snapshots = new();

        public event Action Changed;

        public KeywordInfo(CharBase owner)
        {
            _owner = owner;
        }

        public IReadOnlyList<KeywordSnapshot> Snapshots => _snapshots;

        public bool Apply(
            KeywordData data,
            KeywordGrantData grant,
            IReadOnlyList<KeywordEffectData> effects)
        {
            if (data == null || grant == null)
                return false;
            if (data.index != grant.KeywordID)
            {
                Debug.LogError(
                    $"[KeywordInfo] KeywordData {data.index}와 Grant {grant.index}의 KeywordID가 일치하지 않습니다.");
                return false;
            }

            if (_keywords.TryGetValue(data.index, out KeywordBase existing))
            {
                int previousStacks = existing.Stacks;
                int previousRemainingTurns = existing.RemainingTurns;
                existing.Refresh(grant);
                KeywordDebugLogger.Refreshed(
                    _owner,
                    existing,
                    grant,
                    previousStacks,
                    previousRemainingTurns);
                NotifyChanged();
                return true;
            }

            KeywordBase keyword = KeywordFactory.CreateKeyword(data, grant, effects);
            if (keyword == null)
            {
                Debug.LogWarning(
                    $"[KeywordInfo] 키워드를 생성하지 못했습니다: {data.keywordType}({data.index})");
                return false;
            }

            _keywords.Add(data.index, keyword);
            KeywordDebugLogger.Added(_owner, keyword, grant);
            NotifyChanged();
            return true;
        }

        public void AddKeyword(eKeyword keyword, int stacks = 1, int duration = 1, int value = 0)
        {
            if (!DataManager.Instance.KeywordMap.TryGetValue(keyword, out KeywordData data))
            {
                Debug.LogWarning($"[KeywordInfo] 키워드 정의를 찾을 수 없습니다: {keyword}");
                return;
            }

            KeywordGrantData grant = new()
            {
                KeywordID = data.index,
                Duration = duration,
                InitStack = stacks,
                Probability = 1000,
                StackBehavior = eStackBehavior.Refresh
            };
            Apply(data, grant, DataManager.Instance.GetKeywordEffects(data.index));
        }

        public async UniTask TriggerAsync(eTriggerCondition condition, CancellationToken ct)
        {
            List<KeywordBase> activeKeywords = new(_keywords.Values);
            List<long> removals = null;

            foreach (KeywordBase keyword in activeKeywords)
            {
                ct.ThrowIfCancellationRequested();
                await keyword.TriggerAsync(_owner, condition, ct);

                if (condition != eTriggerCondition.EndOfTurn)
                    continue;

                int previousRemainingTurns = keyword.RemainingTurns;
                bool expired = keyword.ConsumeTurn();
                if (previousRemainingTurns != keyword.RemainingTurns)
                    KeywordDebugLogger.DurationChanged(_owner, keyword, previousRemainingTurns);

                if (expired)
                {
                    removals ??= new List<long>();
                    removals.Add(keyword.KeywordID);
                }
            }

            if (removals != null)
            {
                foreach (long keywordID in removals)
                {
                    if (!_keywords.TryGetValue(keywordID, out KeywordBase keyword))
                        continue;

                    if (_keywords.Remove(keywordID))
                        KeywordDebugLogger.Removed(_owner, keyword, "DurationExpired");
                }
            }

            if (activeKeywords.Count > 0)
                NotifyChanged();
        }

        public async UniTask<bool> TryInterceptIncomingHitAsync(
            DamageParameter damage,
            IKeywordTriggeredSkillExecutor skillExecutor,
            CancellationToken ct)
        {
            List<KeywordBase> activeKeywords = new(_keywords.Values);
            foreach (KeywordBase keyword in activeKeywords)
            {
                ct.ThrowIfCancellationRequested();
                if (keyword is not IIncomingHitInterceptor interceptor ||
                    !interceptor.CanIntercept(_owner, damage))
                {
                    continue;
                }

                if (!_keywords.Remove(keyword.KeywordID))
                    continue;

                KeywordDebugLogger.Intercepted(
                    _owner,
                    keyword,
                    damage.Attacker,
                    damage.Model?.SkillIndex ?? 0);
                KeywordDebugLogger.Removed(_owner, keyword, "ConsumedOnHit");
                NotifyChanged();

                KeywordEffectContext context = new(
                    damage.Attacker,
                    skillExecutor);
                await keyword.TriggerAsync(
                    _owner,
                    eTriggerCondition.OnHit,
                    context,
                    ct);
                return true;
            }

            return false;
        }

        public bool HasKeyword(eKeyword keyword) => Find(keyword) != null;

        public bool RemoveKeyword(eKeyword keyword)
        {
            long keywordID = 0;
            KeywordBase keywordToRemove = null;
            bool found = false;
            foreach (KeyValuePair<long, KeywordBase> entry in _keywords)
            {
                if (entry.Value.KeywordType != keyword)
                    continue;

                keywordID = entry.Key;
                keywordToRemove = entry.Value;
                found = true;
                break;
            }

            if (!found || !_keywords.Remove(keywordID))
                return false;

            KeywordDebugLogger.Removed(_owner, keywordToRemove, "Explicit");
            NotifyChanged();
            return true;
        }

        public int GetKeywordCount(eKeyword keyword) =>
            Find(keyword)?.Stacks ?? 0;

        public int GetKeywordValue(eKeyword keyword) =>
            Find(keyword)?.RemainingTurns ?? 0;

        public int GetRemainingTurns(eKeyword keyword) =>
            Find(keyword)?.RemainingTurns ?? 0;

        public void Clear()
        {
            if (_keywords.Count == 0)
                return;

            foreach (KeywordBase keyword in _keywords.Values)
                KeywordDebugLogger.Removed(_owner, keyword, "Clear");

            _keywords.Clear();
            NotifyChanged();
        }

        private KeywordBase Find(eKeyword keyword)
        {
            if (keyword == eKeyword.None)
                return null;

            foreach (KeywordBase active in _keywords.Values)
            {
                if (active.KeywordType == keyword)
                    return active;
            }

            return null;
        }

        private void NotifyChanged()
        {
            _snapshots.Clear();
            foreach (KeywordBase keyword in _keywords.Values)
                _snapshots.Add(new KeywordSnapshot(keyword));

            _snapshots.Sort(CompareSnapshot);
            Changed?.Invoke();
        }

        private static int CompareSnapshot(KeywordSnapshot lhs, KeywordSnapshot rhs) =>
            lhs.KeywordID.CompareTo(rhs.KeywordID);
    }

    /// <summary>
    /// 에디터와 Development Build에서만 키워드 수명주기를 출력합니다.
    /// Release Build에서는 Conditional 특성에 의해 호출 자체가 제거됩니다.
    /// </summary>
    internal static class KeywordDebugLogger
    {
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void ResolveSkill(
            CharBase caster,
            long skillID,
            int grantCount,
            int targetCount)
        {
            Debug.Log(
                $"[Keyword][Resolve] source=Skill({skillID}) caster={GetCharacterLabel(caster)} " +
                $"grants={grantCount} targets={targetCount}",
                caster);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Added(
            CharBase owner,
            KeywordBase keyword,
            KeywordGrantData grant)
        {
            Debug.Log(
                $"[Keyword][Add] target={GetCharacterLabel(owner)} " +
                $"keyword={GetKeywordLabel(keyword)} grant={grant.index} " +
                $"source={grant.SourceType}({grant.SouceRefID}) " +
                $"stacks={keyword.Stacks} remainingTurns={keyword.RemainingTurns}",
                owner);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Refreshed(
            CharBase owner,
            KeywordBase keyword,
            KeywordGrantData grant,
            int previousStacks,
            int previousRemainingTurns)
        {
            Debug.Log(
                $"[Keyword][Refresh] target={GetCharacterLabel(owner)} " +
                $"keyword={GetKeywordLabel(keyword)} grant={grant.index} " +
                $"behavior={grant.StackBehavior} stacks={previousStacks}->{keyword.Stacks} " +
                $"remainingTurns={previousRemainingTurns}->{keyword.RemainingTurns}",
                owner);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void EffectTriggered(
            CharBase owner,
            KeywordBase keyword,
            KeywordEffectData effect)
        {
            Debug.Log(
                $"[Keyword][Effect] target={GetCharacterLabel(owner)} " +
                $"keyword={GetKeywordLabel(keyword)} effect={effect.EffectType}({effect.index}) " +
                $"trigger={effect.TriggerCondition} stacks={keyword.Stacks} " +
                $"remainingTurns={keyword.RemainingTurns}",
                owner);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Intercepted(
            CharBase owner,
            KeywordBase keyword,
            CharBase attacker,
            long incomingSkillID)
        {
            Debug.Log(
                $"[Keyword][Intercept] target={GetCharacterLabel(owner)} " +
                $"keyword={GetKeywordLabel(keyword)} attacker={GetCharacterLabel(attacker)} " +
                $"incomingSkill={incomingSkillID}",
                owner);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void TriggeredSkill(
            CharBase caster,
            long skillID,
            CharBase target)
        {
            Debug.Log(
                $"[Keyword][TriggerSkill] caster={GetCharacterLabel(caster)} " +
                $"skill={skillID} target={GetCharacterLabel(target)}",
                caster);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void DurationChanged(
            CharBase owner,
            KeywordBase keyword,
            int previousRemainingTurns)
        {
            Debug.Log(
                $"[Keyword][Duration] target={GetCharacterLabel(owner)} " +
                $"keyword={GetKeywordLabel(keyword)} " +
                $"remainingTurns={previousRemainingTurns}->{keyword.RemainingTurns}",
                owner);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Removed(
            CharBase owner,
            KeywordBase keyword,
            string reason)
        {
            Debug.Log(
                $"[Keyword][Remove] target={GetCharacterLabel(owner)} " +
                $"keyword={GetKeywordLabel(keyword)} reason={reason} " +
                $"stacks={keyword.Stacks} remainingTurns={keyword.RemainingTurns}",
                owner);
        }

        private static string GetCharacterLabel(CharBase character)
        {
            return character
                ? $"{character.name}(uid:{character.GetID()})"
                : "<none>";
        }

        private static string GetKeywordLabel(KeywordBase keyword) =>
            $"{keyword.KeywordType}({keyword.KeywordID})";
    }
}
