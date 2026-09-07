namespace Modules.BT.Nodes
{
    public enum BTNodeType
    {
        Root,
        Selector,
        Sequence,
        Condition,
        Action
    }
    
    public abstract class BTNode
    {
        public enum State
        {
            Success,
            Failure,
            Running
        }
        
        public abstract State Evaluate(BTContext context);
    }
}

