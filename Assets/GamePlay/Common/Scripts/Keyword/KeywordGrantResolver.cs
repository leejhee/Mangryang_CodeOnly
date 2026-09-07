using Core.Scripts.Data;
using Core.Scripts.Foundation.Define;
using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Contracts;
using GamePlay.Features.Battle.Scripts.Unit;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GamePlay.Common.Scripts.Keyword
{
    public sealed class KeywordGrantResolver
    {
        private const int CertainProbability = 1000;

        public UniTask ApplySkillGrantsAsync(
            long skillID,
            CharBase caster,
            IReadOnlyList<IDamageable> targets,
            CancellationToken ct)
        {
            IReadOnlyList<KeywordGrantData> grants = DataManager.Instance.GetKeywordGrants(
                SystemEnum.eSourceType.Skill,
                skillID);
            KeywordDebugLogger.ResolveSkill(
                caster,
                skillID,
                grants.Count,
                targets?.Count ?? 0);

            foreach (KeywordGrantData grant in grants)
            {
                ct.ThrowIfCancellationRequested();
                if (grant.Probability != CertainProbability)
                {
                    Debug.LogWarning(
                        $"[KeywordGrantResolver] 확률 {grant.Probability}은 Twister 수직 슬라이스에서 지원하지 않습니다. " +
                        $"Grant {grant.index}를 건너뜁니다.");
                    continue;
                }

                KeywordData keyword = DataManager.Instance.GetData<KeywordData>(grant.KeywordID);
                if (keyword == null)
                    continue;

                IReadOnlyList<KeywordEffectData> effects =
                    DataManager.Instance.GetKeywordEffects(grant.KeywordID);

                switch (grant.ApplyTarget)
                {
                    case SystemEnum.eApplyTarget.TARGET_SELF:
                        caster?.KeywordInfo?.Apply(keyword, grant, effects);
                        break;
                    case SystemEnum.eApplyTarget.TARGET_ENEMY:
                        ApplyToCharacters(targets, keyword, grant, effects);
                        break;
                    default:
                        Debug.LogWarning(
                            $"[KeywordGrantResolver] 아직 지원하지 않는 부여 대상입니다: {grant.ApplyTarget}");
                        break;
                }
            }

            return UniTask.CompletedTask;
        }

        private static void ApplyToCharacters(
            IReadOnlyList<IDamageable> targets,
            KeywordData keyword,
            KeywordGrantData grant,
            IReadOnlyList<KeywordEffectData> effects)
        {
            if (targets == null)
                return;

            foreach (IDamageable target in targets)
            {
                if (target is CharBase character && character && !character.IsDead)
                    character.KeywordInfo?.Apply(keyword, grant, effects);
            }
        }
    }
}
