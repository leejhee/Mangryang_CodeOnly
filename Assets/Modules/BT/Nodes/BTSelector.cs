namespace Modules.BT.Nodes
{
    /// <summary>
    /// '분기'가 되어주는 선택자 노드. 자식들 중 하나라도 성공이면 OK
    /// </summary>
    public class BTSelector : BTCompositeNode
    {
        public override State Evaluate(BTContext context)
        {
            foreach (BTNode child in Children)
            {
                var result = child.Evaluate(context);
                if(result != State.Failure)
                    return result;
            }
            return State.Failure;
        }
    }
}