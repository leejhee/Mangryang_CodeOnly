using Core.Scripts.Foundation.Define;
using Core.Scripts.Foundation.Utils;
using GamePlay.Features.Explore.Scripts.Map.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlay.Features.Explore.Scripts.Map.Logic
{
    /// <summary>
    /// BSP 기반 Room+Corridor 맵 생성기
    /// 레퍼런스처럼 명확한 방과 복도 구조
    /// </summary>
    public class BSPMapBuilder
    {
        private readonly ExploreMapConfig _cfg;
        private readonly GameRandom _rng;
        private readonly ExploreMapSkeleton _skel;

        private bool[] _interiorMask;
        private List<(int x, int y)> _interiorCells;

        private (int x, int y) _anchorA;
        private (int x, int y) _anchorB;

        private List<Room> _rooms = new();
        private List<Corridor> _corridors = new();

        private class Room
        {
            public int X, Y, W, H;
            public (int x, int y) Center => (X + W / 2, Y + H / 2);
        }

        private class Corridor
        {
            public List<(int x, int y)> Path;
        }

        private class BSPNode
        {
            public int X, Y, W, H;
            public BSPNode Left, Right;
            public Room Room;
        }

        public BSPMapBuilder(ExploreMapConfig cfg, ulong seed)
        {
            _cfg = cfg;
            _rng = new GameRandom((ulong)seed);
            _skel = new ExploreMapSkeleton(
                dungeonName: cfg.dungeonName.ToString(),
                floor: cfg.floor,
                width: cfg.xCapacity,
                height: cfg.yCapacity,
                seed: seed
            );
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
        /// BSP로 공간 분할 후 방 생성
        /// </summary>
        public void GenerateRoomsAndCorridors(
            int minRoomSize = 5,
            int maxDepth = 4,
            float roomFillRatio = 0.6f,
            int corridorWidth = 1)
        {
            // 1. 내부 영역의 AABB 계산
            int minX = _interiorCells.Min(c => c.x);
            int maxX = _interiorCells.Max(c => c.x);
            int minY = _interiorCells.Min(c => c.y);
            int maxY = _interiorCells.Max(c => c.y);

            var root = new BSPNode
            {
                X = minX,
                Y = minY,
                W = maxX - minX + 1,
                H = maxY - minY + 1
            };

            // 2. BSP 분할
            SplitNode(root, minRoomSize, maxDepth, 0);

            // 3. 리프 노드에 방 생성
            var leaves = CollectLeaves(root);
            foreach (var leaf in leaves)
            {
                CreateRoomInLeaf(leaf, roomFillRatio);
            }

            // 4. 인접 노드들을 복도로 연결
            ConnectRooms(root, corridorWidth);

            // 5. 방과 복도를 Floor로 설정
            foreach (var room in _rooms)
            {
                CarveRoom(room);
            }
            foreach (var corridor in _corridors)
            {
                CarveCorridor(corridor);
            }
        }

        private void SplitNode(BSPNode node, int minSize, int maxDepth, int depth)
        {
            if (depth >= maxDepth || node.W < minSize * 2 || node.H < minSize * 2)
                return;

            // 가로/세로 중 하나 선택
            bool splitHorizontal = _rng.NextDouble() < 0.5;

            if (node.W > node.H && node.W / node.H >= 1.25)
                splitHorizontal = false;
            else if (node.H > node.W && node.H / node.W >= 1.25)
                splitHorizontal = true;

            int max = (splitHorizontal ? node.H : node.W) - minSize;
            if (max <= minSize)
                return;

            // 분할 지점 (중간 ±30% 범위에서 선택)
            int mid = (splitHorizontal ? node.H : node.W) / 2;
            int variation = Math.Max(1, mid / 3);
            int split = mid + _rng.Next(-variation, variation + 1);
            split = Math.Clamp(split, minSize, max);

            if (splitHorizontal)
            {
                node.Left = new BSPNode { X = node.X, Y = node.Y, W = node.W, H = split };
                node.Right = new BSPNode { X = node.X, Y = node.Y + split, W = node.W, H = node.H - split };
            }
            else
            {
                node.Left = new BSPNode { X = node.X, Y = node.Y, W = split, H = node.H };
                node.Right = new BSPNode { X = node.X + split, Y = node.Y, W = node.W - split, H = node.H };
            }

            SplitNode(node.Left, minSize, maxDepth, depth + 1);
            SplitNode(node.Right, minSize, maxDepth, depth + 1);
        }

        private List<BSPNode> CollectLeaves(BSPNode node)
        {
            var leaves = new List<BSPNode>();
            if (node.Left == null && node.Right == null)
            {
                leaves.Add(node);
            }
            else
            {
                if (node.Left != null) leaves.AddRange(CollectLeaves(node.Left));
                if (node.Right != null) leaves.AddRange(CollectLeaves(node.Right));
            }
            return leaves;
        }

        private void CreateRoomInLeaf(BSPNode leaf, float fillRatio)
        {
            // 리프 영역의 60-80%를 방으로 사용
            int roomW = Math.Max(3, (int)(leaf.W * fillRatio));
            int roomH = Math.Max(3, (int)(leaf.H * fillRatio));

            // 중앙 배치 (약간 랜덤)
            int offsetX = (leaf.W - roomW) / 2 + _rng.Next(-1, 2);
            int offsetY = (leaf.H - roomH) / 2 + _rng.Next(-1, 2);
            offsetX = Math.Clamp(offsetX, 1, leaf.W - roomW - 1);
            offsetY = Math.Clamp(offsetY, 1, leaf.H - roomH - 1);

            var room = new Room
            {
                X = leaf.X + offsetX,
                Y = leaf.Y + offsetY,
                W = roomW,
                H = roomH
            };

            leaf.Room = room;
            _rooms.Add(room);
        }

        private void ConnectRooms(BSPNode node, int corridorWidth)
        {
            if (node.Left == null || node.Right == null)
                return;

            // 재귀적으로 자식 먼저 연결
            ConnectRooms(node.Left, corridorWidth);
            ConnectRooms(node.Right, corridorWidth);

            // 왼쪽/오른쪽 서브트리의 대표 방 찾기
            var leftRooms = CollectLeaves(node.Left).Where(n => n.Room != null).Select(n => n.Room).ToList();
            var rightRooms = CollectLeaves(node.Right).Where(n => n.Room != null).Select(n => n.Room).ToList();

            if (leftRooms.Count == 0 || rightRooms.Count == 0)
                return;

            // 랜덤 선택
            var roomA = leftRooms[_rng.Next(0, leftRooms.Count)];
            var roomB = rightRooms[_rng.Next(0, rightRooms.Count)];

            // L자 복도 생성
            var corridor = CreateLCorridor(roomA.Center, roomB.Center, corridorWidth);
            _corridors.Add(corridor);
        }

        private Corridor CreateLCorridor((int x, int y) start, (int x, int y) end, int width)
        {
            var path = new List<(int x, int y)>();

            // X축 먼저 이동 (50% 확률로 Y축 먼저)
            bool xFirst = _rng.NextDouble() < 0.5;

            if (xFirst)
            {
                // X축 이동
                int x = start.x;
                while (x != end.x)
                {
                    for (int w = 0; w < width; w++)
                        path.Add((x, start.y + w - width / 2));
                    x += Math.Sign(end.x - x);
                }

                // Y축 이동
                int y = start.y;
                while (y != end.y)
                {
                    for (int w = 0; w < width; w++)
                        path.Add((end.x + w - width / 2, y));
                    y += Math.Sign(end.y - y);
                }
            }
            else
            {
                // Y축 먼저
                int y = start.y;
                while (y != end.y)
                {
                    for (int w = 0; w < width; w++)
                        path.Add((start.x + w - width / 2, y));
                    y += Math.Sign(end.y - y);
                }

                // X축
                int x = start.x;
                while (x != end.x)
                {
                    for (int w = 0; w < width; w++)
                        path.Add((x, end.y + w - width / 2));
                    x += Math.Sign(end.x - x);
                }
            }

            return new Corridor { Path = path.Distinct().ToList() };
        }

        private void CarveRoom(Room room)
        {
            for (int y = room.Y; y < room.Y + room.H; y++)
            {
                for (int x = room.X; x < room.X + room.W; x++)
                {
                    if (_skel.InBounds(x, y) && _interiorMask[Index(x, y)])
                    {
                        _skel.SetCellType(x, y, SystemEnum.MapCellType.Floor);
                    }
                }
            }
        }

        private void CarveCorridor(Corridor corridor)
        {
            foreach (var (x, y) in corridor.Path)
            {
                if (_skel.InBounds(x, y) && _interiorMask[Index(x, y)])
                {
                    _skel.SetCellType(x, y, SystemEnum.MapCellType.Floor);
                }
            }
        }

        public void ChooseAnchors()
        {
            if (_rooms.Count < 2)
                throw new InvalidOperationException("Not enough rooms to choose anchors.");

            // 가장 먼 두 방 선택
            Room furthestA = _rooms[0], furthestB = _rooms[1];
            int maxDist = 0;

            for (int i = 0; i < _rooms.Count; i++)
            {
                for (int j = i + 1; j < _rooms.Count; j++)
                {
                    var a = _rooms[i].Center;
                    var b = _rooms[j].Center;
                    int dist = Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
                    if (dist > maxDist)
                    {
                        maxDist = dist;
                        furthestA = _rooms[i];
                        furthestB = _rooms[j];
                    }
                }
            }

            _anchorA = furthestA.Center;
            _anchorB = furthestB.Center;
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

        /// <summary>
        /// 추가 가지치기: 막다른 복도 제거 (선택사항)
        /// </summary>
        public void PruneDeadEnds(int iterations = 1)
        {
            for (int iter = 0; iter < iterations; iter++)
            {
                var toRemove = new List<(int x, int y)>();

                for (int y = 1; y < _skel.Height - 1; y++)
                {
                    for (int x = 1; x < _skel.Width - 1; x++)
                    {
                        if (!_skel.IsFloor(x, y)) continue;

                        // 인접 Floor가 1개뿐인 경우 (막다른 길)
                        int neighbors = 0;
                        foreach (var (nx, ny) in _skel.GetNeighbors4(x, y))
                        {
                            if (_skel.IsFloor(nx, ny)) neighbors++;
                        }

                        if (neighbors == 1)
                        {
                            // 방 내부는 제외
                            bool inRoom = _rooms.Any(r =>
                                x >= r.X && x < r.X + r.W &&
                                y >= r.Y && y < r.Y + r.H);

                            if (!inRoom)
                                toRemove.Add((x, y));
                        }
                    }
                }

                foreach (var (x, y) in toRemove)
                {
                    _skel.SetCellType(x, y, SystemEnum.MapCellType.Wall);
                }

                if (toRemove.Count == 0) break;
            }
        }

        public void ScatterSymbols()
        {
            if (_cfg.symbolConfig == null || _cfg.symbolConfig.Count == 0) return;

            var floorCells = new List<(int x, int y)>();
            for (int y = 0; y < _skel.Height; y++)
                for (int x = 0; x < _skel.Width; x++)
                    if (_skel.IsFloor(x, y)) floorCells.Add((x, y));

            if (floorCells.Count == 0)
                throw new InvalidOperationException("No Floor cells.");

            float[] eventWeights = _cfg.eventCandidate?.Select(e => (float)Math.Max(0, e.probability)).ToArray();
            float[] itemWeights = _cfg.itemCandidate?.Select(i => (float)Math.Max(0, i.dropProbability)).ToArray();

            int needStart = _cfg.symbolConfig.Where(s => s.symbolType == SystemEnum.MapSymbolType.StartPoint)
                                             .Sum(s => Math.Max(0, s.symbolCount));
            int needEnd = _cfg.symbolConfig.Where(s => s.symbolType == SystemEnum.MapSymbolType.EndPoint)
                                           .Sum(s => Math.Max(0, s.symbolCount));

            if (needStart > 0)
            {
                if (!_skel.TryAddSimpleSymbol(_anchorA.x, _anchorA.y, SystemEnum.MapSymbolType.StartPoint, out _, out string err))
                    throw new InvalidOperationException($"Failed to place StartPoint: {err}");
            }

            if (needEnd > 0)
            {
                if (!_skel.TryAddSimpleSymbol(_anchorB.x, _anchorB.y, SystemEnum.MapSymbolType.EndPoint, out _, out string err))
                    throw new InvalidOperationException($"Failed to place EndPoint: {err}");
            }

            foreach (var entry in _cfg.symbolConfig)
            {
                var type = entry.symbolType;
                int need = Math.Max(0, entry.symbolCount);

                if (type == SystemEnum.MapSymbolType.StartPoint && needStart > 0) { need -= 1; needStart = 0; }
                if (type == SystemEnum.MapSymbolType.EndPoint && needEnd > 0) { need -= 1; needEnd = 0; }
                if (need <= 0) continue;

                for (int i = 0; i < need; i++)
                {
                    const int MAX_TRIES = 400;
                    bool ok = false;
                    string lastErr = null;

                    for (int t = 0; t < MAX_TRIES && !ok; t++)
                    {
                        var (x, y) = floorCells[_rng.Next(0, floorCells.Count)];

                        if (type == SystemEnum.MapSymbolType.Event)
                        {
                            if (eventWeights == null || eventWeights.Length == 0)
                                throw new InvalidOperationException("Event symbol requested but eventCandidate is empty.");
                            int idx = _rng.WeightedChoice(eventWeights);
                            if (idx < 0) throw new InvalidOperationException("Event weights sum to 0.");

                            var et = _cfg.eventCandidate[idx].eventType;
                            if (_skel.TryAddEventSymbol(x, y, et, out _, out string err)) ok = true;
                            else lastErr = err;
                        }
                        else if (type == SystemEnum.MapSymbolType.Item)
                        {
                            if (itemWeights == null || itemWeights.Length == 0)
                                throw new InvalidOperationException("Item symbol requested but itemCandidate is empty.");
                            int idx = _rng.WeightedChoice(itemWeights);
                            if (idx < 0) throw new InvalidOperationException("Item weights sum to 0.");

                            long itemIndex = _cfg.itemCandidate[idx].itemIndex;
                            if (_skel.TryAddItemSymbol(x, y, itemIndex, out _, out string err)) ok = true;
                            else lastErr = err;
                        }
                        else
                        {
                            if (_skel.TryAddSimpleSymbol(x, y, type, out _, out string err)) ok = true;
                            else lastErr = err;
                        }
                    }

                    if (!ok)
                        throw new InvalidOperationException($"Failed to place symbol {type}: {lastErr ?? "unknown"}");
                }
            }
        }

        public void BasicValidate()
        {
            int floorCount = 0;
            for (int y = 0; y < _skel.Height; y++)
                for (int x = 0; x < _skel.Width; x++)
                    if (_skel.IsFloor(x, y)) floorCount++;

            if (floorCount == 0)
                throw new InvalidOperationException("No floor cells generated.");

            var starts = _skel.GetSymbolsOfType(SystemEnum.MapSymbolType.StartPoint).ToList();
            var ends = _skel.GetSymbolsOfType(SystemEnum.MapSymbolType.EndPoint).ToList();
            if (starts.Count > 0 && ends.Count > 0)
            {
                var s0 = (starts[0].X, starts[0].Y);
                var e0 = (ends[0].X, ends[0].Y);
                if (!ReachableOnFloors(s0, e0))
                    throw new InvalidOperationException("No Floor path between Start and End.");
            }
        }

        public ExploreMapSkeleton ToSkeleton() => _skel;

        #endregion

        #region Utility

        private int Index(int x, int y) => y * _skel.Width + x;

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

        #endregion
    }
}