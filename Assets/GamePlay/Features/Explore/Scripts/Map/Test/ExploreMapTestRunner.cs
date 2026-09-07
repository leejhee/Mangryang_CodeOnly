using Core.Scripts.Foundation.Define;
using GamePlay.Features.Explore.Scripts.Map.Data;
using GamePlay.Features.Explore.Scripts.Map.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GamePlay.Features.Explore.Scripts.Map.Test
{
    [ExecuteAlways]
    [AddComponentMenu("AngelBeat/Explore Map Test Runner")]
    public sealed class ExploreMapTestRunner : MonoBehaviour
    {
        [Header("Config & Seed")]
        public ExploreMapConfig config;
        public ExploreEventDB eventDB;     // EventType -> Prefab (우선 적용)
        public ExploreSymbolDB symbolDB;   // MapSymbolType -> Prefab (기본/폴백)
        public ulong seed = 12345;
        public bool useRandomSeedOnGenerate = false;

        [Header("Tile Prefabs (override). If null, use config prefabs")]
        public GameObject floorPrefabOverride;
        public GameObject wallPrefabOverride;

        [Header("Isometric Layout")]
        [Tooltip("가로 반쪽 폭. isometric X 좌표 스케일")]
        public float tileWidth = 6.0f;
        [Tooltip("세로 반쪽 높이. isometric Y 좌표 스케일")]
        public float tileHeight = 3.0f;

        [Header("Spawn Options")]
        public bool spawnFloors = true;
        public bool spawnWalls = false;         // 씬 무거워지면 끄고 확인
        public bool spawnSymbols = true;        // ★ 심볼 프리팹 스폰 on/off
        public bool clearBeforeGenerate = true;

        [Header("Symbol Gizmos")]
        public bool drawSymbolGizmos = true;    // 프리팹과 별개로 기즈모도 그림
        public float symbolGizmoSize = 0.3f;

        // 내부 상태(캐시)
        private ExploreMapSkeleton _lastSkeleton;

        // 루트들
        private Transform _mapRoot;     // MapRoot
        private Transform _tilesRoot;   // MapRoot/Tiles
        private Transform _floorsRoot;  // MapRoot/Tiles/Floors
        private Transform _wallsRoot;   // MapRoot/Tiles/Walls
        private Transform _symbolsRoot; // MapRoot/Symbols

        private readonly Dictionary<SystemEnum.MapSymbolType, Color> _symbolColors = new()
        {
            { SystemEnum.MapSymbolType.StartPoint,  new Color(0.2f, 0.9f, 0.3f, 1f) },
            { SystemEnum.MapSymbolType.EndPoint,    new Color(0.9f, 0.2f, 0.2f, 1f) },
            { SystemEnum.MapSymbolType.Battle,      new Color(1f,   0.5f, 0.0f, 1f) },
            { SystemEnum.MapSymbolType.EliteBattle, new Color(1f,   0.8f, 0.0f, 1f) },
            { SystemEnum.MapSymbolType.BossBattle,  new Color(0.7f, 0.0f, 1f, 1f)   },
            { SystemEnum.MapSymbolType.Gather,      new Color(0.0f, 0.7f, 1f, 1f)   },
            { SystemEnum.MapSymbolType.Event,       new Color(1f,   1f,   0.0f, 1f) },
            { SystemEnum.MapSymbolType.Item,        new Color(0.0f, 1f,   1f, 1f)   },
            // { SystemEnum.MapSymbolType.ItemChest,   new Color(0.0f, 0.9f, 0.9f, 1f) },
        };

        // ---------- ContextMenu Actions ----------

        [ContextMenu("Generate (Use Current Seed)")]
        public async void CM_Generate()
        {
            if (config == null)
            {
                Debug.LogError("[ExploreMapTestRunner] Config is null.");
                return;
            }

            if (useRandomSeedOnGenerate)
            {
                byte[] bytes = Guid.NewGuid().ToByteArray();
                seed = BitConverter.ToUInt64(bytes, 0);
                if (seed == 0UL) seed = 1UL;
            }

            try
            {
                if (clearBeforeGenerate) ClearSpawned();

                _lastSkeleton = await ExploreMapGenerator.BuildSkeleton(config, seed);
                BuildViewFromSkeleton(_lastSkeleton);
                LogSummary(_lastSkeleton);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ExploreMapTestRunner] Generate failed: {e.Message}\n{e}");
            }
        }

        [ContextMenu("Reroll Seed & Generate")]
        public void CM_RerollAndGenerate()
        {
            useRandomSeedOnGenerate = true;
            CM_Generate();
            useRandomSeedOnGenerate = false;
        }

        [ContextMenu("Clear Spawned")]
        public void CM_Clear()
        {
            ClearSpawned();
        }

        // ---------- Build (Tiles + Symbols) ----------

        private void BuildViewFromSkeleton(ExploreMapSkeleton skel)
        {
            if (skel == null) return;

            // 루트 확보
            _mapRoot     = EnsureChild(transform, "MapRoot");
            _tilesRoot   = EnsureChild(_mapRoot, "Tiles");
            _symbolsRoot = EnsureChild(_mapRoot, "Symbols");
            _floorsRoot  = EnsureChild(_tilesRoot, "Floors");
            _wallsRoot   = EnsureChild(_tilesRoot, "Walls");

            // 프리팹 소스
            var floorPrefab = floorPrefabOverride != null ? floorPrefabOverride : config.floorPrefab;
            var wallPrefab  = wallPrefabOverride  != null ? wallPrefabOverride  : config.wallPrefab;

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
            // EventDB를 딕셔너리로 변환(없으면 null 유지)
            Dictionary<SystemEnum.CellEventType, GameObject> eventDict = null;
            if (eventDB != null && eventDB.symbols != null && eventDB.symbols.Count > 0)
            {
                eventDict = new();
                foreach (var kv in eventDB.symbols)
                    eventDict[kv.eventType] = kv.symbolPrefab;
            }

            foreach (SystemEnum.MapSymbolType t in Enum.GetValues(typeof(SystemEnum.MapSymbolType)))
            {
                if (t == SystemEnum.MapSymbolType.None) continue;

                foreach (var s in skel.GetSymbolsOfType(t))
                {
                    GameObject prefab = null;

                    if (t == SystemEnum.MapSymbolType.Event)
                    {
                        try
                        {
                            var evType = s.EventType; // nullable일 수 있음
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
                        if (symbolDB != null && symbolDB.TryGet(t, out var pf))
                            prefab = pf;
                        
                        if (prefab == null && t == SystemEnum.MapSymbolType.Item)
                        {
                            try
                            {
                                if (symbolDB != null && symbolDB.TryGet(SystemEnum.MapSymbolType.Item /* or ItemChest */, out var chest))
                                    prefab = chest;
                            }
                            catch { /* enum에 ItemChest가 없는 경우 무시 */ }
                        }
                    }

                    if (prefab != null)
                        Spawn(prefab, CellToWorld(s.X, s.Y), _symbolsRoot);
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

        private void Spawn(GameObject prefab, Vector3 pos, Transform parent)
        {
            if (prefab == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var inst = UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
                if (inst != null)
                {
                    inst.transform.position = pos;
                    inst.name = $"{prefab.name}_{pos.x}_{pos.y}";
                }
                return;
            }
#endif
            var go = Instantiate(prefab, pos, Quaternion.identity, parent);
            go.name = $"{prefab.name}_{pos.x}_{pos.y}";
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
        
        private Vector3 CellToWorld(int x, int y)
        {
            float wx = (x - y) * (tileWidth  * 0.5f);
            float wy = (x + y) * (tileHeight * 0.5f);
            return transform.TransformPoint(new Vector3(wx, wy, 0f));
        }

        // ---------- Gizmos (디버그용) ----------

        private void OnDrawGizmos()
        {
            if (!drawSymbolGizmos || _lastSkeleton == null) return;

            foreach (SystemEnum.MapSymbolType t in Enum.GetValues(typeof(SystemEnum.MapSymbolType)))
            {
                if (t == SystemEnum.MapSymbolType.None) continue;

                Color c;
                if (!_symbolColors.TryGetValue(t, out c))
                    c = Color.white;

                Gizmos.color = c;
                foreach (var s in _lastSkeleton.GetSymbolsOfType(t))
                {
                    var world = CellToWorld(s.X, s.Y);
                    Gizmos.DrawSphere(world, symbolGizmoSize);
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(world + Vector3.up * (symbolGizmoSize * 1.2f), t.ToString());
#endif
                }
            }
        }

        private void OnValidate()
        {
            tileWidth  = Mathf.Max(0.01f, tileWidth);
            tileHeight = Mathf.Max(0.01f, tileHeight);
            symbolGizmoSize = Mathf.Max(0.01f, symbolGizmoSize);
        }

        private void LogSummary(ExploreMapSkeleton skel)
        {
            try
            {
                int floors = 0, walls = 0;
                for (int y = 0; y < skel.Height; y++)
                    for (int x = 0; x < skel.Width; x++)
                        if (skel.GetCellType(x, y) == SystemEnum.MapCellType.Floor) floors++; else walls++;

                int symTotal = 0;
                var perType = new List<string>();
                foreach (SystemEnum.MapSymbolType t in Enum.GetValues(typeof(SystemEnum.MapSymbolType)))
                {
                    if (t == SystemEnum.MapSymbolType.None) continue;
                    int cnt = _lastSkeleton.GetSymbolsOfType(t).Count();
                    symTotal += cnt;
                    perType.Add($"{t}:{cnt}");
                }

                Debug.Log($"[ExploreMapTestRunner] Seed={seed}, Size={skel.Width}x{skel.Height}, Floor={floors}, Wall={walls}, Symbols={symTotal} ({string.Join(", ", perType)})");
            }
            catch { /* no-op */ }
        }
    }
}
