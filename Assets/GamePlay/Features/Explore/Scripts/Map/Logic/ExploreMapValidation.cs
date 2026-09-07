using Core.Scripts.Foundation.Define;
using GamePlay.Features.Explore.Scripts.Map.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlay.Features.Explore.Scripts.Map.Logic
{
    /// <summary>
    /// 생성 입력과 결과에 대한 단일 검증 지점. 런타임 생성과 에디터 일괄 검증이 같은 규칙을 사용한다.
    /// </summary>
    public static class ExploreMapValidation
    {
        public const int MinimumMapSide = 9;
        public const int MaximumMapSide = 256;

        public static void ValidateConfigOrThrow(ExploreMapConfig cfg)
        {
            if (!cfg) throw new ArgumentNullException(nameof(cfg));
            if (cfg.useBakedSkeleton)
            {
                if (!cfg.bakedMapAsset || cfg.bakedMapAsset.skeleton == null)
                    throw new InvalidOperationException("Baked map mode requires a skeleton asset.");
                return;
            }

            if (cfg.xCapacity < MinimumMapSide || cfg.yCapacity < MinimumMapSide)
                throw new InvalidOperationException(
                    $"Map size must be at least {MinimumMapSide}x{MinimumMapSide}; actual={cfg.xCapacity}x{cfg.yCapacity}.");
            if (cfg.xCapacity > MaximumMapSide || cfg.yCapacity > MaximumMapSide)
                throw new InvalidOperationException(
                    $"Map size must not exceed {MaximumMapSide}x{MaximumMapSide}; actual={cfg.xCapacity}x{cfg.yCapacity}.");

            ExploreMapGenerationRules rules = cfg.GenerationRules;
            if (rules.minSpineNodes < 2 || rules.maxSpineNodes < rules.minSpineNodes)
                throw new InvalidOperationException("Spine node range is invalid.");
            if (rules.minNodeSpacing < 1 || rules.maxNodeSpacing < rules.minNodeSpacing)
                throw new InvalidOperationException("Node spacing range is invalid.");
            if (rules.minStartBossPathLength < 1)
                throw new InvalidOperationException("Minimum Start-Boss path length must be positive.");
            if (rules.branchProbability < 0f || rules.branchProbability > 1f)
                throw new InvalidOperationException("Branch probability must be between 0 and 1.");
            if (rules.minBranchLength < 1 || rules.maxBranchLength < rules.minBranchLength)
                throw new InvalidOperationException("Branch length range is invalid.");
            if (rules.borderThickness < 1 || rules.borderThickness * 2 >= Math.Min(cfg.xCapacity, cfg.yCapacity))
                throw new InvalidOperationException("Border thickness leaves no usable map interior.");
            if (rules.minSymbolSpacing < 1 || rules.eliteSpacingBonus < 0)
                throw new InvalidOperationException("Symbol spacing is invalid.");
            if (rules.maxAttempts < 1 || rules.maxAttempts > 256)
                throw new InvalidOperationException("Generation attempts must be between 1 and 256.");

            Dictionary<SystemEnum.MapSymbolType, int> requested = GetRequestedSymbolCounts(cfg);
            ValidateAnchorRequest(requested, SystemEnum.MapSymbolType.StartPoint);
            ValidateAnchorRequest(requested, SystemEnum.MapSymbolType.BossBattle);
            ValidateAnchorRequest(requested, SystemEnum.MapSymbolType.EndPoint);

            if (requested.TryGetValue(SystemEnum.MapSymbolType.Event, out int eventCount) && eventCount > 0 &&
                !HasPositiveEventWeight(cfg))
                throw new InvalidOperationException("Event symbols require at least one positive event candidate weight.");
            if (requested.TryGetValue(SystemEnum.MapSymbolType.Item, out int itemCount) && itemCount > 0 &&
                !HasPositiveItemWeight(cfg))
                throw new InvalidOperationException("Item symbols require at least one positive item candidate weight.");
        }

        public static void ValidateSkeletonOrThrow(
            ExploreMapConfig cfg,
            ExploreMapSkeleton skeleton,
            int expectedGenerationVersion,
            ulong expectedConfigSignature)
        {
            if (skeleton == null) throw new ArgumentNullException(nameof(skeleton));
            if (skeleton.Width != cfg.xCapacity || skeleton.Height != cfg.yCapacity)
                throw new InvalidOperationException("Generated skeleton size does not match its config.");
            if (skeleton.GenerationVersion != expectedGenerationVersion)
                throw new InvalidOperationException("Generated skeleton version does not match the requested version.");
            if (expectedConfigSignature != 0UL && skeleton.ConfigSignature != expectedConfigSignature)
                throw new InvalidOperationException("Generated skeleton config signature does not match its config.");

            List<SkeletonSymbol> symbols = skeleton.CollectSymbols();
            SkeletonSymbol start = RequireSingle(symbols, SystemEnum.MapSymbolType.StartPoint);
            SkeletonSymbol boss = RequireSingle(symbols, SystemEnum.MapSymbolType.BossBattle);
            SkeletonSymbol end = RequireSingle(symbols, SystemEnum.MapSymbolType.EndPoint);

            var occupied = new HashSet<int>();
            foreach (SkeletonSymbol symbol in symbols)
            {
                if (!skeleton.InBounds(symbol.X, symbol.Y) || !skeleton.IsFloor(symbol.X, symbol.Y))
                    throw new InvalidOperationException($"Symbol {symbol.Type} must be placed on a floor cell.");
                if (!occupied.Add(skeleton.ToIndex(symbol.X, symbol.Y)))
                    throw new InvalidOperationException("Multiple symbols occupy the same cell.");
                if (symbol.Type == SystemEnum.MapSymbolType.Event && !symbol.EventType.HasValue)
                    throw new InvalidOperationException("Event symbol is missing its event payload.");
                if (symbol.Type == SystemEnum.MapSymbolType.Item && !symbol.ItemIndex.HasValue)
                    throw new InvalidOperationException("Item symbol is missing its item payload.");
            }

            if (Math.Abs(boss.X - end.X) + Math.Abs(boss.Y - end.Y) != 1)
                throw new InvalidOperationException("Boss and End must be orthogonally adjacent.");

            foreach ((int x, int y) neighbor in skeleton.GetNeighbors4(end.X, end.Y))
            {
                bool isBoss = neighbor.x == boss.X && neighbor.y == boss.Y;
                if (skeleton.IsFloor(neighbor.x, neighbor.y) != isBoss)
                    throw new InvalidOperationException("End must have Boss as its only walkable neighbor.");
            }

            int reachableFloors = CountReachableFloors(skeleton, start.X, start.Y);
            int totalFloors = CountFloors(skeleton);
            if (reachableFloors != totalFloors)
                throw new InvalidOperationException($"All floor cells must be connected; reachable={reachableFloors}, total={totalFloors}.");

            int startBossDistance = ShortestFloorDistance(skeleton, start.X, start.Y, boss.X, boss.Y);
            if (startBossDistance < cfg.GenerationRules.minStartBossPathLength)
                throw new InvalidOperationException(
                    $"Start-Boss path is too short; required={cfg.GenerationRules.minStartBossPathLength}, actual={startBossDistance}.");

            Dictionary<SystemEnum.MapSymbolType, int> expected = GetRequestedSymbolCounts(cfg);
            foreach (SystemEnum.MapSymbolType type in Enum.GetValues(typeof(SystemEnum.MapSymbolType)))
            {
                if (type == SystemEnum.MapSymbolType.None) continue;
                int expectedCount = IsAnchor(type) ? 1 : expected.TryGetValue(type, out int count) ? count : 0;
                int actualCount = symbols.Count(symbol => symbol.Type == type);
                if (actualCount != expectedCount)
                    throw new InvalidOperationException(
                        $"Symbol count mismatch for {type}; expected={expectedCount}, actual={actualCount}.");
            }

            int border = cfg.GenerationRules.borderThickness;
            for (int y = 0; y < skeleton.Height; y++)
            for (int x = 0; x < skeleton.Width; x++)
            {
                if (x < border || y < border || x >= skeleton.Width - border || y >= skeleton.Height - border)
                {
                    if (!skeleton.IsWall(x, y))
                        throw new InvalidOperationException("Configured map border must contain walls only.");
                }
            }
        }

        public static Dictionary<SystemEnum.MapSymbolType, int> GetRequestedSymbolCounts(ExploreMapConfig cfg)
        {
            var result = new Dictionary<SystemEnum.MapSymbolType, int>();
            if (cfg.symbolConfig == null) return result;
            foreach (SymbolConfigEntry entry in cfg.symbolConfig)
            {
                if (entry == null || entry.symbolType == SystemEnum.MapSymbolType.None) continue;
                result.TryGetValue(entry.symbolType, out int current);
                result[entry.symbolType] = checked(current + Math.Max(0, entry.symbolCount));
            }
            return result;
        }

        private static void ValidateAnchorRequest(
            IReadOnlyDictionary<SystemEnum.MapSymbolType, int> requested,
            SystemEnum.MapSymbolType type)
        {
            if (requested.TryGetValue(type, out int count) && count != 1)
                throw new InvalidOperationException($"Anchor symbol {type}, when configured, must have count 1.");
        }

        private static bool HasPositiveEventWeight(ExploreMapConfig cfg) =>
            cfg.eventCandidate != null && cfg.eventCandidate.Any(entry => entry != null && entry.probability > 0);

        private static bool HasPositiveItemWeight(ExploreMapConfig cfg) =>
            cfg.itemCandidate != null && cfg.itemCandidate.Any(entry => entry != null && entry.dropProbability > 0);

        private static bool IsAnchor(SystemEnum.MapSymbolType type) =>
            type == SystemEnum.MapSymbolType.StartPoint ||
            type == SystemEnum.MapSymbolType.BossBattle ||
            type == SystemEnum.MapSymbolType.EndPoint;

        private static SkeletonSymbol RequireSingle(List<SkeletonSymbol> symbols, SystemEnum.MapSymbolType type)
        {
            List<SkeletonSymbol> matches = symbols.Where(symbol => symbol.Type == type).ToList();
            if (matches.Count != 1)
                throw new InvalidOperationException($"Exactly one {type} symbol is required; actual={matches.Count}.");
            return matches[0];
        }

        private static int CountFloors(ExploreMapSkeleton skeleton)
        {
            int count = 0;
            for (int y = 0; y < skeleton.Height; y++)
            for (int x = 0; x < skeleton.Width; x++)
                if (skeleton.IsFloor(x, y)) count++;
            return count;
        }

        private static int CountReachableFloors(ExploreMapSkeleton skeleton, int startX, int startY)
        {
            if (!skeleton.IsFloor(startX, startY)) return 0;
            var visited = new bool[skeleton.Width * skeleton.Height];
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue((startX, startY));
            visited[skeleton.ToIndex(startX, startY)] = true;
            int count = 0;
            while (queue.Count > 0)
            {
                (int x, int y) current = queue.Dequeue();
                count++;
                foreach ((int x, int y) next in skeleton.GetNeighbors4(current.x, current.y))
                {
                    int index = skeleton.ToIndex(next.x, next.y);
                    if (visited[index] || !skeleton.IsFloor(next.x, next.y)) continue;
                    visited[index] = true;
                    queue.Enqueue(next);
                }
            }
            return count;
        }

        private static int ShortestFloorDistance(
            ExploreMapSkeleton skeleton,
            int startX,
            int startY,
            int targetX,
            int targetY)
        {
            var distances = Enumerable.Repeat(-1, skeleton.Width * skeleton.Height).ToArray();
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue((startX, startY));
            distances[skeleton.ToIndex(startX, startY)] = 0;
            while (queue.Count > 0)
            {
                (int x, int y) current = queue.Dequeue();
                int distance = distances[skeleton.ToIndex(current.x, current.y)];
                if (current.x == targetX && current.y == targetY) return distance;
                foreach ((int x, int y) next in skeleton.GetNeighbors4(current.x, current.y))
                {
                    int index = skeleton.ToIndex(next.x, next.y);
                    if (distances[index] >= 0 || !skeleton.IsFloor(next.x, next.y)) continue;
                    distances[index] = distance + 1;
                    queue.Enqueue(next);
                }
            }
            return -1;
        }
    }
}
