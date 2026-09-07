namespace GamePlay.Features.Battle.Scripts.Unit.Components.AI
{
    /// <summary>
    /// 월드 객체와 무관한 순차 판단 입력. 플래너가 계산한 행동 가능 여부만 담는다.
    /// </summary>
    public sealed class EnemyDecisionFacts
    {
        public bool HasLivingTarget { get; set; }
        public bool HasLethalPush { get; set; }
        public bool HasAttack { get; set; }
        public bool LowHP { get; set; }
        public bool CanRetreat { get; set; }
        public bool CanRetreatJump { get; set; }
        public bool HasPush { get; set; }
        public bool ShouldJumpBeforeWalking { get; set; }
        public bool CanApproach { get; set; }
        public bool CanJump { get; set; }
    }
}
