#if UNITY_EDITOR
using System.Collections.Generic;
using GamePlay.Common.Scripts.Skill;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AngelBeat.Editor.Tools
{
    public static class SkillTimelineValidator
    {
        private const string SkillPrefabRoot =
            "Assets/GamePlay/Features/Battle/BattleResources/Prefabs/Skill";

        [MenuItem("Tools/Battle/Validate Skill Timelines")]
        private static void ValidateSkillTimelines()
        {
            string[] prefabGuids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { SkillPrefabRoot });

            List<string> issues = new();
            int checkedCount = 0;

            foreach (string prefabGuid in prefabGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (!prefab) continue;

                checkedCount++;
                ValidatePrefab(prefab, assetPath, issues);
            }

            if (issues.Count == 0)
            {
                Debug.Log($"[SkillTimelineValidator] PASS - Checked {checkedCount} skill prefabs.");
                return;
            }

            foreach (string issue in issues)
                Debug.LogError($"[SkillTimelineValidator] {issue}");

            Debug.LogError(
                $"[SkillTimelineValidator] FAIL - {issues.Count} issue(s) found " +
                $"in {checkedCount} skill prefabs.");
        }

        private static void ValidatePrefab(
            GameObject prefab,
            string assetPath,
            ICollection<string> issues)
        {
            if (!prefab.TryGetComponent(out SkillBase _))
                issues.Add($"Missing SkillBase: {assetPath}");

            if (!prefab.TryGetComponent(out SkillMarkerReceiver _))
                issues.Add($"Missing SkillMarkerReceiver: {assetPath}");

            if (!prefab.TryGetComponent(out PlayableDirector director))
            {
                issues.Add($"Missing PlayableDirector: {assetPath}");
                return;
            }

            if (!director.playableAsset)
            {
                issues.Add($"PlayableDirector has no playable asset: {assetPath}");
                return;
            }

            if (director.playableAsset is not TimelineAsset)
            {
                issues.Add(
                    $"PlayableDirector asset is not a TimelineAsset " +
                    $"({director.playableAsset.GetType().Name}): {assetPath}");
            }
        }
    }
}
#endif
