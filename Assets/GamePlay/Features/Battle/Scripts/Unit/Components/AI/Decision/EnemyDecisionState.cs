namespace GamePlay.Features.Battle.Scripts.Unit.Components.AI
{
    /// <summary>한 턴에 적 AI가 선택하는 상위 수준의 의도.</summary>
    public enum EnemyDecisionState
    {
        Attack,
        Push,
        Retreat,
        ChangeLevel,
        Approach,
        Wait
    }
}
