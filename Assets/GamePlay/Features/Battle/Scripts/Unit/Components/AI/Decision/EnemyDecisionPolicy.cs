namespace GamePlay.Features.Battle.Scripts.Unit.Components.AI
{
    /// <summary>
    /// 후보 생성이나 실행을 하지 않고 상태 우선순위만 결정한다.
    /// Unity 런타임 없이 빠르게 단위 테스트할 수 있는 테스트 경계다.
    /// </summary>
    public static class EnemyDecisionPolicy
    {
        public static EnemyDecisionState Decide(EnemyDecisionFacts facts)
        {
            if (facts == null || !facts.HasLivingTarget)
                return EnemyDecisionState.Wait;

            if (facts.HasLethalPush)
                return EnemyDecisionState.Push;

            if (facts.HasAttack)
                return EnemyDecisionState.Attack;

            if (facts.LowHP)
            {
                if (facts.CanRetreat)
                    return EnemyDecisionState.Retreat;
                if (facts.CanRetreatJump)
                    return EnemyDecisionState.ChangeLevel;
            }

            if (facts.HasPush)
                return EnemyDecisionState.Push;

            if (facts.ShouldJumpBeforeWalking)
                return EnemyDecisionState.ChangeLevel;

            if (facts.CanApproach)
                return EnemyDecisionState.Approach;

            return facts.CanJump
                ? EnemyDecisionState.ChangeLevel
                : EnemyDecisionState.Wait;
        }
    }
}
