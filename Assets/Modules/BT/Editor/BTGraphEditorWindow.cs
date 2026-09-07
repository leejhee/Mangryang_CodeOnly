#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Modules.BT.Editor
{
    public class BTGraphEditorWindow :  EditorWindow
    {
        private BTGraphView _graphView;
        private BTGraphData _targetAsset;

        [MenuItem("Window/AI/Behavior Tree Graph Editor")]
        public static void OpenEmpty()
        {
            var window = GetWindow<BTGraphEditorWindow>();
            window.titleContent = new GUIContent("Behavior Tree");
            window.Show();
        }

        public static void OpenWithAsset(BTGraphData asset)
        {
            var window = GetWindow<BTGraphEditorWindow>();
            window.titleContent = new GUIContent(asset.name);
            window._targetAsset = asset;
            window.Show();
        }

        private void OnEnable()
        {
            ConstructGraphView();
            
            // 노드 생성
            var node1 = _graphView.CreateNode("Selector", "", Guid.NewGuid().ToString());
            node1.SetPosition(new Rect(100, 200, 200, 150));
    
            var node2 = _graphView.CreateNode("Action", "RunAway", Guid.NewGuid().ToString());
            node2.SetPosition(new Rect(400, 200, 200, 150));

            _graphView.AddElement(node1);
            _graphView.AddElement(node2);

            _graphView.ConnectNodes(node1, node2);
            
            if (_targetAsset != null)
            {
                BTUtility.GetInstance(_graphView).LoadGraph(_targetAsset);
            }
        }

        private void OnDisable()
        {
            rootVisualElement.Remove(_graphView);
        }

        private void ConstructGraphView()
        {
            _graphView = new BTGraphView
            {
                name = "Behavior Tree Graph"
            };
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);
        }
    }
}

#endif