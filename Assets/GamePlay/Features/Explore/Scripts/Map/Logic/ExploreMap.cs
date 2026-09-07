using Core.Scripts.Foundation.Define;
using Cysharp.Threading.Tasks;
using GamePlay.Features.Explore.Scripts.Map.Data;
using GamePlay.Features.Explore.Scripts.Symbol.Encounter;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Features.Explore.Scripts.Map.Logic
{
    public class ExploreMap : MonoBehaviour
    {
        [Header("Config & DB")]
        [SerializeField] private ExploreMapConfigDB configDB;
        [SerializeField] private ExploreEventDB eventDB;  
        [SerializeField] private ExploreSymbolDB symbolDB;

        [Header("Isometric Layout")]
        [Tooltip("가로 반쪽 폭. isometric X 좌표 스케일")]
        [SerializeField] private float tileWidth = 6.0f;
        [Tooltip("세로 반쪽 높이. isometric Y 좌표 스케일")]
        [SerializeField] private float tileHeight = 3.0f;

        [Header("Spawn Options")]
        [SerializeField] private bool spawnFloors = true;
        [SerializeField] private bool spawnWalls = false;
        [SerializeField] private bool spawnSymbols = true;

        // ---------- 맵 상태 ----------

        [SerializeField] private ExploreMapSkeleton currentSkeleton;
        [SerializeField] private ExploreMapConfig currentConfig;
        [SerializeField] private SystemEnum.Dungeon currentDungeon;
        [SerializeField] private int currentFloor;
        [SerializeField] private ulong currentSeed;

        // Hierarchy
        private Transform _mapRoot;     // MapRoot
        private Transform _tilesRoot;   // MapRoot/Tiles
        private Transform _floorsRoot;  // MapRoot/Tiles/Floors
        private Transform _wallsRoot;   // MapRoot/Tiles/Walls
        private Transform _symbolsRoot; // MapRoot/Symbols

        public ExploreMapSkeleton CurrentSkeleton => currentSkeleton;
        public SystemEnum.Dungeon CurrentDungeon => currentDungeon;
        public int CurrentFloor => currentFloor;
        public ulong CurrentSeed => currentSeed;

        public event Action<Vector2Int> OnTileClicked;

        private void Awake()
        {
            EnsureRoots();
        }

        private void EnsureRoots()
        {
            _mapRoot     = EnsureChild(transform, "MapRoot");
            _tilesRoot   = EnsureChild(_mapRoot, "Tiles");
            _symbolsRoot = EnsureChild(_mapRoot, "Symbols");
            _floorsRoot  = EnsureChild(_tilesRoot, "Floors");
            _wallsRoot   = EnsureChild(_tilesRoot, "Walls");
        }
        
        public async UniTask GenerateMap(
            ulong seed,
            int floor,
            SystemEnum.Dungeon dungeon,
            int generationVersion = ExploreMapGenerator.CurrentVersion,
            ulong expectedConfigSignature = 0UL)
        {
            if (configDB == null)
            {
                throw new InvalidOperationException("[ExploreMap] configDB is null.");
            }

            if (!configDB.TryGetConfig(dungeon, floor, out var config))
            {
                throw new InvalidOperationException($"[ExploreMap] Invalid dungeon parameter - {dungeon} & {floor}");
            }

            // 스켈레톤 생성. 실패는 호출자에게 전파해 잘못된 빈 맵으로 탐사를 시작하지 않는다.
            ExploreMapSkeleton generatedSkeleton = await ExploreMapGenerator.BuildSkeleton(
                config,
                seed,
                generationVersion,
                expectedConfigSignature);

            // 생성 성공 뒤에만 현재 상태와 뷰를 교체한다.
            currentConfig  = config;
            currentDungeon = dungeon;
            currentFloor   = floor;
            currentSeed    = seed;
            currentSkeleton = generatedSkeleton;

            ClearSpawned();
            EnsureRoots();

            // 타일/심볼 생성
            BuildViewFromSkeleton(currentSkeleton);

#if UNITY_EDITOR
            Debug.Log($"[ExploreMap] Map generated successfully: {dungeon} Floor {floor}, Size: {currentSkeleton.Width}x{currentSkeleton.Height}, Seed={seed}, Generator=v{currentSkeleton.GenerationVersion}, Config={currentSkeleton.ConfigSignature:X16}");
#endif
        }

        // ---------- Build (Tiles + Symbols) ----------

        private void BuildViewFromSkeleton(ExploreMapSkeleton skel)
        {
            if (skel == null) return;

            EnsureRoots();

            var floorPrefab = currentConfig != null ? currentConfig.floorPrefab : null;
            var wallPrefab  = currentConfig != null ? currentConfig.wallPrefab  : null;

            // 타일 스폰
            for (int y = 0; y < skel.Height; y++)
            {
                for (int x = 0; x < skel.Width; x++)
                {
                    var type = skel.GetCellType(x, y);
                    if (type == SystemEnum.MapCellType.Floor && spawnFloors && floorPrefab != null)
                        Spawn(floorPrefab, CellToWorld(x, y), _floorsRoot);
                    else if (type == SystemEnum.MapCellType.Wall && spawnWalls && wallPrefab != null)
                        Spawn(wallPrefab, CellToWorld(x, y), _wallsRoot);
                }
            }

            // 심볼 스폰
            if (spawnSymbols)
                BuildSymbolsFromSkeleton(skel);
        }

        private void BuildSymbolsFromSkeleton(ExploreMapSkeleton skel)
        {
            Dictionary<SystemEnum.CellEventType, GameObject> eventDict = null;
            if (eventDB != null && eventDB.symbols != null && eventDB.symbols.Count > 0)
            {
                eventDict = new();
                foreach (var kv in eventDB.symbols)
                    eventDict[kv.eventType] = kv.symbolPrefab;
            }
            
            var cleared = ExploreSession.Instance?.ClearedSymbol;
            
            foreach (SystemEnum.MapSymbolType t in Enum.GetValues(typeof(SystemEnum.MapSymbolType)))
            {
                if (t == SystemEnum.MapSymbolType.None) continue;

                foreach (var s in skel.GetSymbolsOfType(t))
                {
                    int cellIndex = s.Y * skel.Width + s.X;

                    if (cleared != null && cleared.Contains(cellIndex))
                        continue;
                    
                    GameObject prefab = null;

                    if (t == SystemEnum.MapSymbolType.Event)
                    {
                        try
                        {
                            var evType = s.EventType;
                            if (eventDict != null && evType.HasValue && eventDict.TryGetValue(evType.Value, out var evPf))
                                prefab = evPf;
                            else if (symbolDB != null && symbolDB.TryGet(SystemEnum.MapSymbolType.Event, out var genericEv))
                                prefab = genericEv;
                        }
                        catch
                        {
                            if (symbolDB != null && symbolDB.TryGet(SystemEnum.MapSymbolType.Event, out var genericEv))
                                prefab = genericEv;
                        }
                    }
                    else
                    {
                        // 일반 심볼: 심볼DB에서 타입 매핑
                        if (symbolDB != null && symbolDB.TryGet(t, out var pf))
                            prefab = pf;

                        // 아이템 계열 폴백
                        if (prefab == null && t == SystemEnum.MapSymbolType.Item)
                        {
                            try
                            {
                                if (symbolDB != null && symbolDB.TryGet(SystemEnum.MapSymbolType.Item, out var chest))
                                    prefab = chest;
                            }
                            catch { }
                        }
                    }

                    if (prefab != null)
                    {
                        var world = CellToWorld(s.X, s.Y);
                        var go = Spawn(prefab, world, _symbolsRoot);
                        if (go != null)
                        {
                            EncounterSymbol disposal = go.GetComponent<EncounterSymbol>();
                            if (t is SystemEnum.MapSymbolType.Battle or
                                SystemEnum.MapSymbolType.EliteBattle or
                                SystemEnum.MapSymbolType.BossBattle)
                            {
                                ExploreBattleSymbol battleSymbol = disposal as ExploreBattleSymbol;
                                if (battleSymbol == null)
                                {
                                    battleSymbol = go.AddComponent<ExploreBattleSymbol>();
                                    disposal = battleSymbol;

                                    if (!go.TryGetComponent(out Collider2D _))
                                    {
                                        CircleCollider2D trigger = go.AddComponent<CircleCollider2D>();
                                        trigger.isTrigger = true;
                                        trigger.radius = 0.5f;
                                    }
                                }

                                battleSymbol.Configure(t);
                            }

                            if (disposal != null)
                                disposal.InitializeCellIndex(cellIndex);
                        }
                    }
                }
            }
        }

        // ---------- Utils ----------

        private Transform EnsureChild(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                t = go.transform;
            }
            return t;
        }

        private GameObject Spawn(GameObject prefab, Vector3 pos, Transform parent)
        {
            if (prefab == null) return null;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var inst = UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
                if (inst != null)
                {
                    inst.transform.position = pos;
                    inst.name = $"{prefab.name}_{pos.x}_{pos.y}";
                }
                return inst;
            }
#endif
            var go = Instantiate(prefab, pos, Quaternion.identity, parent);
            go.name = $"{prefab.name}_{pos.x}_{pos.y}";
            return go;
        }

        private void ClearSpawned()
        {
            var mapRoot = transform.Find("MapRoot");
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (mapRoot != null) UnityEditor.Undo.DestroyObjectImmediate(mapRoot.gameObject);
            }
            else
            {
                if (mapRoot != null) Destroy(mapRoot.gameObject);
            }
