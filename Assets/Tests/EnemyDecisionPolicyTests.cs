using GamePlay.Features.Battle.Scripts.Unit.Components.AI;
using NUnit.Framework;

public class EnemyDecisionPolicyTests
{
    [Test]
    public void NoLivingTarget_AlwaysWaits()
    {
        EnemyDecisionFacts facts = AllOptions();
        facts.HasLivingTarget = false;

        Assert.That(EnemyDecisionPolicy.Decide(facts), Is.EqualTo(EnemyDecisionState.Wait));
    }

    [Test]
    public void LethalPush_HasPriorityOverAttack()
    {
        EnemyDecisionFacts facts = BaseFacts();
        facts.HasLethalPush = true;
        facts.HasAttack = true;

        Assert.That(EnemyDecisionPolicy.Decide(facts), Is.EqualTo(EnemyDecisionState.Push));
    }

    [Test]
    public void Attack_HasPriorityOverLowHpRetreat()
    {
        EnemyDecisionFacts facts = BaseFacts();
        facts.HasAttack = true;
        facts.LowHP = true;
        facts.CanRetreat = true;

        Assert.That(EnemyDecisionPolicy.Decide(facts), Is.EqualTo(EnemyDecisionState.Attack));
    }

    [Test]
    public void LowHpWithoutAttack_RetreatsBeforeNormalPush()
    {
        EnemyDecisionFacts facts = BaseFacts();
        facts.LowHP = true;
        facts.CanRetreat = true;
        facts.HasPush = true;

        Assert.That(EnemyDecisionPolicy.Decide(facts), Is.EqualTo(EnemyDecisionState.Retreat));
    }

    [Test]
    public void LowHpWithoutWalkRetreat_UsesRetreatJump()
    {
        EnemyDecisionFacts facts = BaseFacts();
        facts.LowHP = true;
        facts.CanRetreatJump = true;
        facts.HasPush = true;

        Assert.That(EnemyDecisionPolicy.Decide(facts), Is.EqualTo(EnemyDecisionState.ChangeLevel));
    }

    [Test]
    public void NormalPush_HasPriorityOverRepositioning()
    {
        EnemyDecisionFacts facts = BaseFacts();
        facts.HasPush = true;
        facts.ShouldJumpBeforeWalking = true;
        facts.CanApproach = true;

        Assert.That(EnemyDecisionPolicy.Decide(facts), Is.EqualTo(EnemyDecisionState.Push));
    }

    [Test]
    public void RequiredLevelChange_HasPriorityOverWalking()
    {
        EnemyDecisionFacts facts = BaseFacts();
        facts.ShouldJumpBeforeWalking = true;
        facts.CanApproach = true;

        Assert.That(EnemyDecisionPolicy.Decide(facts), Is.EqualTo(EnemyDecisionState.ChangeLevel));
    }

    [Test]
    public void Approach_HasPriorityOverOptionalJump()
    {
        EnemyDecisionFacts facts = BaseFacts();
        facts.CanApproach = true;
        facts.CanJump = true;

        Assert.That(EnemyDecisionPolicy.Decide(facts), Is.EqualTo(EnemyDecisionState.Approach));
    }

    [Test]
    public void Jump_IsUsedWhenWalkingCannotImprovePosition()
    {
        EnemyDecisionFacts facts = BaseFacts();
        facts.CanJump = true;

        Assert.That(EnemyDecisionPolicy.Decide(facts), Is.EqualTo(EnemyDecisionState.ChangeLevel));
    }

    [Test]
    public void NoValidAction_Waits()
    {
        Assert.That(
            EnemyDecisionPolicy.Decide(BaseFacts()),
            Is.EqualTo(EnemyDecisionState.Wait));
    }

    [Test]
    public void NullFacts_WaitsInsteadOfThrowing()
    {
        Assert.That(
            EnemyDecisionPolicy.Decide(null),
            Is.EqualTo(EnemyDecisionState.Wait));
    }

    private static EnemyDecisionFacts BaseFacts() => new()
    {
        HasLivingTarget = true
    };

    private static EnemyDecisionFacts AllOptions() => new()
    {
        HasLivingTarget = true,
        HasLethalPush = true,
        HasAttack = true,
        LowHP = true,
        CanRetreat = true,
        CanRetreatJump = true,
        HasPush = true,
        ShouldJumpBeforeWalking = true,
        CanApproach = true,
        CanJump = true
    };
}
