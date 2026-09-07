using Core.Scripts.Foundation.Define;
using GamePlay.Features.Battle.Scripts.Unit;

namespace GamePlay.Features.Battle.Scripts.BattleAction
{
    public static class BattleActionRestriction
    {
        public static bool IsBlocked(CharBase actor, ActionType actionType)
        {
            if (!actor || actor.KeywordInfo == null)
                return false;
            if (actionType != ActionType.Move && actionType != ActionType.Jump)
                return false;

            return actor.KeywordInfo.HasKeyword(SystemEnum.eKeyword.BanMove);
        }
    }
}
