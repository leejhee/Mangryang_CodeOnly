using Core.Scripts.Foundation.Define;
using Core.Scripts.Foundation.Utils;
using GamePlay.Features.Explore.Scripts.Map.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlay.Features.Explore.Scripts.Map.Logic
{
    /// <summary>
    /// 선형 진행 구조 기반 맵 생성기 (Boss → End 인접 보장)
    /// </summary>
    public class NodeMapBuilder
    {
        private readonly ExploreMapConfig _cfg;
        private readonly GameRandom _rng;
        private readonly ExploreMapSkeleton _skel;

        private bool[] _interiorMask;
        private List<(int x, int y)> _interiorCells;

        private List<Node> _nodes = new();
        private Node _bossNode;
        private Node _endNode;
        private Node _startNode;
        private List<Node> _spineNodes = new(); // 메인 경로 노드들
        private readonly HashSet<int> _protectedCells = new();

        private class Node
        {
            public int X, Y;
            public int Radius;
            public int SpineOrder = -1; // 스파인 상의 순서 (-1이면 사이드 브랜치)
            public Node Parent; // 부모 노드 (연결 정보)
            public List<Node> Children = new();

            public SystemEnum.MapSymbolType? SymbolType;
            public SystemEnum.CellEventType? EventType;
            public long? ItemIndex;
        }

        public NodeMapBuilder(
            ExploreMapConfig cfg,
            ulong randomSeed,
            ulong mapSeed,
            int generationVersion,
            ulong configSignature)
        {
            _cfg = cfg;
            _rng = new GameRandom(randomSeed);
            _skel = new ExploreMapSkeleton(
                dungeonName: cfg.dungeonName.ToString(),
                floor: cfg.floor,
                width: cfg.xCapacity,
                height: cfg.yCapacity,
                seed: mapSeed,
                generationVersion: generationVersion,
                configSignature: configSignature
            );
        }

        public NodeMapBuilder(ExploreMapConfig cfg, ulong seed)
            : this(cfg, seed, seed, ExploreMapGenerator.LegacyVersion, 0UL)
        {
        }

        #region Core Methods

        public void ComputeInteriorMask()
        {
            int W = _skel.Width;
            int H = _skel.Height;
            _interiorMask = new bool[W * H];
            _interiorCells = new List<(int, int)>(W * H / 2 + 8);

            int cx = (W - 1) / 2;
            int cy = (H - 1) / 2;
            int hx = Math.Max(1, (W - 1) / 2);
            int hy = Math.Max(1, (H - 1) / 2);

            for (int y = 0; y < H; y++)
            {
                int dy = Math.Abs(y - cy);
                for (int x = 0; x < W; x++)
                {
                    int dx = Math.Abs(x - cx);
                    bool isInside = (dx * hy + dy * hx) <= (hx * hy);

                    if (x == 0 || y == 0 || x == W - 1 || y == H - 1)
                        isInside = false;

                    if (isInside)
                    {
                        _interiorMask[Index(x, y)] = true;
                        _interiorCells.Add((x, y));
                    }
                }
            }

            if (_interiorCells.Count == 0)
                throw new InvalidOperationException("Interior mask is empty.");
        }

        public void InitWalls()
        {
            for (int y = 0; y < _skel.Height; y++)
            for (int x = 0; x < _skel.Width; x++)
                _skel.SetCellType(x, y, SystemEnum.MapCellType.Wall);
        }

        /// <summary>
        /// Boss와 End를 인접하게 배치 (거리 1-2칸)
        /// </summary>
        public void PlaceEndAndBossAdjacent(int adjacentDistance = 1)
        {
            // End 위치를 먼저 랜덤 선택
            var endPos = _interiorCells[_rng.Next(_interiorCells.Count)];

            _endNode = new Node
            {
                X = endPos.x,
                Y = endPos.y,
                //Radius = _rng.Next(1, 3),
                Radius = 0,
                SymbolType = SystemEnum.MapSymbolType.EndPoint
            };
            _nodes.Add(_endNode);

            // End에서 adjacentDistance 거리 내의 유효한 위치 찾기
            //var bossPos = FindAdjacentPosition(endPos, adjacentDistance, minDistance: adjacentDistance);
            var bossPos = FindOrthNeighbor(endPos);

            _bossNode = new Node
            {
                X = bossPos.x, Y = bossPos.y, Radius = 2, SymbolType = SystemEnum.MapSymbolType.BossBattle
            };
            _nodes.Add(_bossNode);

            // Boss → End 연결
            _bossNode.Children.Add(_endNode);
            _endNode.Parent = _bossNode;
        }

        /// <summary>
        /// Boss에서 시작해서 Start 방향으로 메인 스파인 생성
        /// </summary>
        public void CreateMainSpineFromBoss(int minLength = 8, int maxLength = 12, int minSpacing = 3,
            int maxSpacing = 5)
        {
            int targetLength = _rng.Next(minLength, maxLength + 1);

            _spineNodes.Add(_bossNode);
            _bossNode.SpineOrder = 0;

            Node currentNode = _bossNode;
            int currentOrder = 1;

            for (int i = 0; i < targetLength; i++)
            {
                int spacing = _rng.Next(minSpacing, maxSpacing + 1);

                // 현재 노드에서 spacing 거리만큼 떨어진 위치 찾기
                var nextPos = FindSpineNextPosition(currentNode, spacing, avoidNodes: _nodes);

                if (nextPos.x == -1) // 더 이상 배치 불가
                {
                    break;
                }

                var newNode = new Node { X = nextPos.x, Y = nextPos.y, Radius = 1, SpineOrder = currentOrder };

                _nodes.Add(newNode);
                _spineNodes.Add(newNode);

                // 연결
                currentNode.Children.Add(newNode);
                newNode.Parent = currentNode;

                currentNode = newNode;
                currentOrder++;
            }

            // 마지막 노드를 Start 후보로 지정
            _startNode = currentNode;
        }

        /// <summary>
        /// 메인 스파인에서 짧은 사이드 브랜치 생성
        /// </summary>
        public void AddSideBranches(float branchProbability = 0.3f, int minBranchLength = 1, int maxBranchLength = 3)
        {
            // Boss와 End를 제외한 스파인 노드들에서만 분기
            var branchableNodes = _spineNodes.Where(n => n != _bossNode && n != _endNode).ToList();

            foreach (var spineNode in branchableNodes)
            {
                if (_rng.NextDouble() > branchProbability)
                    continue;

                int branchLength = _rng.Next(minBranchLength, maxBranchLength + 1);

                // 좌우 방향 결정
                bool goLeft = _rng.Next(2) == 0;

                Node currentBranch = spineNode;

                for (int i = 0; i < branchLength; i++)
                {
                    var branchPos = FindSideBranchPosition(currentBranch, goLeft, distance: 3);

                    if (branchPos.x == -1)
                        break;

                    var branchNode = new Node
                    {
                        X = branchPos.x, Y = branchPos.y, Radius = _rng.Next(1, 2), SpineOrder = -1 // 사이드 브랜치
                    };

                    _nodes.Add(branchNode);
                    currentBranch.Children.Add(branchNode);
                    branchNode.Parent = currentBranch;

                    currentBranch = branchNode;
                }
            }
        }

        /// <summary>
        /// 가장 먼 스파인 노드를 Start로 선택
        /// </summary>
        public void ChooseStartFromFurthest()
        {
            if (_startNode != null)
            {
                _startNode.SymbolType = SystemEnum.MapSymbolType.StartPoint;
                return;
            }

            // Fallback: 스파인에서 Boss로부터 가장 먼 노드
            Node furthest = _spineNodes.OrderByDescending(n => n.SpineOrder).FirstOrDefault();
            if (furthest != null)
            {
                furthest.SymbolType = SystemEnum.MapSymbolType.StartPoint;
                _startNode = furthest;
            }
        }

        public void CarveNodes()
        {
            foreach (var node in _nodes)
            {
                if (_interiorMask[Index(node.X, node.Y)])
                    _skel.SetCellType(node.X, node.Y, SystemEnum.MapCellType.Floor);

                for (int dy = -node.Radius; dy <= node.Radius; dy++)
                {
                    for (int dx = -node.Radius; dx <= node.Radius; dx++)
                    {
                        if (Math.Abs(dx) + Math.Abs(dy) <= node.Radius)
                        {
                            int nx = node.X + dx;
                            int ny = node.Y + dy;
                            if (_skel.InBounds(nx, ny) && _interiorMask[Index(nx, ny)])
                            {
                                _skel.SetCellType(nx, ny, SystemEnum.MapCellType.Floor);
                            }
                        }
                    }
                }
            }
        }

        public void CarveCorridors()
        {
            foreach (var node in _nodes)
            {
                foreach (var child in node.Children)
                {
                    CarveCorridorBetween(node.X, node.Y, child.X, child.Y);
                }
            }

            // 대각선 연결 제거
            //FixDiagonalGaps();
            ProtectMainRoute();
            RemoveDiagonalTouches();
        }

        private void CarveCorridorBetween(int x1, int y1, int x2, int y2)
        {
            // BFS 기반 interior 내부 경로 찾기
            var path = FindPathInInterior((x1, y1), (x2, y2));

            if (path == null || path.Count == 0)
            {
                // Fallback: L자 복도
                CarveCorridorFallback(x1, y1, x2, y2);
                return;
            }

            foreach (var (x, y) in path)
            {
                if (_skel.InBounds(x, y))
                    _skel.SetCellType(x, y, SystemEnum.MapCellType.Floor);
            }
        }

        private List<(int x, int y)> FindPathInInterior((int x, int y) start, (int x, int y) end)
        {
            int W = _skel.Width, H = _skel.Height;
            var q = new Queue<(int x, int y)>();
            var prev = new Dictionary<(int, int), (int, int)>();
            var visited = new bool[W * H];

            if (!_interiorMask[Index(start.x, start.y)] || !_interiorMask[Index(end.x, end.y)])
                return null;

            q.Enqueue(start);
            visited[Index(start.x, start.y)] = true;
            prev[start] = (-1, -1);

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur == end) break;

                foreach (var (nx, ny) in GetNeighbors4(cur.x, cur.y))
                {
                    int ni = Index(nx, ny);
                    if (!_interiorMask[ni] || visited[ni]) continue;
                    if (IsEndForbidden(nx, ny)) continue;
                    visited[ni] = true;
                    prev[(nx, ny)] = cur;
                    q.Enqueue((nx, ny));
                }
            }

            if (!visited[Index(end.x, end.y)]) return null;

            var path = new List<(int x, int y)>();
            var p = end;
            while (p.x != -1)
            {
                path.Add(p);
                p = prev[p];
            }

            path.Reverse();
            return path;
        }

        private void CarveCorridorFallback(int x1, int y1, int x2, int y2)
        {
            bool xFirst = _rng.Next(2) == 0;

            if (xFirst)
            {
                int x = x1;
                while (x != x2)
                {
                    if (_skel.InBounds(x, y1))
                        _skel.SetCellType(x, y1, SystemEnum.MapCellType.Floor);
                    x += Math.Sign(x2 - x);
                }

                int y = y1;
                while (y != y2)
                {
                    if (_skel.InBounds(x2, y))
                        _skel.SetCellType(x2, y, SystemEnum.MapCellType.Floor);
                    y += Math.Sign(y2 - y);
                }
            }
            else
            {
                int y = y1;
                while (y != y2)
                {
                    if (_skel.InBounds(x1, y))
                        _skel.SetCellType(x1, y, SystemEnum.MapCellType.Floor);
                    y += Math.Sign(y2 - y);
                }

                int x = x1;
                while (x != x2)
                {
                    if (_skel.InBounds(x, y2))
                        _skel.SetCellType(x, y2, SystemEnum.MapCellType.Floor);
                    x += Math.Sign(x2 - x);
                }
            }

            if (_skel.InBounds(x2, y2))
                _skel.SetCellType(x2, y2, SystemEnum.MapCellType.Floor);
        }

        private void FixDiagonalGaps()
        {
            int W = _skel.Width, H = _skel.Height;

            for (int y = 0; y < H - 1; y++)
            {
                for (int x = 0; x < W - 1; x++)
                {
                    var tl = _skel.GetCellType(x, y);
                    var tr = _skel.GetCellType(x + 1, y);
                    var bl = _skel.GetCellType(x, y + 1);
                    var br = _skel.GetCellType(x + 1, y + 1);

                    bool isDiagonal1 = (tl == SystemEnum.MapCellType.Floor && br == SystemEnum.MapCellType.Floor &&
                                        tr == SystemEnum.MapCellType.Wall && bl == SystemEnum.MapCellType.Wall);
                    bool isDiagonal2 = (tr == SystemEnum.MapCellType.Floor && bl == SystemEnum.MapCellType.Floor &&
                                        tl == SystemEnum.MapCellType.Wall && br == SystemEnum.MapCellType.Wall);

                    if (isDiagonal1)
                    {
                        if (_interiorMask[Index(x + 1, y)])
                            _skel.SetCellType(x + 1, y, SystemEnum.MapCellType.Floor);
                        else if (_interiorMask[Index(x, y + 1)])
                            _skel.SetCellType(x, y + 1, SystemEnum.MapCellType.Floor);
                        else
                            _skel.SetCellType(x + 1, y, SystemEnum.MapCellType.Floor);
                    }
                    else if (isDiagonal2)
                    {
                        if (_interiorMask[Index(x, y)])
                            _skel.SetCellType(x, y, SystemEnum.MapCellType.Floor);
                        else if (_interiorMask[Index(x + 1, y + 1)])
                            _skel.SetCellType(x + 1, y + 1, SystemEnum.MapCellType.Floor);
                        else
                            _skel.SetCellType(x, y, SystemEnum.MapCellType.Floor);
                    }
                }
            }
        }

        public void EnforceEndLockedByBoss()
        {
            if (_endNode == null || _bossNode == null) return;
            int ex = _endNode.X, ey = _endNode.Y;
            int bx = _bossNode.X, by = _bossNode.Y;

            // 4방 이웃 중 Boss만 바닥, 나머지는 벽
            var four = new (int x, int y)[] { (ex + 1, ey), (ex - 1, ey), (ex, ey + 1), (ex, ey - 1) };
            foreach (var c in four)
            {
                if (!_skel.InBounds(c.x, c.y)) continue;
                if (c.x == bx && c.y == by)
                    _skel.SetCellType(c.x, c.y, SystemEnum.MapCellType.Floor);
                else
                    _skel.SetCellType(c.x, c.y, SystemEnum.MapCellType.Wall);
            }

            // 대각선 이웃은 전부 벽
            var diag = new (int x, int y)[] { (ex + 1, ey + 1), (ex + 1, ey - 1), (ex - 1, ey + 1), (ex - 1, ey - 1) };
            foreach (var c in diag)
                if (_skel.InBounds(c.x, c.y))
                    _skel.SetCellType(c.x, c.y, SystemEnum.MapCellType.Wall);
        }

        public void RemoveDiagonalTouches()
        {
            int W = _skel.Width, H = _skel.Height;

            for (int y = 1; y < H; y++)
            {
                for (int x = 1; x < W; x++)
                {
                    bool tl = _skel.IsFloor(x - 1, y - 1);
                    bool tr = _skel.IsFloor(x, y - 1);
                    bool bl = _skel.IsFloor(x - 1, y);
                    bool br = _skel.IsFloor(x, y);

                    void TryWall(int cx, int cy)
                    {
                        if (_protectedCells.Contains(Index(cx, cy))) return;
                        if (_startNode != null && cx == _startNode.X && cy == _startNode.Y) return;
                        if (_endNode != null && cx == _endNode.X && cy == _endNode.Y) return;
                        if (_bossNode != null && cx == _bossNode.X && cy == _bossNode.Y) return;
                        _skel.SetCellType(cx, cy, SystemEnum.MapCellType.Wall);
                    }

                    if (tl && br && !tr && !bl)
                    {
                        int degTL = CountNeighbors4(x - 1, y - 1);
                        int degBR = CountNeighbors4(x, y);
                        if (degTL <= degBR) TryWall(x - 1, y - 1);
                        else TryWall(x, y);
                    }
                    else if (tr && bl && !tl && !br)
                    {
                        int degTR = CountNeighbors4(x, y - 1);
                        int degBL = CountNeighbors4(x - 1, y);
                        if (degTR <= degBL) TryWall(x, y - 1);
                        else TryWall(x - 1, y);
                    }
                }
            }
        }

        private int CountNeighbors4(int x, int y)
        {
            int cnt = 0;
            if (_skel.InBounds(x + 1, y) && _skel.IsFloor(x + 1, y)) cnt++;
            if (_skel.InBounds(x - 1, y) && _skel.IsFloor(x - 1, y)) cnt++;
            if (_skel.InBounds(x, y + 1) && _skel.IsFloor(x, y + 1)) cnt++;
            if (_skel.InBounds(x, y - 1) && _skel.IsFloor(x, y - 1)) cnt++;
            return cnt;
        }

        public void SealBorderWalls(int thickness = 1)
        {
            int W = _skel.Width, H = _skel.Height;
            thickness = Math.Max(1, Math.Min(thickness, Math.Min(W, H) / 2));

            for (int t = 0; t < thickness; t++)
            {
                for (int x = 0; x < W; x++)
                {
                    _skel.SetCellType(x, t, SystemEnum.MapCellType.Wall);
                    _skel.SetCellType(x, H - 1 - t, SystemEnum.MapCellType.Wall);
                }

                for (int y = 0; y < H; y++)
                {
                    _skel.SetCellType(t, y, SystemEnum.MapCellType.Wall);
                    _skel.SetCellType(W - 1 - t, y, SystemEnum.MapCellType.Wall);
                }
            }
        }

        /*public void ScatterSymbols()
        {
            if (_cfg.symbolConfig == null || _cfg.symbolConfig.Count == 0) return;

            float[] eventWeights = _cfg.eventCandidate?.Select(e => (float)Math.Max(0, e.probability)).ToArray();
            float[] itemWeights = _cfg.itemCandidate?.Select(i => (float)Math.Max(0, i.dropProbability)).ToArray();

            // Boss, End, Start는 이미 배치됨
            if (_bossNode != null)
            {
                if (!_skel.TryAddSimpleSymbol(_bossNode.X, _bossNode.Y, SystemEnum.MapSymbolType.BossBattle, out _,
                        out string err))
                    throw new InvalidOperationException($"Failed to place Boss: {err}");
            }

            if (_endNode != null)
            {
                if (!_skel.TryAddSimpleSymbol(_endNode.X, _endNode.Y, SystemEnum.MapSymbolType.EndPoint, out _,
                        out string err))
                    throw new InvalidOperationException($"Failed to place End: {err}");
            }

            if (_startNode != null)
            {
                if (!_skel.TryAddSimpleSymbol(_startNode.X, _startNode.Y, SystemEnum.MapSymbolType.StartPoint, out _,
                        out string err))
                    throw new InvalidOperationException($"Failed to place Start: {err}");
            }

            // Critical Path(Boss, End, Start) 제외한 노드들
            var availableNodes = _nodes.Where(n =>
                n != _bossNode && n != _endNode && n != _startNode
            ).ToList();

            foreach (var entry in _cfg.symbolConfig)
            {
                var type = entry.symbolType;

                // 이미 배치된 타입은 건너뛰기
                if (type == SystemEnum.MapSymbolType.StartPoint ||
                    type == SystemEnum.MapSymbolType.EndPoint ||
                    type == SystemEnum.MapSymbolType.BossBattle)
                    continue;

                int need = Math.Max(0, entry.symbolCount);
                int placed = 0;

                const int MAX_TRIES_PER_SYMBOL = 100;

                for (int i = 0; i < need; i++)
                {
                    bool success = false;

                    for (int attempt = 0; attempt < MAX_TRIES_PER_SYMBOL && !success; attempt++)
                    {
                        if (availableNodes.Count == 0)
                        {
                            UnityEngine.Debug.LogWarning(
                                $"[NodeMapBuilder] No available nodes left for symbol {type}. Placed {placed}/{need}.");
                            break;
                        }

                        var node = availableNodes[_rng.Next(availableNodes.Count)];

                        if (type == SystemEnum.MapSymbolType.Event)
                        {
                            if (eventWeights != null && eventWeights.Length > 0)
                            {
                                int idx = _rng.WeightedChoice(eventWeights);
                                var et = _cfg.eventCandidate[idx].eventType;
                                if (_skel.TryAddEventSymbol(node.X, node.Y, et, out _, out _))
                                {
                                    availableNodes.Remove(node);
                                    placed++;
                                    success = true;
                                }
                            }
                        }
                        else if (type == SystemEnum.MapSymbolType.Item)
                        {
                            if (itemWeights != null && itemWeights.Length > 0)
                            {
                                int idx = _rng.WeightedChoice(itemWeights);
                                long itemIndex = _cfg.itemCandidate[idx].itemIndex;
                                if (_skel.TryAddItemSymbol(node.X, node.Y, itemIndex, out _, out _))
                                {
                                    availableNodes.Remove(node);
                                    placed++;
                                    success = true;
                                }
                            }
                        }
                        else
                        {
                            if (_skel.TryAddSimpleSymbol(node.X, node.Y, type, out _, out _))
                            {
                                availableNodes.Remove(node);
                                placed++;
                                success = true;
                            }
                        }
                    }

                    if (!success)
                    {
                        UnityEngine.Debug.LogWarning(
                            $"[NodeMapBuilder] Failed to place symbol {type} after {MAX_TRIES_PER_SYMBOL} attempts. Placed {placed}/{need}.");
                    }
                }
            }
        }*/
        public void ScatterSymbols(int configuredMinSpacing = 2, int eliteSpacingBonus = 1, bool requireExactCounts = false)
        {
            float[] eventWeights = _cfg.eventCandidate?
                .Select(e => e == null ? 0f : (float)Math.Max(0, e.probability)).ToArray();
            float[] itemWeights  = _cfg.itemCandidate?
                .Select(i => i == null ? 0f : (float)Math.Max(0, i.dropProbability)).ToArray();
            
            EnsureAnchorsOnFloor();
            
            // 0) 앵커 3종 먼저 고정 배치
            if (_bossNode != null)
            {
                if (!_skel.TryAddSimpleSymbol(_bossNode.X, _bossNode.Y, SystemEnum.MapSymbolType.BossBattle, out _, out var err))
                    throw new InvalidOperationException($"Failed to place Boss: {err}");
            }
            if (_endNode != null)
            {
                if (!_skel.TryAddSimpleSymbol(_endNode.X, _endNode.Y, SystemEnum.MapSymbolType.EndPoint, out _, out var err))
                    throw new InvalidOperationException($"Failed to place End: {err}");
            }
            if (_startNode != null)
            {
                if (!_skel.TryAddSimpleSymbol(_startNode.X, _startNode.Y, SystemEnum.MapSymbolType.StartPoint, out _, out var err))
                    throw new InvalidOperationException($"Failed to place Start: {err}");
            }

            // 1) 심볼 후보 풀: (a) 앵커 제외 노드들 + (b) 모든 Floor(복도) 셀
            var spotPool = BuildSymbolSpotPool();
            
            int floorCount = 0;
            for (int y = 0; y < _skel.Height; y++)
               for (int x = 0; x < _skel.Width;  x++)
                   if (_skel.IsFloor(x, y))
                       floorCount++;

            int totalNeed = (_cfg.symbolConfig ?? new List<SymbolConfigEntry>())
                .Where(e => e != null)
                .Where(e => e.symbolType is not (SystemEnum.MapSymbolType.StartPoint
                    or SystemEnum.MapSymbolType.EndPoint or SystemEnum.MapSymbolType.BossBattle))
                .Sum(e => Math.Max(0, e.symbolCount));

            // 셀/심볼 개수로부터 간격 근사치 산출 (체비쇼프 거리)
            int baseMinSpacing = totalNeed <= 0
                ? 2
                : (int)Math.Max(2, Math.Min(6, Math.Sqrt((double)floorCount / (totalNeed + 3))));
            baseMinSpacing = Math.Max(Math.Max(1, configuredMinSpacing), baseMinSpacing);

            // 이미 배치된 지점(앵커 포함) 목록 – 간격 검사에 사용
            var placedPoints = new List<(int x, int y)>();
            if (_startNode != null) placedPoints.Add((_startNode.X, _startNode.Y));
            if (_bossNode  != null) placedPoints.Add((_bossNode.X,  _bossNode.Y));
            if (_endNode   != null) placedPoints.Add((_endNode.X,   _endNode.Y));
            
            foreach (var entry in _cfg.symbolConfig ?? new List<SymbolConfigEntry>())
            {
                if (entry == null) continue;
                var type = entry.symbolType;
                if (type is SystemEnum.MapSymbolType.StartPoint or SystemEnum.MapSymbolType.EndPoint or SystemEnum.MapSymbolType.BossBattle)
                    continue;

                int need = Math.Max(0, entry.symbolCount);
                int placed = 0;

                const int MAX_TRIES_PER_SYMBOL = 300;

                for (int i = 0; i < need; i++)
                {
                    bool success = false;

                    for (int attempt = 0; attempt < MAX_TRIES_PER_SYMBOL && !success; attempt++)
                    {
                        if (spotPool.Count == 0)
                        {
                            UnityEngine.Debug.LogWarning($"[NodeMapBuilder] No symbol spots left for {type}. Placed {placed}/{need}.");
                            break;
                        }

                        int k = _rng.Next(spotPool.Count);
                        var (sx, sy) = spotPool[k];
                        int minSpacing = baseMinSpacing;
                        if (type == SystemEnum.MapSymbolType.EliteBattle)
                            minSpacing = Math.Min(8, baseMinSpacing + Math.Max(0, eliteSpacingBonus));
                        
                        // 보스 뒤 End 규칙 위반/겹침 방지
                        if (IsEndForbidden(sx, sy) || !_skel.IsFloor(sx, sy) || _skel.HasAnySymbolAt(sx, sy)
                            || !IsFarFromPlaced((sx, sy), placedPoints, minSpacing))
                        {
                            spotPool.RemoveAt(k);
                            continue;
                        }

                        if (type == SystemEnum.MapSymbolType.Event)
                        {
                            if (eventWeights is { Length: > 0 })
                            {
                                int idx = _rng.WeightedChoice(eventWeights);
                                var et  = _cfg.eventCandidate[idx].eventType;
                                if (_skel.TryAddEventSymbol(sx, sy, et, out _, out _))
                                {
                                    spotPool.RemoveAt(k);
                                    placed++; success = true;
                                    placedPoints.Add((sx, sy));
                                }
                                else spotPool.RemoveAt(k);
                            }
                            else spotPool.RemoveAt(k);
                        }
                        else if (type == SystemEnum.MapSymbolType.Item)
                        {
                            if (itemWeights is { Length: > 0 })
                            {
                                int idx = _rng.WeightedChoice(itemWeights);
                                long itemIndex = _cfg.itemCandidate[idx].itemIndex;
                                if (_skel.TryAddItemSymbol(sx, sy, itemIndex, out _, out _))
                                {
                                    spotPool.RemoveAt(k);
                                    placed++; success = true;
                                    placedPoints.Add((sx, sy));
                                }
                                else spotPool.RemoveAt(k);
                            }
                            else spotPool.RemoveAt(k);
                        }
                        else
                        {
                            if (_skel.TryAddSimpleSymbol(sx, sy, type, out _, out _))
                            {
                                spotPool.RemoveAt(k);
                                placed++; success = true;
                                placedPoints.Add((sx, sy));
                            }
                            else spotPool.RemoveAt(k);
                        }
                    }

                    if (!success)
                    {
                        string message = $"Failed to place symbol {type}. Placed {placed}/{need}.";
                        if (requireExactCounts)
                            throw new InvalidOperationException(message);
                        UnityEngine.Debug.LogWarning($"[NodeMapBuilder] {message}");
                    }
                }
            }
        }

        /// <summary>
        /// 심볼 배치 후보 풀 구성: (1) 앵커 제외 노드들, (2) 모든 Floor 셀(End 인접 8방 제외)
        /// 작은 맵에서도 충분한 수를 보장하기 위해 복도를 포함.
        /// </summary>
        private List<(int x, int y)> BuildSymbolSpotPool()
        {
            var pool = new List<(int x, int y)>(_nodes.Count + _skel.Width * _skel.Height / 2);
            var taken = new HashSet<int>();

            void TryAdd(int x, int y)
            {
                if (!_skel.InBounds(x, y)) return;
                int idx = Index(x, y);
                if (taken.Contains(idx)) return;
                if (!_skel.IsFloor(x, y)) return;
                if (IsEndForbidden(x, y)) return;
                // 앵커 3종 좌표 제외
                if (_startNode != null && x == _startNode.X && y == _startNode.Y) return;
                if (_endNode   != null && x == _endNode.X   && y == _endNode.Y)   return;
                if (_bossNode  != null && x == _bossNode.X  && y == _bossNode.Y)  return;
                pool.Add((x, y));
                taken.Add(idx);
            }

            // (1) 앵커 제외 노드
            foreach (var n in _nodes)
            {
                if (n == _startNode || n == _endNode || n == _bossNode) continue;
                TryAdd(n.X, n.Y);
            }
            // (2) 모든 Floor(복도 포함)
            for (int y = 0; y < _skel.Height; y++)
            for (int x = 0; x < _skel.Width;  x++)
                if (_skel.IsFloor(x, y))
                    TryAdd(x, y);

            return pool;
        }
        
        public void BasicValidate()
        {
            int floorCount = 0;
            for (int y = 0; y < _skel.Height; y++)
            for (int x = 0; x < _skel.Width; x++)
                if (_skel.IsFloor(x, y))
                    floorCount++;

            if (floorCount == 0)
                throw new InvalidOperationException("No floor cells.");

            // Boss → End 연결 확인
            if (_bossNode != null && _endNode != null)
            {
                if (!ReachableOnFloors((_bossNode.X, _bossNode.Y), (_endNode.X, _endNode.Y)))
                    throw new InvalidOperationException("Boss is not connected to End.");
            }

            // Start → Boss 연결 확인
            if (_startNode != null && _bossNode != null)
            {
                if (!ReachableOnFloors((_startNode.X, _startNode.Y), (_bossNode.X, _bossNode.Y)))
                    throw new InvalidOperationException("Start is not connected to Boss.");
            }
        }

        public ExploreMapSkeleton ToSkeleton() => _skel;

        #endregion

        #region Position Finding Helpers

        private (int x, int y) FindAdjacentPosition((int x, int y) origin, int targetDistance, int minDistance)
        {
            const int MAX_ATTEMPTS = 100;

            for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
            {
                // 랜덤 방향
                int dx = _rng.Next(-targetDistance, targetDistance + 1);
                int dy = _rng.Next(-targetDistance, targetDistance + 1);

                int dist = Math.Abs(dx) + Math.Abs(dy);
                if (dist < minDistance || dist > targetDistance + 1)
                    continue;

                int nx = origin.x + dx;
                int ny = origin.y + dy;

                if (!_skel.InBounds(nx, ny) || !_interiorMask[Index(nx, ny)])
                    continue;
                if (!IsFarFromEnd(nx, ny, 5)) continue;

                // 다른 노드와 충돌 체크
                bool collision = false;
                foreach (var node in _nodes)
                {
                    if (Math.Abs(node.X - nx) + Math.Abs(node.Y - ny) < 2)
                    {
                        collision = true;
                        break;
                    }
                }

                if (!collision)
                    return (nx, ny);
            }

            throw new InvalidOperationException($"Failed to find adjacent position near ({origin.x}, {origin.y})");
        }

        private (int x, int y) FindOrthNeighbor((int x, int y) origin)
        {
            var candidates = new List<(int x, int y)>
            {
                (origin.x + 1, origin.y), (origin.x - 1, origin.y), (origin.x, origin.y + 1), (origin.x, origin.y - 1),
            };
            // 셔플
            for (int i = 0; i < candidates.Count; i++)
            {
                int j = _rng.Next(i, candidates.Count);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            foreach (var c in candidates)
            {
                if (_skel.InBounds(c.x, c.y) && _interiorMask[Index(c.x, c.y)])
                    return c;
            }

            throw new InvalidOperationException("No orthogonal neighbor for Boss near End");
        }

        private (int x, int y) FindSpineNextPosition(Node fromNode, int spacing, List<Node> avoidNodes)
        {
            const int MAX_ATTEMPTS = 200;

            // 이전 노드의 방향을 고려하여 계속 진행
            int preferredDx = 0, preferredDy = 0;

            if (fromNode.Parent != null)
            {
                preferredDx = Math.Sign(fromNode.X - fromNode.Parent.X);
                preferredDy = Math.Sign(fromNode.Y - fromNode.Parent.Y);
            }

            for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
            {
                int dx, dy;

                if (attempt < MAX_ATTEMPTS / 2 && (preferredDx != 0 || preferredDy != 0))
                {
                    // 전반부: 이전 방향 유지 (약간의 변동)
                    dx = preferredDx * spacing + _rng.Next(-1, 2);
                    dy = preferredDy * spacing + _rng.Next(-1, 2);
                }
                else
                {
                    // 후반부: 랜덤 방향
                    double angle = _rng.NextDouble() * Math.PI * 2;
                    dx = (int)(Math.Cos(angle) * spacing);
                    dy = (int)(Math.Sin(angle) * spacing);
                }

                int nx = fromNode.X + dx;
                int ny = fromNode.Y + dy;

                if (!_skel.InBounds(nx, ny) || !_interiorMask[Index(nx, ny)])
                    continue;

                // 다른 노드와 최소 거리 유지
                bool tooClose = false;
                foreach (var node in avoidNodes)
                {
                    int dist = Math.Abs(node.X - nx) + Math.Abs(node.Y - ny);
                    if (dist < spacing - 1)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                    return (nx, ny);
            }

            return (-1, -1); // 실패
        }

        private (int x, int y) FindSideBranchPosition(Node fromNode, bool goLeft, int distance)
        {
            // 부모 방향 기준으로 좌우 결정
            int mainDx = 0, mainDy = 0;

            if (fromNode.Parent != null)
            {
                mainDx = Math.Sign(fromNode.X - fromNode.Parent.X);
                mainDy = Math.Sign(fromNode.Y - fromNode.Parent.Y);
            }
            else
            {
                // 기본값: 오른쪽
                mainDx = 1;
            }

            // 수직 방향 계산 (좌우)
            int perpDx = -mainDy;
            int perpDy = mainDx;

            if (!goLeft)
            {
                perpDx = -perpDx;
                perpDy = -perpDy;
            }

            for (int attempt = 0; attempt < 50; attempt++)
            {
                int variance = _rng.Next(-1, 2);
                int nx = fromNode.X + perpDx * distance + variance;
                int ny = fromNode.Y + perpDy * distance + variance;

                if (IsEndForbidden(nx, ny) || !IsFarFromEnd(nx, ny, 5)) continue;

                if (!_skel.InBounds(nx, ny) || !_interiorMask[Index(nx, ny)])
                    continue;

                // 다른 노드와 충돌 체크
                bool collision = false;
                foreach (var node in _nodes)
                {
                    if (Math.Abs(node.X - nx) + Math.Abs(node.Y - ny) < 2)
                    {
                        collision = true;
                        break;
                    }
                }

                if (!collision)
                    return (nx, ny);
            }

            return (-1, -1);
        }

        #endregion

        #region Utility

        private int Index(int x, int y) => y * _skel.Width + x;

        private IEnumerable<(int x, int y)> GetNeighbors4(int x, int y)
        {
            if (x > 0) yield return (x - 1, y);
            if (x + 1 < _skel.Width) yield return (x + 1, y);
            if (y > 0) yield return (x, y - 1);
            if (y + 1 < _skel.Height) yield return (x, y + 1);
        }

        private bool ReachableOnFloors((int x, int y) A, (int x, int y) B)
        {
            int W = _skel.Width, H = _skel.Height;
            var q = new Queue<(int x, int y)>();
            var vis = new bool[W * H];
            if (!_skel.IsFloor(A.x, A.y)) return false;

            q.Enqueue(A);
            vis[Index(A.x, A.y)] = true;

            while (q.Count > 0)
            {
                var p = q.Dequeue();
                if (p.x == B.x && p.y == B.y) return true;

                foreach (var n in _skel.GetNeighbors4(p.x, p.y))
                {
                    int ni = Index(n.x, n.y);
                    if (vis[ni]) continue;
                    if (!_skel.IsFloor(n.x, n.y)) continue;
                    vis[ni] = true;
                    q.Enqueue(n);
                }
            }

            return false;
        }

        private bool IsEndForbidden(int x, int y)
        {
            if (_endNode == null) return false;
            if (_bossNode != null && x == _bossNode.X && y == _bossNode.Y) return false;
            int ex = _endNode.X, ey = _endNode.Y;
            int dx = Math.Abs(x - ex), dy = Math.Abs(y - ey);
            if (dx <= 1 && dy <= 1) return !(dx == 0 && dy == 0); // End 자신 제외, 주변 8타일 금지
            return false;
        }

        private bool IsFarFromEnd(int x, int y, int minManhattan)
        {
            if (_endNode == null) return true;
            return Math.Abs(x - _endNode.X) + Math.Abs(y - _endNode.Y) >= minManhattan;
        }

        // 보스→스타트 실제 Floor 경로를 보호 셀로 마킹
        private void ProtectMainRoute()
        {
            _protectedCells.Clear();
            if (_bossNode == null || _startNode == null) return;

            var path = FindPathOnFloors((_bossNode.X, _bossNode.Y), (_startNode.X, _startNode.Y));
            if (path == null) return;
            foreach (var (x, y) in path)
                _protectedCells.Add(Index(x, y));
        }

        // Floor 위 BFS로 실제 경로 복원
        private List<(int x, int y)> FindPathOnFloors((int x, int y) start, (int x, int y) end)
        {
            int W = _skel.Width, H = _skel.Height;
            var q = new Queue<(int x, int y)>();
            var prev = new Dictionary<(int, int), (int, int)>();
            var vis = new bool[W * H];
            if (!_skel.IsFloor(start.x, start.y) || !_skel.IsFloor(end.x, end.y)) return null;

            q.Enqueue(start);
            vis[Index(start.x, start.y)] = true;
            prev[start] = (-1, -1);

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur == end) break;
                foreach (var (nx, ny) in _skel.GetNeighbors4(cur.x, cur.y))
                {
                    int ni = Index(nx, ny);
                    if (vis[ni]) continue;
                    if (!_skel.IsFloor(nx, ny)) continue;
                    vis[ni] = true;
                    prev[(nx, ny)] = cur;
                    q.Enqueue((nx, ny));
                }
            }

            if (!vis[Index(end.x, end.y)]) return null;

            var path = new List<(int x, int y)>();
            var p = end;
            while (p.x != -1)
            {
                path.Add(p);
                p = prev[p];
            }

            path.Reverse();
            return path;
        }

        public void EnsureStartBossConnectivity()
        {
            if (_bossNode == null || _startNode == null) return;
            var boss = (_bossNode.X, _bossNode.Y);
            var start = (_startNode.X, _startNode.Y);

            var path = FindPathOnFloors(boss, start);
            if (path != null && path.Count > 0)
            {
                // 이미 경로가 있으면 보호만 보강
                ProtectMainRoute();
                return;
            }

            // 경로가 없으면 L-자 맨해튼 직조로 최소 경로를 강제로 판다.
            int x = boss.Item1, y = boss.Item2;
            _skel.SetFloor(x, y);
            while (x != start.Item1)
            {
                x += x < start.Item1 ? 1 : -1;
                _skel.SetFloor(x, y);
            }

            while (y != start.Item2)
            {
                y += y < start.Item2 ? 1 : -1;
                _skel.SetFloor(x, y);
            }

            // 강제 경로를 보호한다
            ProtectMainRoute();
        }

        /// <summary>
        /// Boss에서 도달 불가능한 모든 Floor를 제거(벽으로 변경)한다.
        /// 고립된 바닥 섬을 정리하여 다중 컴포넌트 문제를 없앤다.
        /// </summary>
        public void CullFloorsNotReachableFromBoss()
        {
            if (_bossNode == null) return;
            var q = new Queue<(int x, int y)>();
            var vis = new bool[_skel.Width, _skel.Height];
            q.Enqueue((_bossNode.X, _bossNode.Y));
            vis[_bossNode.X, _bossNode.Y] = true;

            while (q.Count > 0)
            {
                var (cx, cy) = q.Dequeue();
                foreach (var (nx, ny) in _skel.GetNeighbors4(cx, cy))
                {
                    if (!vis[nx, ny] && _skel.IsFloor(nx, ny))
                    {
                        vis[nx, ny] = true;
                        q.Enqueue((nx, ny));
                    }
                }
            }

            for (int ix = 0; ix < _skel.Width; ix++)
            for (int iy = 0; iy < _skel.Height; iy++)
            {
                if (_skel.IsFloor(ix, iy) && !vis[ix, iy])
                    _skel.SetWall(ix, iy);
            }
        }
        /// <summary> 앵커 좌표를 강제로 Floor로 보정(심볼 배치 전 안전핀) </summary>
        private void EnsureAnchorsOnFloor()
        {
            if (_startNode != null) _skel.SetFloor(_startNode.X, _startNode.Y);
            if (_bossNode  != null) _skel.SetFloor(_bossNode.X,  _bossNode.Y);
            if (_endNode   != null) _skel.SetFloor(_endNode.X,   _endNode.Y);
        }

        /// <summary> 이미 배치된 지점들과 체비쇼프 거리 기준 최소 간격 확인 </summary>
        private bool IsFarFromPlaced((int x,int y) p, List<(int x,int y)> placed, int minChebyshev)
        {
            foreach (var q in placed)
            {
                int dx = Math.Abs(p.x - q.x);
                int dy = Math.Abs(p.y - q.y);
                if (Math.Max(dx, dy) < minChebyshev) return false;
            }
            return true;
        }
        #endregion
    }
}
