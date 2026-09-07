using Modules.BT.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq;
using Edge = UnityEditor.Experimental.GraphView.Edge;

namespace Modules.BT.Editor
{
    public class BTGraphView : GraphView
    {
        public BTGraphView()
        {
            // 줌 & 인터랙션
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // 그리드 배경
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            
            var rootNode = CreateNode("Root", "", System.Guid.NewGuid().ToString());
            rootNode.SetPosition(new Rect(100, 200, 200, 150));
            AddElement(rootNode);
        }
        
        private void AddNodeAt(BTNodeType type, Vector2 pos)
        {
            var node = CreateNode(type.ToString(), "", Guid.NewGuid().ToString());
            node.SetPosition(new Rect(pos, new Vector2(200, 150)));
            AddElement(node);
        }
        
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            Vector2 mousePos = evt.localMousePosition;

            evt.menu.AppendAction("Add Root", (_) => AddNodeAt(BTNodeType.Root, mousePos));
            evt.menu.AppendAction("Add Selector", (_) => AddNodeAt(BTNodeType.Selector, mousePos));
            evt.menu.AppendAction("Add Sequence", (_) => AddNodeAt(BTNodeType.Sequence, mousePos));
            evt.menu.AppendAction("Add Condition", (_) => AddNodeAt(BTNodeType.Condition, mousePos));
            evt.menu.AppendAction("Add Action", (_) => AddNodeAt(BTNodeType.Action, mousePos));
        }
        
        // 노드 생성
        public BTNodeView CreateNode(string nodeType, string parameter = "", string guid = null)
        {
            var type = Enum.TryParse<BTNodeType>(nodeType, out var parsed) ? parsed : BTNodeType.Action;
            var node = new BTNodeView(nodeType, type)
            {
                Guid = guid ?? Guid.NewGuid().ToString(),
                Parameter = parameter
            };

            AddElement(node);
            node.RefreshPorts();
            node.RefreshExpandedState();
            return node;
        }
        
        // 노드 검색
        public BTNodeView GetNodeByGuid(string guid)
        {
            return nodes.OfType<BTNodeView>().FirstOrDefault(n => n.Guid == guid);
        }

        // 노드 연결
        public void ConnectNodes(BTNodeView from, BTNodeView to)
        {
            Port outputPort = from.OutputPort;
            Port inputPort = to.InputPort;

            if (outputPort == null || inputPort == null)
            {
                Debug.LogWarning("포트가 null이라 연결 불가");
                return;
            }
                

            var edge = new Edge
            {
                output = outputPort,
                input = inputPort
            };

            edge.input.Connect(edge);
            edge.output.Connect(edge);

            AddElement(edge);
        }

        // 그래프 초기화
        public void ClearGraph()
        {
            graphElements.ToList().ForEach(RemoveElement);
        }
    }

}