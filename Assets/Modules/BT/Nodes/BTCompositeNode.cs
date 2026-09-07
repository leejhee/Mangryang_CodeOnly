using System.Collections.Generic;

namespace Modules.BT.Nodes
{
    /// <summary>
    /// BTNode를 합성관계로 갖는 노드
    /// </summary>
    public abstract class BTCompositeNode : BTNode
    {
        protected List<BTNode> Children = new();

        public void AddChild(BTNode child)
        {
            Children.Add(child);
        }

        public void SetChildren(List<BTNode> children)
        {
            Children = children;
        }
    }
}