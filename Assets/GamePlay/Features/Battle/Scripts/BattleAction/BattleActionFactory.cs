namespace GamePlay.Features.Battle.Scripts.BattleAction
{
    public static class BattleActionFactory
    {
        public static BattleActionBase CreateBattleAction(BattleActionContext ctx)
        {
            return ctx.battleActionType switch
            {
                ActionType.Move => new MoveBattleAction(ctx),
                ActionType.Jump => new JumpBattleAction(ctx),
                ActionType.Push => new PushBattleAction(ctx),
                ActionType.Skill => new SkillBattleAction(ctx),
                _ => throw new System.NotImplementedException()
            };
        }
    }
}