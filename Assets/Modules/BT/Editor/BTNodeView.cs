using Modules.BT.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Modules.BT.Editor
{
    public class BTNodeView : Node
    {
        public string Guid;
        public BTNodeType NodeType;
        public string Parameter;
        
        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }
        public BTNodeView(string title, BTNodeType type)
        {
            this.title = title;
            this.NodeType = type;
            this.Guid = System.Guid.NewGuid().ToString();
            this.Parameter = ""; 

            // 포트 구성
            if (type != BTNodeType.Root)
            {
                InputPort = CreatePort(Direction.Input, Port.Capacity.Single);
                InputPort.portName = "In";
                inputContainer.Add(InputPort);
                Debug.Log($"[BTNodeView] Created port. Type: {InputPort.portType}, Direction: {InputPort.direction}");
            }


            if (type is BTNodeType.Selector or BTNodeType.Sequence or BTNodeType.Root)
            {
                OutputPort = CreatePort(Direction.Output, Port.Capacity.Multi);
                OutputPort.portName = "Out";
                outputContainer.Add(OutputPort);
                Debug.Log($"[BTNodeView] Created port. Type: {OutputPort.portType}, Direction: {OutputPort.direction}");
            }


            RefreshExpandedState();
            RefreshPorts();
            
            if (type is BTNodeType.Condition or BTNodeType.Action)
            {
                var paramField = new TextField("Parameter") { value = Parameter };
                paramField.RegisterValueChangedCallback(evt => Parameter = evt.newValue);
                mainContainer.Add(paramField);
            }

            // 우클릭 삭제 메뉴
            this.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Delete Node", (a) =>
                {
                    this.RemoveFromHierarchy();
                });
            }));
            
            
        }

        private Port CreatePort(Direction dir, Port.Capacity capacity)
        {
            return Port.Create<Edge>(Orientation.Vertical, dir, capacity, typeof(bool));
        }

    }

}