using Modules.BT.Condition;
using System;

namespace Modules.BT.Nodes
{
    public class BTCondition : BTNode
    {
        private readonly IBTCondition _condition;

        public BTCondition(IBTCondition condition)
        {
            _condition = condition;
        }

        public override State Evaluate(BTContext context)
        {
            return _condition.Evaluate(context) ? State.Success : State.Failure;
        }
    }
}