using Core.Scripts.Foundation.Define;
using Cysharp.Threading.Tasks;
using GamePlay.Features.Battle.Scripts.Unit;
using System.Threading;

namespace GamePlay.Features.Battle.Scripts.BattleTurn
{
    public sealed class BattleKeywordTurnProcessor
    {
        public UniTask ProcessAsync(TurnEventContext context, CancellationToken ct)
        {
            CharBase actor = context?.Actor;
            if (!actor || actor.IsDead || actor.KeywordInfo == null)
                return UniTask.CompletedTask;

            return actor.KeywordInfo.TriggerAsync(SystemEnum.eTriggerCondition.EndOfTurn, ct);
        }
    }
}
