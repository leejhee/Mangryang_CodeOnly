using Core.Scripts.Foundation.Utils;
using Cysharp.Threading.Tasks;
using GamePlay.Features.Explore.Scripts.Map.Data;
using System;
using System.Collections.Generic;

namespace GamePlay.Features.Explore.Scripts.Map.Logic
{
    /// <summary>
    /// 시드와 설정만으로 탐사 스켈레톤을 결정적으로 생성한다.
    /// 저장된 맵 호환성을 깨는 변경을 할 때는 CurrentVersion을 올리고 이전 경로를 유지한다.
    /// </summary>
    public static class ExploreMapGenerator
    {
        public const int LegacyVersion = 1;
        public const int CurrentVersion = 2;

        public static UniTask<ExploreMapSkeleton> BuildSkeleton(
            ExploreMapConfig cfg,
            ulong seed,
            int generationVersion = CurrentVersion,
            ulong expectedConfigSignature = 0UL)
        {
            // 현재 맵 크기에서는 메인 스레드에서 수 ms 내에 끝난다. ScriptableObject를 작업자
            // 스레드에서 읽지 않도록 동기 생성 후 완료된 UniTask로 반환한다.
            return UniTask.FromResult(BuildSkeletonImmediate(cfg, seed, generationVersion, expectedConfigSignature));
        }

        public static ExploreMapSkeleton BuildSkeletonImmediate(
            ExploreMapConfig cfg,
            ulong seed,
            int generationVersion = CurrentVersion,
            ulong expectedConfigSignature = 0UL)
        {
            ExploreMapValidation.ValidateConfigOrThrow(cfg);

            if (cfg.useBakedSkeleton)
                return cfg.bakedMapAsset.skeleton;

            ulong configSignature = ComputeConfigSignature(cfg);
            if (expectedConfigSignature != 0UL && expectedConfigSignature != configSignature)
                throw new InvalidOperationException(
                    $"Map config changed since save. Saved={expectedConfigSignature:X16}, Current={configSignature:X16}.");

            return generationVersion switch
            {
                LegacyVersion => BuildLegacy(cfg, seed),
                CurrentVersion => BuildCurrent(cfg, seed, configSignature),
                _ => throw new NotSupportedException($"Explore map generator version {generationVersion} is not supported.")
            };
        }

        private static ExploreMapSkeleton BuildLegacy(ExploreMapConfig cfg, ulong seed)
        {
            var builder = new NodeMapBuilder(cfg, seed, seed, LegacyVersion, 0UL);
            RunPipeline(
                builder,
                minSpineNodes: 14,
                maxSpineNodes: 20,
                minNodeSpacing: 7,
                maxNodeSpacing: 10,
                branchProbability: 0.12f,
                minBranchLength: 2,
                maxBranchLength: 6,
                borderThickness: 1,
                minSymbolSpacing: 2,
                eliteSpacingBonus: 1,
                requireExactSymbolCounts: false);
            builder.BasicValidate();
            return builder.ToSkeleton();
        }

        private static ExploreMapSkeleton BuildCurrent(ExploreMapConfig cfg, ulong seed, ulong configSignature)
        {
            ExploreMapGenerationRules rules = cfg.GenerationRules;
            InvalidOperationException lastFailure = null;

            for (int attempt = 0; attempt < rules.maxAttempts; attempt++)
            {
                ulong attemptSeed = RandomUtil.Mix3(seed, configSignature, (ulong)attempt + 1UL);
                var builder = new NodeMapBuilder(
                    cfg,
                    randomSeed: attemptSeed,
                    mapSeed: seed,
                    generationVersion: CurrentVersion,
                    configSignature: configSignature);

                try
                {
                    RunPipeline(
                        builder,
                        rules.minSpineNodes,
                        rules.maxSpineNodes,
                        rules.minNodeSpacing,
                        rules.maxNodeSpacing,
                        rules.branchProbability,
                        rules.minBranchLength,
                        rules.maxBranchLength,
                        rules.borderThickness,
                        rules.minSymbolSpacing,
                        rules.eliteSpacingBonus,
                        requireExactSymbolCounts: true);

                    builder.BasicValidate();
                    ExploreMapSkeleton skeleton = builder.ToSkeleton();
                    ExploreMapValidation.ValidateSkeletonOrThrow(
                        cfg,
                        skeleton,
                        CurrentVersion,
                        configSignature);
                    return skeleton;
                }
                catch (InvalidOperationException exception)
                {
                    lastFailure = exception;
                }
            }

            throw new InvalidOperationException(
                $"Failed to generate a valid map after {rules.maxAttempts} deterministic attempts " +
                $"(seed={seed}, config={configSignature:X16}).",
                lastFailure);
        }

