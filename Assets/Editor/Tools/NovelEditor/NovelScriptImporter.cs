using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;

[ScriptedImporter(1, "novel")]
public class NovelScriptImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        string text = System.IO.File.ReadAllText(ctx.assetPath);

        var asset = new TextAsset(text);
        ctx.AddObjectToAsset("text", asset);
        ctx.SetMainObject(asset);
    }
}
