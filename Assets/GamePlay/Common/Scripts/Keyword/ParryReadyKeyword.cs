using Core.Scripts.Data;
using Core.Scripts.Foundation.Define;
using GamePlay.Common.Scripts.Entities.Character.Components;
using GamePlay.Common.Scripts.Keyword;
using GamePlay.Features.Battle.Scripts.Unit;
using System.Collections.Generic;

namespace GamePlay.Features.Scripts.Keyword
{
    public sealed class ParryReadyKeyword : KeywordBase, IIncomingHitInterceptor
    {
        private readonly bool _hasReactionSkill;

        public ParryReadyKeyword(
            KeywordData data,
            KeywordGrantData grant,
            IReadOnlyList<KeywordEffectData> effects)
            : base(data, grant, effects)
        {
            if (effects == null)
                return;

            foreach (KeywordEffectData effect in effects)
            {
                if (effect == null ||
                    effect.EffectType != SystemEnum.eEffectType.TriggerSkill ||
                    effect.TriggerCondition != SystemEnum.eTriggerCondition.OnHit ||
                    effect.ActionID <= 0)
                {
                    continue;
                }

                _hasReactionSkill = true;
                break;
            }
        }

        bool IIncomingHitInterceptor.CanIntercept(
            CharBase owner,
            DamageParameter damage)
        {
            if (!_hasReactionSkill ||
                !owner || owner.IsDead ||
                !damage.Attacker || damage.Attacker.IsDead ||
                damage.Attacker == owner ||
                damage.Model == null)
            {
                return false;
            }

            return damage.Model.SkillType == SystemEnum.eSkillType.PhysicalAttack ||
                   damage.Model.SkillType == SystemEnum.eSkillType.MagicAttack;
        }
    }
}
