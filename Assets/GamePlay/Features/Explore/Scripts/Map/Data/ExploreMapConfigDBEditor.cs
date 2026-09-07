#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GamePlay.Features.Explore.Scripts.Map.Data
{
    [CustomEditor(typeof(ExploreMapConfigDB))]
    public class ExploreMapConfigDBEditor : Editor
    {
        private SerializedProperty mapConfigsProp;
        private SerializedProperty pairsProp;

        void OnEnable()
        {
            mapConfigsProp = serializedObject.FindProperty("mapConfigs");
            pairsProp = mapConfigsProp.FindPropertyRelative("pairs");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Explore Map Configurations", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (pairsProp != null && pairsProp.isArray)
            {
                for (int i = 0; i < pairsProp.arraySize; i++)
                {
                    DrawConfigElement(pairsProp.GetArrayElementAtIndex(i), i);
                }

                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Config"))
                {
                    pairsProp.InsertArrayElementAtIndex(pairsProp.arraySize);
                }

                if (GUILayout.Button("Remove Last") && pairsProp.arraySize > 0)
                {
                    pairsProp.DeleteArrayElementAtIndex(pairsProp.arraySize - 1);
                }

                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConfigElement(SerializedProperty element, int index)
        {
            var keyProp = element.FindPropertyRelative("key");
            var valueProp = element.FindPropertyRelative("value");

            if (keyProp != null)
            {
                var dungeonProp = keyProp.FindPropertyRelative("dungeon");
                var floorProp = keyProp.FindPropertyRelative("floor");

                string dungeonName = dungeonProp.enumDisplayNames[dungeonProp.enumValueIndex];
                int floor = floorProp.intValue;
                string label = $"{dungeonName} - Floor {floor}";

                element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, label, true);

                if (element.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(keyProp, new GUIContent("Key"));
                    EditorGUILayout.PropertyField(valueProp, new GUIContent("Config"));
                    EditorGUI.indentLevel--;
                }
            }
        }
    }

}
#endif