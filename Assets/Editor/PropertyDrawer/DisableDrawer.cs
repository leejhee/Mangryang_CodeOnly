using Core.Attributes;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;



[CustomPropertyDrawer(typeof(CustomDisableAttribute))]
public class DisableDrawer : PropertyDrawer
{
    private static GUIStyle style = new GUIStyle(EditorStyles.label);
    private static Texture2D texture = new Texture2D(1, 1);
    private bool isEven = false;

    private CustomDisableAttribute Attr { get { return (CustomDisableAttribute)attribute; } }


    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float totalHeight = 0f;
        var singleLineHeight = base.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing;

        int parentdepth = property.depth;
        var iter = property.Copy();
        var end = property.GetEndProperty();
        bool enterChildren = true;

        while(iter.NextVisible(enterChildren) &&
            !SerializedProperty.EqualContents(iter, end))
        {
            if(iter.depth > parentdepth + 1)
            {
                enterChildren = false;
                continue;
            }
            totalHeight += EditorGUI.GetPropertyHeight(iter, null, true) + EditorGUIUtility.standardVerticalSpacing;
            enterChildren = false;
        }

        return totalHeight;

    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!Attr.IsInitialized)
        {
            isEven = false;
            if (texture != null)
            {
                Color backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.22f, 0.22f, 0.22f, 1f)  
                    : new Color(0.76f, 0.76f, 0.76f, 1f); 

                texture.SetPixel(0, 0, backgroundColor);
                texture.Apply();
            }
            Attr.IsInitialized = true;
            return;
        }

        if (isEven)
            style.normal.background = texture;
        else
            style.normal.background = null;
        isEven = !isEven;

        position.y -= EditorGUIUtility.standardVerticalSpacing / 2;
        EditorGUI.LabelField(position, GUIContent.none, style);

        EditorGUI.BeginDisabledGroup(true);
        EditorGUI.BeginProperty(position, label, property);
        int parentDepth = property.depth;
        var iter = property.Copy();
        var end = property.GetEndProperty();

        bool enterChildren = true;

        while (iter.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iter, end))
        {
            if (iter.depth > parentDepth + 1)
            {
                enterChildren = false;
                continue;
            }
        
            float height = EditorGUI.GetPropertyHeight(iter, null, true);
            var rect = new Rect(position.x, position.y, position.width, height);
        
            EditorGUI.PropertyField(rect, iter, true);
            position.y += height + EditorGUIUtility.standardVerticalSpacing;
        
            enterChildren = false;
        }

        EditorGUI.EndProperty();

        EditorGUI.EndDisabledGroup();
    }
}