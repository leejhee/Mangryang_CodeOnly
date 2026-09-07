using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Modules.BT.Editor
{
    public static class BTGraphDataEditor
    {
        [OnOpenAsset(1)]
        public static bool OnOpen(int instanceID, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID) as BTGraphData;
            if (obj != null)
            {
                BTGraphEditorWindow.OpenWithAsset(obj);
                return true;
            }

            return false;
        }
        
        [MenuItem("Window/AI/Behavior Tree Graph Editor")]
        public static void OpenEmpty()
        {
            var asset = ScriptableObject.CreateInstance<BTGraphData>();
            AssetDatabase.CreateAsset(asset, "Assets/NewBTGraphData.asset");
            AssetDatabase.SaveAssets();

            BTGraphEditorWindow.OpenWithAsset(asset);
        }
    }
}