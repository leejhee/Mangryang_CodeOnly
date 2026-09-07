using Modules.BT.Nodes;

namespace Modules.BT.Action
{
    public interface IBTAction
    {
        BTNode.State Execute(BTContext context);
    }
}