
namespace Modules.RoguelikeNodeMap.MapSkeleton
{
    [System.Serializable]
    public class MapPath
    {
        int _from;
        int _to;
        //BaseMapNodeData _data = null;

        public MapPath(int from, int to)
        {
            _from = from; _to = to;
        }

        //public void SetEventNode(BaseMapNodeData data) => _data = data;

    }
}