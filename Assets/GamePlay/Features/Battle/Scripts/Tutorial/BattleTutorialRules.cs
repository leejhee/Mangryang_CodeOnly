namespace GamePlay.Features.Battle.Scripts.Tutorial
{
    public enum TutorialHitRule
    {
        None = 0,   // 기본 규칙
        AlwaysHit,  // 무조건 맞음
        AlwaysEvade // 무조건 회피
    }
    
    public static class BattleTutorialRules
    {
        public static TutorialHitRule HitRule { get; set; } = TutorialHitRule.None;
    }
}