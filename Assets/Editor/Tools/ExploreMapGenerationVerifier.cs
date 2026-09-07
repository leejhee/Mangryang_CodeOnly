using Core.Scripts.Foundation.Utils;
using GamePlay.Features.Explore.Scripts.Map.Data;
using GamePlay.Features.Explore.Scripts.Map.Logic;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AngelBeat.Editor.Tools
{
    /// <summary>
    /// 프로젝트에 등록된 모든 절차적 탐사 설정을 다중 시드로 검증한다.
    /// 메뉴 또는 Unity -executeMethod에서 동일하게 실행할 수 있다.
    /// </summary>
    public static class ExploreMapGenerationVerifier
    {
        private const int SeedsPerConfig = 100;
        private const ulong VerificationSeed = 0xA11CE5EEDUL;

        [MenuItem("AngelBeat/Validation/Verify Procedural Explore Maps")]
        public static void RunFromMenu()
        {
            RunBatch();
            EditorUtility.DisplayDialog(
                "Procedural Map Verification",
                "All procedural Explore map configs passed.",
                "OK");
        }

        public static void RunBatch()
        {
            string[] guids = AssetDatabase.FindAssets("t:ExploreMapConfig");
            if (guids.Length == 0)
                throw new InvalidOperationException("No ExploreMapConfig assets were found.");

            int generatedMaps = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ExploreMapConfig config = AssetDatabase.LoadAssetAtPath<ExploreMapConfig>(path);
                if (!config || config.useBakedSkeleton) continue;

                ExploreMapValidation.ValidateConfigOrThrow(config);
                ulong configSignature = ExploreMapGenerator.ComputeConfigSignature(config);
                var uniqueLayouts = new HashSet<ulong>();

                for (int index = 0; index < SeedsPerConfig; index++)
                {
                    ulong seed = RandomUtil.Mix3(VerificationSeed, configSignature, (ulong)index + 1UL);
                    ExploreMapSkeleton first = ExploreMapGenerator.BuildSkeletonImmediate(config, seed);
                    ExploreMapSkeleton replay = ExploreMapGenerator.BuildSkeletonImmediate(
                        config,
                        seed,
                        ExploreMapGenerator.CurrentVersion,
                        configSignature);

                    ulong firstLayout = ExploreMapGenerator.ComputeSkeletonSignature(first);
                    ulong replayLayout = ExploreMapGenerator.ComputeSkeletonSignature(replay);
                    if (firstLayout != replayLayout)
                        throw new InvalidOperationException(
                            $"Same-seed replay mismatch at {path}, seed={seed}.");

                    uniqueLayouts.Add(firstLayout);
                    generatedMaps += 2;
                }

                int minimumUniqueLayouts = Mathf.CeilToInt(SeedsPerConfig * 0.9f);
                if (uniqueLayouts.Count < minimumUniqueLayouts)
                    throw new InvalidOperationException(
                        $"Map diversity is too low at {path}; unique={uniqueLayouts.Count}/{SeedsPerConfig}.");

                // V1 스냅샷은 설정 서명 없이 레거시 생성기를 선택한다. 대표 시드가 여전히
                // 생성 가능하며 같은 입력으로 같은 결과를 내는지 호환 경로를 확인한다.
                const ulong legacySeed = 12345UL;
                ExploreMapSkeleton legacyFirst = ExploreMapGenerator.BuildSkeletonImmediate(
                    config,
                    legacySeed,
                    ExploreMapGenerator.LegacyVersion);
                ExploreMapSkeleton legacyReplay = ExploreMapGenerator.BuildSkeletonImmediate(
                    config,
                    legacySeed,
                    ExploreMapGenerator.LegacyVersion);
                if (ExploreMapGenerator.ComputeSkeletonSignature(legacyFirst) !=
                    ExploreMapGenerator.ComputeSkeletonSignature(legacyReplay))
                    throw new InvalidOperationException($"Legacy same-seed replay mismatch at {path}.");
                generatedMaps += 2;

                bool rejectedChangedConfig = false;
                try
                {
                    ExploreMapGenerator.BuildSkeletonImmediate(
                        config,
                        VerificationSeed,
                        ExploreMapGenerator.CurrentVersion,
                        configSignature ^ 1UL);
                }
                catch (InvalidOperationException)
                {
                    rejectedChangedConfig = true;
                }

                if (!rejectedChangedConfig)
                    throw new InvalidOperationException($"Config signature mismatch was not rejected at {path}.");

                Debug.Log(
                    $"[ExploreMapGenerationVerifier] PASS {path}: " +
                    $"{SeedsPerConfig} seeds, {uniqueLayouts.Count} unique layouts, config={configSignature:X16}");
            }

            if (generatedMaps == 0)
                throw new InvalidOperationException("No procedural Explore map configs were eligible for verification.");

            Debug.Log($"[ExploreMapGenerationVerifier] PASS: generated and validated {generatedMaps} maps.");
        }

        /// <summary>CI/배치 실행 진입점. 검증 결과를 프로세스 종료 코드로 전달한다.</summary>
        public static void RunBatchAndExit()
        {
            try
            {
                RunBatch();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
