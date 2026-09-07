using Modules.BT.Action;
using System;

namespace Modules.BT.Nodes
{
    public class BTAction : BTNode
    {
        private readonly IBTAction _action;

        public BTAction(IBTAction action)
        {
            _action = action;
        }

        public override State Evaluate(BTContext context)
        {
            return _action.Execute(context);
        }
    }
}