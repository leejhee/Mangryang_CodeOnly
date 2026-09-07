using Core.Scripts.Foundation.Define;
using Core.Scripts.Foundation.Utils;
using GamePlay.Features.Explore.Scripts.Map.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlay.Features.Explore.Scripts.Map.Logic
{
    /// <summary>
    /// Cellular Automata 기반 밀집형 맵 생성기
    /// 레퍼런스와 같은 균일하고 빽빽한 맵 생성
    /// </summary>
    public class DenseMapBuilder
    {
        private readonly ExploreMapConfig _cfg;
        private readonly GameRandom _rng;
        private readonly ExploreMapSkeleton _skel;

        private bool[] _interiorMask;
        private List<(int x, int y)> _interiorCells;

        // Anchors (Start/End)
        private (int x, int y) _anchorA;
        private (int x, int y) _anchorB;

        public DenseMapBuilder(ExploreMapConfig cfg, ulong seed)
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

        /// <summary>
        /// 1단계: 타원형 내부 마스크 계산
        /// </summary>
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

                    // 경계 제외
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
                throw new InvalidOperationException("Interior mask is empty. Check grid size.");
        }

        /// <summary>
        /// 2단계: 전체를 Wall로 초기화
        /// </summary>
        public void InitWalls()
        {
            for (int y = 0; y < _skel.Height; y++)
                for (int x = 0; x < _skel.Width; x++)
                    _skel.SetCellType(x, y, SystemEnum.MapCellType.Wall);
        }

        /// <summary>
        /// 3단계: 내부 영역을 랜덤하게 Floor/Wall로 채우기
        /// </summary>
        public void RandomFillInterior(float floorProbability = 0.70f)
        {
            foreach (var (x, y) in _interiorCells)
            {
                if (_rng.NextDouble() < floorProbability)
                {
                    _skel.SetCellType(x, y, SystemEnum.MapCellType.Floor);
                }
                // else는 이미 Wall
            }
        }

        /// <summary>
        /// 4단계: Cellular Automata 규칙 적용
        /// </summary>
        public void ApplyCellularAutomata(int iterations = 4)
        {
            for (int iter = 0; iter < iterations; iter++)
            {
                ApplyCARuleOnce();
            }
        }

        private void ApplyCARuleOnce()
        {
            var newCells = _skel.CloneCells();

            foreach (var (x, y) in _interiorCells)
            {
                int floorNeighbors = CountFloorNeighbors4(x, y);

                // CA 규칙:
                // - 인접 Floor가 3개 이상 → Floor
                // - 인접 Floor가 2개 이하 → Wall
                // (4방향 기준이므로 최대 4개)
                if (floorNeighbors >= 3)
                    newCells[Index(x, y)] = SystemEnum.MapCellType.Floor;
                else if (floorNeighbors <= 1)
                    newCells[Index(x, y)] = SystemEnum.MapCellType.Wall;
                // 2개면 현재 상태 유지
            }

            // 업데이트
            foreach (var (x, y) in _interiorCells)
            {
                _skel.SetCellType(x, y, newCells[Index(x, y)]);
            }
        }

        private int CountFloorNeighbors4(int x, int y)
        {
            int count = 0;
            foreach (var (nx, ny) in _skel.GetNeighbors4(x, y))
            {
                if (_skel.IsFloor(nx, ny)) count++;
            }
            return count;
        }

        /// <summary>
        /// 5단계: Start/End Anchor 선택
        /// </summary>
        public void ChooseAnchors()
        {
            // Floor 셀 중 랜덤 선택
            var floorCells = _interiorCells.Where(c => _skel.IsFloor(c.x, c.y)).ToList();
            if (floorCells.Count < 2)
                throw new InvalidOperationException("Not enough floor cells to choose anchors.");

            var start = floorCells[_rng.Next(0, floorCells.Count)];
            var A = FarthestFloorByBFS(start);
            var B = FarthestFloorByBFS(A);
            _anchorA = A;
            _anchorB = B;
        }

        /// <summary>
        /// 6단계: 모든 Floor 영역 연결 보장
        /// </summary>
        public void ConnectAllFloors()
        {
            int W = _skel.Width, H = _skel.Height;
            int[] comp = new int[W * H];
            Array.Fill(comp, -1);
            var components = new List<List<(int x, int y)>>();

            // 1) Floor 컴포넌트 라벨링
            int cid = 0;
            foreach (var (x, y) in _interiorCells)
            {
                if (!_skel.IsFloor(x, y) || comp[Index(x, y)] != -1) continue;

                var q = new Queue<(int, int)>();
                q.Enqueue((x, y));
                comp[Index(x, y)] = cid;
                var cells = new List<(int, int)> { (x, y) };

                while (q.Count > 0)
                {
                    var p = q.Dequeue();
                    foreach (var n in _skel.GetNeighbors4(p.Item1, p.Item2))
                    {
                        int ni = Index(n.x, n.y);
                        if (_skel.IsFloor(n.x, n.y) && comp[ni] == -1)
                        {
                            comp[ni] = cid;
                            q.Enqueue((n.x, n.y));
                            cells.Add((n.x, n.y));
                        }
                    }
                }
                components.Add(cells);
                cid++;
            }

            if (components.Count <= 1) return; // 이미 단일 컴포넌트

            // 2) 가장 큰 컴포넌트를 메인으로
            int mainId = 0;
            int best = components[0].Count;
            for (int i = 1; i < components.Count; i++)
            {
                if (components[i].Count > best)
                {
                    best = components[i].Count;
                    mainId = i;
                }
            }

            // 3) 나머지를 메인과 연결
            for (int id = 0; id < components.Count; id++)
            {
                if (id == mainId) continue;

                var other = components[id];
                (int ax, int ay) bestA = other[0];
                (int bx, int by) bestB = components[mainId][0];
                int bestDist = int.MaxValue;

                // 가장 가까운 쌍 찾기
                foreach (var a in other)
                {
                    foreach (var b in components[mainId])
                    {
                        int d = Math.Abs(a.Item1 - b.Item1) + Math.Abs(a.Item2 - b.Item2);
                        if (d < bestDist)
                        {
                            bestDist = d;
                            bestA = a;
                            bestB = b;
                        }
                    }
                }

                // 직선 경로로 연결
                ConnectTwoPoints(bestA, bestB);

                // 메인 컴포넌트에 병합
                components[mainId].AddRange(other);
            }
        }

        private void ConnectTwoPoints((int x, int y) A, (int x, int y) B)
        {
            // 간단한 L자 연결 (X축 먼저, Y축 다음)
            int x = A.x, y = A.y;

            // X축 이동
            while (x != B.x)
            {
                if (_interiorMask[Index(x, y)])
                    _skel.SetCellType(x, y, SystemEnum.MapCellType.Floor);
                x += Math.Sign(B.x - x);
            }

            // Y축 이동
            while (y != B.y)
            {
                if (_interiorMask[Index(x, y)])
                    _skel.SetCellType(x, y, SystemEnum.MapCellType.Floor);
                y += Math.Sign(B.y - y);
            }
        }

        /// <summary>
        /// 7단계: 경계 벽 봉인
        /// </summary>
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
        /// 8단계: 심볼 배치
        /// </summary>
        public void ScatterSymbols()
        {
            if (_cfg.symbolConfig == null || _cfg.symbolConfig.Count == 0) return;

            // Floor 좌표 수집
            var floorCells = new List<(int x, int y)>();
            for (int y = 0; y < _skel.Height; y++)
                for (int x = 0; x < _skel.Width; x++)
                    if (_skel.IsFloor(x, y)) floorCells.Add((x, y));

            if (floorCells.Count == 0)
                throw new InvalidOperationException("No Floor cells to place symbols.");

            // 가중치 준비
            float[] eventWeights = _cfg.eventCandidate?.Select(e => (float)Math.Max(0, e.probability)).ToArray();
            float[] itemWeights = _cfg.itemCandidate?.Select(i => (float)Math.Max(0, i.dropProbability)).ToArray();

            // Start/End 배치
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

            // 나머지 심볼 배치
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

        /// <summary>
        /// 9단계: 기본 검증
        /// </summary>
        public void BasicValidate()
        {
            int floorCount = 0;
            for (int y = 0; y < _skel.Height; y++)
                for (int x = 0; x < _skel.Width; x++)
                    if (_skel.IsFloor(x, y)) floorCount++;

            if (floorCount == 0)
                throw new InvalidOperationException("No floor cells generated.");

            // Start/End 경로 확인
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

        #region Utility Methods

        private int Index(int x, int y) => y * _skel.Width + x;

        private (int x, int y) FarthestFloorByBFS((int x, int y) src)
        {
            int W = _skel.Width, H = _skel.Height;
            var q = new Queue<(int x, int y)>();
            var dist = new int[W * H];
            Array.Fill(dist, -1);

            q.Enqueue(src);
            dist[Index(src.x, src.y)] = 0;
            (int x, int y) best = src;

            while (q.Count > 0)
            {
                var p = q.Dequeue();
                int d = dist[Index(p.x, p.y)];
                if (d > dist[Index(best.x, best.y)]) best = p;

                foreach (var n in _skel.GetNeighbors4(p.x, p.y))
                {
                    int ni = Index(n.x, n.y);
                    if (!_skel.IsFloor(n.x, n.y)) continue;
                    if (dist[ni] != -1) continue;
                    dist[ni] = d + 1;
                    q.Enqueue(n);
                }
            }
            return best;
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

        #endregion
    }
}