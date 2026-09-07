#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


public class RPCheck
{
    [MenuItem("Tools/RenderPipeline/Print Status")]
    public static void PrintStatus()
    {
        var rp = GraphicsSettings.currentRenderPipeline;
        Debug.Log($"Current RP: {(rp ? rp.GetType().Name : "Built-in")}");

#if UNITY_RENDER_PIPELINE_UNIVERSAL
    Debug.Log("Symbol: UNITY_RENDER_PIPELINE_UNIVERSAL = defined");
#else
        Debug.Log("Symbol: UNITY_RENDER_PIPELINE_UNIVERSAL = NOT defined");
#endif
    }
}
#endif