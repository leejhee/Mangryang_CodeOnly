using Core.Scripts.Foundation.Define;
using Core.Scripts.Foundation.Utils;
using GamePlay.Features.Explore.Scripts.Map.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GamePlay.Features.Explore.Scripts.Map.Logic
{
    public class MapBuilder
    {
        private readonly ExploreMapConfig _cfg;
        private readonly GameRandom _rng;
        private readonly ExploreMapSkeleton _skel;

        private bool[] _interiorMask; 
        private List<(int x, int y)> _interiorCells;

        // Anchors (Start/End 후보)
        private (int x, int y) _anchorA;
        private (int x, int y) _anchorB;
        private List<(int x,int y)> _spinePath; // 메인 스파인 경로
        
        public MapBuilder(ExploreMapConfig cfg, ulong seed)
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

            int inside = 0;
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
                        inside++;
                    }
                }
            }
            if (inside == 0) throw new InvalidOperationException("Interior mask is empty. Check grid size.");
        }

        public void InitWalls()
        {
            for (int y = 0; y < _skel.Height; y++)
            for (int x = 0; x < _skel.Width; x++)
                _skel.SetCellType(x, y, SystemEnum.MapCellType.Wall);
        }

        public void ChooseAnchors()
        {
            var start = _interiorCells[_rng.Next(0, _interiorCells.Count)];
            var A = FarthestInteriorByBFS(start);
            var B = FarthestInteriorByBFS(A);
            _anchorA = A;
            _anchorB = B;
        }

        public void CarveMainSpine()
        {
            var path = ShortestPathInside(_anchorA, _anchorB);
            if (path == null || path.Count == 0)
                throw new InvalidOperationException("Failed to carve main spine: no path between anchors.");
            
            _spinePath = path;
            
            foreach (var (x, y) in path)
                _skel.SetCellType(x, y, SystemEnum.MapCellType.Floor);
        }
        
        /// <summary>
        /// 터널러(헤드) 기반 얇은 브랜칭 + 스파인 시딩 + 길이 예산 + 직진 페널티.
        /// - coverage: 브랜치에 쓸 예산 구간
        /// - seedFromSpine: true면 스파인을 따라 간격(step)으로 좌/우 팁 시딩
        /// </summary>
        public void GrowBranchesThin(
            float minCoverage = 0.44f,
            float maxCoverage = 0.52f,
            float junctionChance = 0.12f,
            float forwardBias = 0.78f,
            bool  seedFromSpine = true,
            int   seedStep = 6,
            int   skipFromStart = 2,
            int   skipFromEnd = 2,
            int   maxSeedPerSide = 3,
            int   minLen = 3,           // 팁 길이 예산
            int   maxLen = 12,
            int   straightLimit = 4,    // 이 길이 이상 직진이면 턴 보너스 적용
            float turnBonus = 0.20f,
            int   safetyIter = 220000)
        {
            if (maxCoverage < minCoverage) maxCoverage = minCoverage;
            float target = minCoverage + (float)_rng.NextDouble() * (maxCoverage - minCoverage);
            
            int SampleLen() => Math.Clamp(minLen + (int)Math.Floor(_rng.NextDouble() * (maxLen - minLen + 1)), minLen, maxLen);

            // 스파인 시딩
            var tips = new List<Tip>(256);
            if (seedFromSpine && _spinePath != null && _spinePath.Count >= 3)
            {
                int seededL = 0, seededR = 0;
                for (int i = skipFromStart; i < _spinePath.Count - skipFromEnd; i += Math.Max(1, seedStep))
                {
                    var cur = _spinePath[i];
                    var nxt = _spinePath[Math.Min(i + 1, _spinePath.Count - 1)];
                    int dx = Math.Sign(nxt.x - cur.x);
                    int dy = Math.Sign(nxt.y - cur.y);
                    if (dx == 0 && dy == 0) continue;

                    // 좌/우 후보를 번갈아 시딩
                    foreach (bool pickLeft in new[] { true, false })
                    {
                        if (pickLeft && seededL >= maxSeedPerSide) continue;
                        if (!pickLeft && seededR >= maxSeedPerSide) continue;

                        var (ox, oy) = pickLeft ? Left(dx, dy) : Right(dx, dy);
                        int cx = cur.x + ox, cy = cur.y + oy;
                        if (IsCarvable(cx, cy))
                        {
                            if (pickLeft) seededL++; else seededR++;
                            tips.Add(new Tip { x = cx, y = cy, dx = ox, dy = oy, stepsLeft = SampleLen(), straight = 0 });
                        }
                    }
                }
            }

            // 초기 팁이 부족하면 Floor 주변의 기본 팁도 소량 추가
            if (tips.Count < 2)
            {
                for (int y = 0; y < _skel.Height; y++)
                for (int x = 0; x < _skel.Width; x++)
                {
                    if (_skel.GetCellType(x,y) != SystemEnum.MapCellType.Wall) continue;
                    if (!_interiorMask[Index(x,y)]) continue;
                    int fx=0, fy=0, fcnt=0;
                    foreach (var n in Neigh4(x,y)) if (_skel.IsFloor(n.x,n.y)) { fx=n.x; fy=n.y; fcnt++; }
                    if (fcnt == 1)
                    {
                        int dx = x - fx, dy = y - fy;
                        tips.Add(new Tip{ x=x,y=y,dx=dx,dy=dy, stepsLeft=SampleLen(), straight=0 });
                        if (tips.Count >= 6) break;
                    }
                }
            }

            // ---- 성장 루프 ----
            float Coverage() => GetCoverage();
            int iter = 0;

            while (Coverage() < target && tips.Count > 0 && iter++ < safetyIter)
            {
                int hi = _rng.Next(0, tips.Count);
                var tip = tips[hi];

                // 동적 forwardBias (과도 직진 시 페널티, 턴 보너스)
                float effForward = forwardBias;
                if (tip.straight >= straightLimit) effForward = Math.Max(0.1f, effForward - turnBonus);

                bool carved = false;
                
                int carvedX = -1, carvedY = -1;   // 방금 판 칸
                int moveDx = tip.dx, moveDy = tip.dy; // 그때 쓴 진행 벡터
                
                // 정면 시도
                if (IsCarvable(tip.x, tip.y) && _rng.NextDouble() < effForward)
                {
                    _skel.SetCellType(tip.x, tip.y, SystemEnum.MapCellType.Floor);
                    carvedX = tip.x; carvedY = tip.y;
                    moveDx = tip.dx; moveDy = tip.dy;     // 직진으로 판 것

                    tip.x += tip.dx;                      // ★ 앞으로 전진!
                    tip.y += tip.dy;
                    tip.stepsLeft--; 
                    tip.straight++;

                    if (IsCarvable(tip.x, tip.y)) tips[hi] = tip;
                    else tips.RemoveAt(hi);

                    carved = true;
                }
                else
                {
                    // 좌/우 굽기
                    var cand = new List<(int nx,int ny,int ndx,int ndy)>(2);
                    var (lx,ly) = Left(tip.dx, tip.dy);
                    var (rx,ry) = Right(tip.dx, tip.dy);
                    int lcx = tip.x + lx, lcy = tip.y + ly;
                    if (IsCarvable(lcx,lcy)) cand.Add((lcx,lcy,lx,ly));
                    int rcx = tip.x + rx, rcy = tip.y + ry;
                    if (IsCarvable(rcx,rcy)) cand.Add((rcx,rcy,rx,ry));

                    if (cand.Count > 0)
                    {
                        var pick = cand[_rng.Next(0, cand.Count)];
                        _skel.SetCellType(pick.nx, pick.ny, SystemEnum.MapCellType.Floor);
                        carvedX = pick.nx; carvedY = pick.ny;
                        moveDx = pick.ndx; moveDy = pick.ndy;   // 굽어서 판 진행 벡터

                        tip.x = pick.nx + pick.ndx;             // ★ 전진!
                        tip.y = pick.ny + pick.ndy;
                        tip.dx = pick.ndx; tip.dy = pick.ndy;
                        tip.stepsLeft--;
                        tip.straight = 1;

                        if (IsCarvable(tip.x, tip.y)) tips[hi] = tip;
                        else tips.RemoveAt(hi);

                        carved = true;
                    }
                    else
                    {
                        tips.RemoveAt(hi);
                        continue;
                    }
                }
                
                if (carved)
                {
                    var (sx1, sy1) = Left (moveDx, moveDy);
                    var (sx2, sy2) = Right(moveDx, moveDy);

                    if (_rng.NextDouble() < junctionChance) {
                        int bx = carvedX + sx1, by = carvedY + sy1;
                        if (IsCarvable(bx, by))
                            tips.Add(new Tip{ x=bx, y=by, dx=sx1, dy=sy1, stepsLeft=SampleLen(), straight=0 });
                    }
                    if (_rng.NextDouble() < junctionChance) {
                        int bx = carvedX + sx2, by = carvedY + sy2;
                        if (IsCarvable(bx, by))
                            tips.Add(new Tip{ x=bx, y=by, dx=sx2, dy=sy2, stepsLeft=SampleLen(), straight=0 });
                    }
                }
                
                //// 정면으로 한 칸 앞 갱신(정면을 판 경우)
                //if (tips.Count > 0 && hi < tips.Count)
                //{
                //    // tip.x, tip.y 는 "다음 후보 위치"이어야 함
                //    int nx = tip.x, ny = tip.y;
                //    if (IsCarvable(nx, ny)) tips[hi] = tip;
                //    else tips[hi] = tip; // 그대로 두고 다음 루프에서 못 파면 제거됨
                //}

                // 길이 예산 소진 시 팁 제거
                if (tip.stepsLeft <= 0) { if (hi < tips.Count) tips.RemoveAt(hi); }

                // 분기 팁 스폰 (낮은 확률)
                if (_rng.NextDouble() < junctionChance)
                {
                    var (sx, sy) = Left(tip.dx, tip.dy);
                    int bx = (tip.x - tip.dx) + sx, by = (tip.y - tip.dy) + sy; // 방금 판 자리의 좌측
                    if (IsCarvable(bx, by))
                        tips.Add(new Tip{ x=bx, y=by, dx=sx, dy=sy, stepsLeft=SampleLen(), straight=0 });
                }
                if (_rng.NextDouble() < junctionChance)
                {
                    var (sx, sy) = Right(tip.dx, tip.dy);
                    int bx = (tip.x - tip.dx) + sx, by = (tip.y - tip.dy) + sy;
                    if (IsCarvable(bx, by))
                        tips.Add(new Tip{ x=bx, y=by, dx=sx, dy=sy, stepsLeft=SampleLen(), straight=0 });
                }
            }

            //CornerFix();
            CornerBridgeConservative();
        }

        public void ConnectAllFloors()
        {
            int W = _skel.Width, H = _skel.Height;
            int[] comp = new int[W * H]; Array.Fill(comp, -1);
            var queues = new List<List<(int x,int y)>>();

            // 1) Floor 컴포넌트 라벨링
            int cid = 0;
            for (int y=0; y<H; y++)
            for (int x=0; x<W; x++)
            {
                if (!_skel.IsFloor(x,y) || comp[Index(x,y)] != -1) continue;
                var q = new Queue<(int,int)>(); q.Enqueue((x,y));
                comp[Index(x,y)] = cid;
                var cells = new List<(int,int)>{(x,y)};
                while(q.Count>0){
                    var p=q.Dequeue();
                    foreach(var n in Neigh4(p.Item1,p.Item2)){
                        int ni=Index(n.x,n.y);
                        if(_skel.IsFloor(n.x,n.y) && comp[ni]==-1){
                            comp[ni]=cid; q.Enqueue((n.x,n.y)); cells.Add((n.x,n.y));
                        }
                    }
                }
                queues.Add(cells); cid++;
            }
            if (cid <= 1) return; // 이미 단일 컴포넌트

            // 2) 기준 컴포넌트(가장 큰 것 또는 Start가 포함된 것)
            int mainId = 0;
            int best = queues[0].Count;
            for(int i=1;i<queues.Count;i++) if(queues[i].Count>best){best=queues[i].Count; mainId=i;}

            // 3) 나머지 컴포넌트를 기준과 연결
            var main = new HashSet<int>(queues[mainId].Select(p=>Index(p.Item1,p.Item2)));
            for (int id=0; id<queues.Count; id++)
            {
                if (id==mainId) continue;
                var other = queues[id];

                // 가장 가까운 쌍 찾기 (맨해튼 거리)
                (int ax,int ay) bestA = other[0];
                (int bx,int by) bestB = queues[mainId][0];
                int bestDist = int.MaxValue;
                foreach (var a in other)
                foreach (var b in queues[mainId])
                {
                    int d = Math.Abs(a.Item1-b.Item1)+Math.Abs(a.Item2-b.Item2);
                    if (d < bestDist){ bestDist=d; bestA=a; bestB=b; }
                }

                // interior 기준으로 최단 경로를 캐내면서 연결
                var path = ShortestPathInside(bestA, bestB);
                if (path == null || path.Count==0) continue;
                foreach (var (x,y) in path) _skel.SetCellType(x,y, SystemEnum.MapCellType.Floor);

                // 기준 집합 갱신
                queues[mainId].AddRange(other);
            }
        }
        
        public void CornerFix()
        {
            int W = _skel.Width, H = _skel.Height;

            for (int y = 0; y < H - 1; y++)
            {
                for (int x = 0; x < W - 1; x++)
                {
                    var a = _skel.GetCellType(x, y);
                    var b = _skel.GetCellType(x + 1, y);
                    var c = _skel.GetCellType(x, y + 1);
                    var d = _skel.GetCellType(x + 1, y + 1);

                    // a,d가 Floor이고 b,c가 Wall
                    if (a == SystemEnum.MapCellType.Floor && d == SystemEnum.MapCellType.Floor &&
                        b == SystemEnum.MapCellType.Wall && c == SystemEnum.MapCellType.Wall)
                    {
                        // interior 허용인 쪽 하나를 연다
                        if (_interiorMask[Index(x + 1, y)]) _skel.SetCellType(x + 1, y, SystemEnum.MapCellType.Floor);
                        else if (_interiorMask[Index(x, y + 1)]) _skel.SetCellType(x, y + 1, SystemEnum.MapCellType.Floor);
                    }
                    // b,c가 Floor이고 a,d가 Wall
                    else if (b == SystemEnum.MapCellType.Floor && c == SystemEnum.MapCellType.Floor &&
                             a == SystemEnum.MapCellType.Wall && d == SystemEnum.MapCellType.Wall)
                    {
                        if (_interiorMask[Index(x, y)]) _skel.SetCellType(x, y, SystemEnum.MapCellType.Floor);
                        else if (_interiorMask[Index(x + 1, y + 1)]) _skel.SetCellType(x + 1, y + 1, SystemEnum.MapCellType.Floor);
                    }
                }
            }
        }
        
        public void CornerBridgeConservative()
        {
            int W = _skel.Width, H = _skel.Height;
            for (int y = 0; y < H - 1; y++)
            for (int x = 0; x < W - 1; x++)
            {
                var a = _skel.GetCellType(x, y);
                var b = _skel.GetCellType(x + 1, y);
                var c = _skel.GetCellType(x, y + 1);
                var d = _skel.GetCellType(x + 1, y + 1);

                // 대각선 a-d만 Floor, 나머지 둘은 Wall이면 -> 둘 중 "IsCarvable"인 것만 1칸 열기
                if (a == SystemEnum.MapCellType.Floor && d == SystemEnum.MapCellType.Floor &&
                    b == SystemEnum.MapCellType.Wall  && c == SystemEnum.MapCellType.Wall)
                {
                    if (IsCarvable(x + 1, y)) _skel.SetCellType(x + 1, y, SystemEnum.MapCellType.Floor);
                    else if (IsCarvable(x, y + 1)) _skel.SetCellType(x, y + 1, SystemEnum.MapCellType.Floor);
                }
                // 대각선 b-c만 Floor인 경우도 대칭 처리
                else if (b == SystemEnum.MapCellType.Floor && c == SystemEnum.MapCellType.Floor &&
                         a == SystemEnum.MapCellType.Wall  && d == SystemEnum.MapCellType.Wall)
                {
                    if (IsCarvable(x, y)) _skel.SetCellType(x, y, SystemEnum.MapCellType.Floor);
                    else if (IsCarvable(x + 1, y + 1)) _skel.SetCellType(x + 1, y + 1, SystemEnum.MapCellType.Floor);
                }
            }
        }
        
        public void FillEnclosedWallIslands()
        {
            int W = _skel.Width, H = _skel.Height;
            var vis = new bool[W * H];
            var q = new Queue<(int x, int y)>();

            // 경계에서 시작하는 "바깥 Wall"을 큐에 넣는다.
            void EnqueueIfWall(int x, int y)
            {
                if (_skel.GetCellType(x, y) != SystemEnum.MapCellType.Wall) return;
                int i = Index(x, y);
                if (vis[i]) return;
                vis[i] = true;
                q.Enqueue((x, y));
            }

            for (int x = 0; x < W; x++) { EnqueueIfWall(x, 0); EnqueueIfWall(x, H - 1); }
            for (int y = 0; y < H; y++) { EnqueueIfWall(0, y); EnqueueIfWall(W - 1, y); }

            // 바깥과 연결된 Wall들을 마킹
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                foreach (var n in Neigh4(p.x, p.y))
                {
                    if (_skel.GetCellType(n.x, n.y) != SystemEnum.MapCellType.Wall) continue;
                    int ni = Index(n.x, n.y);
                    if (vis[ni]) continue;
                    vis[ni] = true;
                    q.Enqueue(n);
                }
            }

            // 방문되지 않은 Wall = 내부 섬 → Floor로 채움
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                if (_skel.GetCellType(x, y) == SystemEnum.MapCellType.Wall && !vis[Index(x, y)])
                    _skel.SetCellType(x, y, SystemEnum.MapCellType.Floor);
            }
        }
        
        public void FillTinyWallPockets(int maxSize = 3)
        {
            int W=_skel.Width,H=_skel.Height;
            var vis=new bool[W*H];
            for(int y=1;y<H-1;y++)
            for(int x=1;x<W-1;x++)
            {
                if (vis[Index(x,y)]) continue;
                if (_skel.GetCellType(x,y)!=SystemEnum.MapCellType.Wall) continue;

                // Wall 컴포넌트 BFS
                var q=new Queue<(int,int)>(); var comp=new List<(int,int)>();
                q.Enqueue((x,y)); vis[Index(x,y)]=true;
                bool touchesBorder=false;
                while(q.Count>0){
                    var p=q.Dequeue(); comp.Add(p);
                    if (p.Item1==0||p.Item1==W-1||p.Item2==0||p.Item2==H-1) touchesBorder=true;
                    foreach(var n in Neigh4(p.Item1,p.Item2)){
                        int ni=Index(n.x,n.y);
                        if(vis[ni]) continue;
                        if(_skel.GetCellType(n.x,n.y)!=SystemEnum.MapCellType.Wall) continue;
                        vis[ni]=true; q.Enqueue(n);
                    }
                }
                if (!touchesBorder && comp.Count<=maxSize){
                    foreach(var (cx,cy) in comp) _skel.SetCellType(cx,cy,SystemEnum.MapCellType.Floor);
                }
            }
        }
        
        public void SealBorderWalls(int thickness = 1)
        {
            int W = _skel.Width, H = _skel.Height;
            thickness = Math.Max(1, Math.Min(thickness, Math.Min(W, H) / 2));

            for (int t = 0; t < thickness; t++)
            {
                for (int x = 0; x < W; x++)
                {
                    _skel.SetCellType(x, t,            SystemEnum.MapCellType.Wall);
                    _skel.SetCellType(x, H - 1 - t,    SystemEnum.MapCellType.Wall);
                }
                for (int y = 0; y < H; y++)
                {
                    _skel.SetCellType(t,         y, SystemEnum.MapCellType.Wall);
                    _skel.SetCellType(W - 1 - t, y, SystemEnum.MapCellType.Wall);
                }
            }
        }
        
        public void ScatterSymbols()
        {
            if (_cfg.symbolConfig == null || _cfg.symbolConfig.Count == 0) return;

            // Floor 좌표 수집
            var floorCells = new List<(int x, int y)>();
            for (int y = 0; y < _skel.Height; y++)
                for (int x = 0; x < _skel.Width; x++)
                    if (_skel.IsFloor(x, y)) floorCells.Add((x, y));
            if (floorCells.Count == 0) throw new InvalidOperationException("No Floor cells to place symbols.");

            // 이벤트/아이템 가중치 준비
            float[] eventWeights = _cfg.eventCandidate?.Select(e => (float)Math.Max(0, e.probability)).ToArray();
            float[] itemWeights  = _cfg.itemCandidate?.Select(i => (float)Math.Max(0, i.dropProbability)).ToArray();

            // 앵커 기반 배치 함수
            void PlaceAt((int x, int y) pos, SystemEnum.MapSymbolType type)
            {
                if (type == SystemEnum.MapSymbolType.Event ||
                    type == SystemEnum.MapSymbolType.Item)
                    throw new ArgumentException("Use payload-specific adders for Event/Item.");
                if (!_skel.TryAddSimpleSymbol(pos.x, pos.y, type, out _, out string err))
                    throw new InvalidOperationException($"Failed to place {type} at anchor {pos}: {err}");
            }

            // Start/End 우선 처리 (있을 때만)
            int needStart = _cfg.symbolConfig.Where(s => s.symbolType == SystemEnum.MapSymbolType.StartPoint)
                                             .Sum(s => Math.Max(0, s.symbolCount));
            int needEnd   = _cfg.symbolConfig.Where(s => s.symbolType == SystemEnum.MapSymbolType.EndPoint)
                                             .Sum(s => Math.Max(0, s.symbolCount));

            if (needStart > 0) PlaceAt(_anchorA, SystemEnum.MapSymbolType.StartPoint);
            if (needEnd   > 0) PlaceAt(_anchorB, SystemEnum.MapSymbolType.EndPoint);

            // 그 외 심볼 배치
            foreach (var entry in _cfg.symbolConfig)
            {
                var type = entry.symbolType;
                int need = Math.Max(0, entry.symbolCount);

                // 앵커로 이미 1개를 소진했다면 남은 수량만 진행
                if (type == SystemEnum.MapSymbolType.StartPoint && needStart > 0) { need -= 1; needStart = 0; }
                if (type == SystemEnum.MapSymbolType.EndPoint   && needEnd   > 0) { need -= 1; needEnd   = 0; }
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
                            if (idx < 0) throw new InvalidOperationException("Event  weights sum to 0.");

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
            // 역할: 최소 검증(범위, 겹침, Floor-only, Start-End 경로 등).
            // 실패 시 예외 던지거나 에러 리스트 축적 후 예외.
            int floorCount = 0;
            for (int y = 0; y < _skel.Height; y++)
            for (int x = 0; x < _skel.Width; x++)
                if (_skel.IsFloor(x, y)) floorCount++;

            int symCount = 0;
            foreach (SystemEnum.MapSymbolType t in Enum.GetValues(typeof(SystemEnum.MapSymbolType)))
            {
                if (t == SystemEnum.MapSymbolType.None) continue;
                foreach (var s in _skel.GetSymbolsOfType(t))
                {
                    symCount++;
                    if (_skel.GetCellType(s.X, s.Y) != SystemEnum.MapCellType.Floor)
                        throw new InvalidOperationException($"Symbol {t} not on Floor at ({s.X},{s.Y}).");
                }
            }
            if (symCount > floorCount)
                throw new InvalidOperationException($"Too many symbols ({symCount}) for floors ({floorCount}).");

            // Start/End 경로 확인(둘 다 있으면)
            var starts = _skel.GetSymbolsOfType(SystemEnum.MapSymbolType.StartPoint).ToList();
            var ends   = _skel.GetSymbolsOfType(SystemEnum.MapSymbolType.EndPoint).ToList();
            if (starts.Count > 0 && ends.Count > 0)
            {
                var s0 = (starts[0].X, starts[0].Y);
                var e0 = (ends[0].X,   ends[0].Y);
                if (!ReachableOnFloors(s0, e0))
                    throw new InvalidOperationException("No Floor path between Start and End.");
            }

        }
        
        public ExploreMapSkeleton ToSkeleton() => _skel;
        #endregion
        
        
        #region Internal Util
        private int Index(int x, int y) => y * _skel.Width + x;
        
        private bool InBounds(int x, int y)
        {
            return (uint)x < (uint)_skel.Width && (uint)y < (uint)_skel.Height;
        }
        
        private IEnumerable<(int x, int y)> Neigh4(int x, int y)
        {
            if (x > 0) yield return (x - 1, y);
            if (x + 1 < _skel.Width) yield return (x + 1, y);
            if (y > 0) yield return (x, y - 1);
            if (y + 1 < _skel.Height) yield return (x, y + 1);
        }
        
        private (int x, int y) FarthestInteriorByBFS((int x, int y) src)
        {
            int W = _skel.Width, H = _skel.Height;
            var q = new Queue<(int x, int y)>();
            var dist = new int[W * H];
            Array.Fill(dist, -1);

            int si = Index(src.x, src.y);
            if (!_interiorMask[si]) src = _interiorCells[_rng.Next(0, _interiorCells.Count)];

            q.Enqueue(src);
            dist[Index(src.x, src.y)] = 0;
            (int x, int y) best = src;

            while (q.Count > 0)
            {
                var p = q.Dequeue();
                int d = dist[Index(p.x, p.y)];
                if (d > dist[Index(best.x, best.y)]) best = p;

                foreach (var n in Neigh4(p.x, p.y))
                {
                    int ni = Index(n.x, n.y);
                    if (!_interiorMask[ni]) continue;
                    if (dist[ni] != -1) continue;
                    dist[ni] = d + 1;
                    q.Enqueue(n);
                }
            }
            return best;
        }
        
        private List<(int x, int y)> ShortestPathInside((int x, int y) A, (int x, int y) B)
        {
            int W = _skel.Width, H = _skel.Height;
            var q = new Queue<(int x, int y)>();
            var prev = new (int x, int y)[W * H];
            var vis = new bool[W * H];

            q.Enqueue(A);
            vis[Index(A.x, A.y)] = true;
            prev[Index(A.x, A.y)] = (-1, -1);

            while (q.Count > 0)
            {
                var p = q.Dequeue();
                if (p.x == B.x && p.y == B.y) break;

                foreach (var n in Neigh4(p.x, p.y))
                {
                    int ni = Index(n.x, n.y);
                    if (!_interiorMask[ni] || vis[ni]) continue;
                    vis[ni] = true;
                    prev[ni] = p;
                    q.Enqueue(n);
                }
            }

            if (!vis[Index(B.x, B.y)]) return null;

            var path = new List<(int x, int y)>();
            var cur = B;
            while (cur.x != -1)
            {
                path.Add(cur);
                var p = prev[Index(cur.x, cur.y)];
                cur = p;
            }
            path.Reverse();
            return path;
        }
        
        private bool ReachableOnFloors((int x, int y) A, (int x, int y) B)
        {
            int W = _skel.Width, H = _skel.Height;
            var q = new Queue<(int x, int y)>();
            var vis = new bool[W * H];
            if (_skel.GetCellType(A.x, A.y) != SystemEnum.MapCellType.Floor) return false;

            q.Enqueue(A);
            vis[Index(A.x, A.y)] = true;

            while (q.Count > 0)
            {
                var p = q.Dequeue();
                if (p.x == B.x && p.y == B.y) return true;

                foreach (var n in Neigh4(p.x, p.y))
                {
                    int ni = Index(n.x, n.y);
                    if (vis[ni]) continue;
                    if (_skel.GetCellType(n.x, n.y) != SystemEnum.MapCellType.Floor) continue;
                    vis[ni] = true;
                    q.Enqueue(n);
                }
            }
            return false;
        }
        
        private int CountInterior()
        {
            int W = _skel.Width, H = _skel.Height, c = 0;
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                if (_interiorMask[Index(x,y)]) c++;
            return c;
        }
        private int CountFloors()
        {
            int W = _skel.Width, H = _skel.Height, c = 0;
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                if (_skel.IsFloor(x,y)) c++;
            return c;
        }
        private float GetCoverage()
        {
            int interior = CountInterior();
            if (interior == 0) return 0f;
            return (float)CountFloors() / interior;
        }

        private void CollectFrontier(HashSet<int> frontier)
        {
            frontier.Clear();
            int W = _skel.Width, H = _skel.Height;
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                if (!_skel.IsFloor(x,y)) continue;
                foreach (var n in Neigh4(x,y))
                {
                    int ni = Index(n.x,n.y);
                    if (!_interiorMask[ni]) continue;
                    if (_skel.GetCellType(n.x,n.y) == SystemEnum.MapCellType.Wall)
                        frontier.Add(ni);
                }
            }
        }
        
        // MapBuilder 내부

        private struct Tip
        {
            public int x, y;       // 현재 후보 칸
            public int dx, dy;     // 진행 방향
            public int stepsLeft;  // 남은 길이 예산
            public int straight;   // 직진 연속 카운트
        }

        bool IsCarvable(int x, int y) 
        {
            if (!_interiorMask[Index(x,y)]) return false;
            if (_skel.GetCellType(x,y) != SystemEnum.MapCellType.Wall) return false;
            // 인접 Floor ≤ 1
            int f = 0;
            foreach (var n in Neigh4(x, y)) if (_skel.IsFloor(n.x, n.y)) f++;
            if (f != 1) return false;
            // 2x2 금지 (후보를 Floor로 가정하고 검증)
            if (Creates2x2IfCarved(x,y)) return false;
            return true;
        }

        bool Creates2x2IfCarved(int x, int y) 
        {
            // (x,y)를 Floor라고 가정하고 네 개 블록 검사
            // [x,y], [x+1,y], [x,y+1], [x+1,y+1] / 반시계 4블록
            int[,] d = {{0,0},{1,0},{0,1},{1,1}};
            for (int ox = -1; ox <= 0; ox++)
            for (int oy = -1; oy <= 0; oy++) {
                int cnt = 0;
                for (int k=0;k<4;k++){
                    int cx = x + ox + d[k,0], cy = y + oy + d[k,1];
                    if (!InBounds(cx,cy)) { cnt = -100; break; }
                    if ((cx==x && cy==y) || _skel.IsFloor(cx,cy)) cnt++;
                }
                if (cnt == 4) return true;
            }
            return false;
        }

        static (int, int) Left(int dx,int dy)  => (-dy, dx);
        static (int, int) Right(int dx,int dy) => (dy, -dx);

        

        #endregion
    }
}