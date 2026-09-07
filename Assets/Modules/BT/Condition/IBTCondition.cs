namespace Modules.BT.Condition
{
    public interface IBTCondition
    {
        bool Evaluate(BTContext context);
    }
}