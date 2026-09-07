
namespace Modules.BT.Nodes
{
    public abstract class BTDecorator : BTNode
    {
        protected BTNode Child;

        public void SetChild(BTNode child)
        {
            Child = child;
        }
    }
}