        private static void RunPipeline(
            NodeMapBuilder builder,
            int minSpineNodes,
            int maxSpineNodes,
            int minNodeSpacing,
            int maxNodeSpacing,
            float branchProbability,
            int minBranchLength,
            int maxBranchLength,
            int borderThickness,
            int minSymbolSpacing,
            int eliteSpacingBonus,
            bool requireExactSymbolCounts)
        {
            builder.ComputeInteriorMask();
            builder.InitWalls();
            builder.PlaceEndAndBossAdjacent();
            builder.CreateMainSpineFromBoss(
                minSpineNodes,
                maxSpineNodes,
                minNodeSpacing,
                maxNodeSpacing);
            builder.AddSideBranches(branchProbability, minBranchLength, maxBranchLength);
            builder.ChooseStartFromFurthest();
            builder.CarveNodes();
            builder.CarveCorridors();
            builder.EnsureStartBossConnectivity();
            builder.EnforceEndLockedByBoss();
            builder.RemoveDiagonalTouches();
            builder.CullFloorsNotReachableFromBoss();
            builder.SealBorderWalls(borderThickness);
            builder.ScatterSymbols(minSymbolSpacing, eliteSpacingBonus, requireExactSymbolCounts);
        }

        public static ulong ComputeConfigSignature(ExploreMapConfig cfg)
        {
            if (!cfg) throw new ArgumentNullException(nameof(cfg));
            ulong hash = 14695981039346656037UL;
            Add(ref hash, CurrentVersion);
            Add(ref hash, (int)cfg.dungeonName);
            Add(ref hash, cfg.floor);
            Add(ref hash, cfg.xCapacity);
            Add(ref hash, cfg.yCapacity);

            ExploreMapGenerationRules rules = cfg.GenerationRules;
            Add(ref hash, rules.minSpineNodes);
            Add(ref hash, rules.maxSpineNodes);
            Add(ref hash, rules.minNodeSpacing);
            Add(ref hash, rules.maxNodeSpacing);
            Add(ref hash, rules.minStartBossPathLength);
            Add(ref hash, (int)Math.Round(rules.branchProbability * 1_000_000f));
            Add(ref hash, rules.minBranchLength);
            Add(ref hash, rules.maxBranchLength);
            Add(ref hash, rules.borderThickness);
            Add(ref hash, rules.minSymbolSpacing);
            Add(ref hash, rules.eliteSpacingBonus);
            Add(ref hash, rules.maxAttempts);

            AddSymbolConfig(ref hash, cfg.symbolConfig);
            AddEventConfig(ref hash, cfg.eventCandidate);
            AddItemConfig(ref hash, cfg.itemCandidate);
            return hash == 0UL ? 1UL : hash;
        }

        public static ulong ComputeSkeletonSignature(ExploreMapSkeleton skeleton)
        {
            if (skeleton == null) throw new ArgumentNullException(nameof(skeleton));
            ulong hash = 14695981039346656037UL;
            Add(ref hash, skeleton.Width);
            Add(ref hash, skeleton.Height);
            for (int y = 0; y < skeleton.Height; y++)
            for (int x = 0; x < skeleton.Width; x++)
                Add(ref hash, (int)skeleton.GetCellType(x, y));

            foreach (SkeletonSymbol symbol in skeleton.CollectSymbols())
            {
                Add(ref hash, symbol.Id);
                Add(ref hash, (int)symbol.Type);
                Add(ref hash, symbol.X);
                Add(ref hash, symbol.Y);
                Add(ref hash, symbol.EventType.HasValue ? (int)symbol.EventType.Value + 1 : 0);
                Add(ref hash, symbol.ItemIndex.HasValue ? unchecked((ulong)symbol.ItemIndex.Value) : 0UL);
            }
            return hash;
        }

        private static void AddSymbolConfig(ref ulong hash, List<SymbolConfigEntry> entries)
        {
            Add(ref hash, entries?.Count ?? 0);
            if (entries == null) return;
            foreach (SymbolConfigEntry entry in entries)
            {
                Add(ref hash, entry == null ? -1 : (int)entry.symbolType);
                Add(ref hash, entry?.symbolCount ?? 0);
            }
        }

        private static void AddEventConfig(ref ulong hash, List<EventConfigEntry> entries)
        {
            Add(ref hash, entries?.Count ?? 0);
            if (entries == null) return;
            foreach (EventConfigEntry entry in entries)
            {
                Add(ref hash, entry == null ? -1 : (int)entry.eventType);
                Add(ref hash, entry?.probability ?? 0);
            }
        }

        private static void AddItemConfig(ref ulong hash, List<ItemConfigEntry> entries)
        {
            Add(ref hash, entries?.Count ?? 0);
            if (entries == null) return;
            foreach (ItemConfigEntry entry in entries)
            {
                Add(ref hash, entry == null ? ulong.MaxValue : unchecked((ulong)entry.itemIndex));
                Add(ref hash, entry?.dropProbability ?? 0);
            }
        }

        private static void Add(ref ulong hash, int value) => Add(ref hash, unchecked((ulong)(uint)value));

        private static void Add(ref ulong hash, ulong value)
        {
            unchecked
            {
                for (int shift = 0; shift < 64; shift += 8)
                {
                    hash ^= (byte)(value >> shift);
                    hash *= 1099511628211UL;
                }
            }
        }
    }
}
