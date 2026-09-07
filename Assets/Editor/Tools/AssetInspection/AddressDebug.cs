using Cysharp.Threading.Tasks;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.AddressableAssets;


public static class AddressDebug
{
    /// <summary>
    /// 키에 연결된 것 찾기
    /// </summary>
    /// <param name="key">키!</param>
    public static async UniTask DumpKeyAsync(object key)
    {
        var locationsHandle = Addressables.LoadResourceLocationsAsync(key, typeof(GameObject));
        var locations = await locationsHandle.Task;
        if (locations == null || locations.Count == 0)
        {
            Debug.LogError($"[AddrDump] No locations for key '{key}'");
            return;
        }

        foreach (var loc in locations)
        {
            Debug.Log(
                $"[AddrDump] Key='{key}' -> " +
                $"PrimaryKey='{loc.PrimaryKey}', " +
                $"Provider='{loc.ResourceType?.Name}', " +
                $"Id='{loc.InternalId}', " +
                $"DepCount={loc.Dependencies?.Count ?? 0}"
            );
        }
        Addressables.Release(locationsHandle);

        var goHandle = Addressables.InstantiateAsync(key.ToString());
        var go = await goHandle.Task;
        Debug.Log($"[AddrDump] Instantiated: {(go ? go.name : "NULL")}");
        if (go)
        {
            var hasPlayer = go.GetComponentInChildren<NovelPlayer>(true) != null;
            Debug.Log($"[AddrDump] Has NovelPlayer component: {hasPlayer}");
            Addressables.ReleaseInstance(go);
        }
    }
    
    [MenuItem("Tools/AssetInspection/Dump Address to Asset")]
    public static void Dump()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (!settings) { Debug.LogError("No Addressables settings."); return; }

        StringBuilder sb = new();
        foreach (var group in settings.groups)
        {
            if (!group) continue;
            foreach (var e in group.entries)
            {
                sb.AppendLine($"[AddrMap] Group='{group.name}' Address='{e.address}' Path='{e.AssetPath}' Labels='{string.Join(",", e.labels)}'");
            }
        }
        
        Debug.Log(sb.ToString());
    }
}
