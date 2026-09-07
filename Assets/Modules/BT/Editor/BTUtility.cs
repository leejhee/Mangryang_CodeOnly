using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Modules.BT.Editor
{
    public class BTUtility
    {
        private BTGraphView _graphView;
        private BTGraphData _targetData;

        public static BTUtility GetInstance(BTGraphView view)
        {
            return new BTUtility { _graphView = view };
        }

        public void SaveGraph(BTGraphData dataAsset)
        {
            _targetData = dataAsset;
            _targetData.nodes.Clear();
            _targetData.links.Clear();

            // Save Nodes
            foreach (var node in _graphView.nodes.OfType<BTNodeView>())
            {
                var nodeData = new BTNodeData
                {
                    guid = node.Guid,
                    nodeType = node.NodeType.ToString(),
                    parameter = node.Parameter,
                    position = node.GetPosition().position,
                };
                _targetData.nodes.Add(nodeData);
            }

            // Save Links
            var edges = _graphView.edges.ToList();
            foreach (var edge in edges)
            {
                if (edge.output.node is not BTNodeView outputNode || edge.input.node is not BTNodeView inputNode)
                    continue;

                var link = new BTNodeLinkData
                {
                    outputNodeGuid = outputNode.Guid,
                    inputNodeGuid = inputNode.Guid
                };
                _targetData.links.Add(link);
            }

            EditorUtility.SetDirty(_targetData);
            AssetDatabase.SaveAssets();
        }

        public void LoadGraph(BTGraphData dataAsset)
        {
            _targetData = dataAsset;

            _graphView.ClearGraph(); 

            // Load Nodes
            foreach (var nodeData in _targetData.nodes)
            {
                var node = _graphView.CreateNode(nodeData.nodeType, nodeData.parameter, nodeData.guid);
                node.SetPosition(new Rect(nodeData.position, new Vector2(200, 150)));
            }

            // Load Edges
            foreach (var link in _targetData.links)
            {
                var outputNode = _graphView.GetNodeByGuid(link.outputNodeGuid);
                var inputNode = _graphView.GetNodeByGuid(link.inputNodeGuid);

                _graphView.ConnectNodes(outputNode, inputNode);
            }
        }
    }
}