#else
            if (mapRoot != null) Destroy(mapRoot.gameObject);
#endif
        }

        // ---------- Iso Mapping ----------

        /// <summary>
        /// 아이소메트릭(마름모) 월드 좌표 변환
        /// world = ( (x - y) * tileWidth/2, (x + y) * tileHeight/2, 0 )
        /// </summary>
        private Vector3 CellToWorld(int x, int y)
        {
            float wx = (x - y) * (tileWidth  * 0.5f);
            float wy = (x + y) * (tileHeight * 0.5f);
            return transform.TransformPoint(new Vector3(wx, wy, 0f));
        }

        public Vector3 CellToWorld(Vector2Int position)
        {
            return CellToWorld(position.x, position.y);
        }
        // ---------- ExploreManager가 쓰는 API ----------

        public Vector2Int GetMapSize()
        {
            if (currentSkeleton == null) return Vector2Int.zero;
            return new Vector2Int(currentSkeleton.Width, currentSkeleton.Height);
        }

        public int GetCellIndex(Vector2Int position)
        {
            if (currentSkeleton == null) return -1;
            return position.y * currentSkeleton.Width + position.x;
        }

        public bool IsValidPosition(Vector2Int position)
        {
            if (currentSkeleton == null) return false;
            return position.x >= 0 && position.x < currentSkeleton.Width &&
                   position.y >= 0 && position.y < currentSkeleton.Height;
        }

        public bool IsFloorTile(Vector2Int position)
        {
            if (!IsValidPosition(position)) return false;
            return currentSkeleton.GetCellType(position.x, position.y) == SystemEnum.MapCellType.Floor;
        }
    }
}
