using Core.Attributes;
using Core.Scripts.Data;
using Core.Scripts.Foundation.Define;
using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Entities.Character;
using GamePlay.Features.Battle.Scripts.Unit;
using System;
using System.Collections.Generic;
using UIs.Runtime;
using UnityEngine;
using static Core.Scripts.Foundation.Define.SystemEnum;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GamePlay.Features.Battle.Scripts.BattleMap
{
    /// <summary>
    /// 전투 스테이지의 런타임용 데이터를 가지며,
    /// 전투 시스템 초기화 시 생성되는 스테이지 인스턴스
    /// </summary>
    [RequireComponent(typeof(Grid)), Serializable]
    public class StageField : MonoBehaviour
    {
        #region In-Editor Field
        private Dictionary<eCharType, List<SpawnData>> _spawnDict = new();
        
        // Spawn/Object 배치는 StageEditor가 관리한다. Inspector 수동 편집은 이중 입력이 되므로 숨긴다.
        [SerializeReference, HideInInspector/*, CustomDisable*/] // 데이터 클래스에서 바로 파싱할 수 있도록 그냥 큰 단위 하나를 만듬
        private BattleFieldSpawnInfo battleSpawnerData = new();
        #endregion
        
        #region Runtime Providing Fields (편집 후 불변.)

        [SerializeField] private GameObject backgroundImage;
        
        /// <summary>
        /// 현재 맵에 존재하는 좌표의 총 사이즈(GridProvider의 초기화에 사용)
        /// </summary>
        [SerializeField] private Vector2Int gridSize;
        
        /// <summary>
        /// 미리보기 전용 그리드 제공자
        /// </summary>
        [SerializeField] private BattleGridProvider gridProvider;

        private Grid _grid;
        private Vector2 _cellWorld;
        private Vector2 _originWorld;
        private bool _runtimeInitialized;

        // StageEditor가 배치된 프리팹에서 자동 생성한다. 수동 좌표 입력은 데이터 불일치의 원인이므로 숨긴다.
        [SerializeField, HideInInspector] private List<Vector2Int> platformGridCells = new();
        [SerializeField, HideInInspector] private List<ObstacleEntry> obstacleGridCells = new();
        [SerializeField, HideInInspector] private List<CoverEntry> coverageGridCells = new();
        public Grid Grid => _grid ? _grid : (_grid = GetComponent<Grid>());
        public Vector2Int GridSize => gridSize;
        public Vector2 CellWorld => _cellWorld;
        public Vector2 OriginWorld => _originWorld;
        
        public List<Vector2Int> PlatformGridCells => platformGridCells ??= new List<Vector2Int>();
        public List<ObstacleEntry> ObstacleGridCells => obstacleGridCells ??= new List<ObstacleEntry>();
        public List<CoverEntry> CoverageGridCells => coverageGridCells ??= new List<CoverEntry>();

        public GameObject BackgroundImageSprite => backgroundImage;

        public Transform PlatformsRoot => GetOrCreateRoot("Platforms");
        public Transform ObjectsRoot => GetOrCreateRoot("Objects");
        #endregion
        
        #region Initialization
        
        private void Awake()
        {
            Debug.Log("Initializing Spawn Data...");
            _spawnDict = battleSpawnerData.Convert2Dict();
            _grid = GetComponent<Grid>();
            if(!gridProvider)
                gridProvider = GetComponentInChildren<BattleGridProvider>();
        }

        private void Start()
        {
            InitializeRuntime();
        }

        /// <summary>
        /// Addressables 인스턴스의 Start 호출 시점에 의존하지 않고 그리드 좌표계를 준비한다.
        /// </summary>
        public void InitializeRuntime()
        {
            if (_runtimeInitialized)
                return;

            _grid ??= GetComponent<Grid>();
            if (!gridProvider)
                gridProvider = GetComponentInChildren<BattleGridProvider>();

            _cellWorld = new Vector2(
                _grid.cellSize.x * transform.lossyScale.x,
                _grid.cellSize.y * transform.lossyScale.y
            );
            
            ComputeGridBasis(out _cellWorld, out _originWorld, out _);
            if (gridProvider)
            {
                gridProvider.ApplySpec(gridSize, _cellWorld, _originWorld);
                gridProvider.InitMask();
                gridProvider.Show(false);
            }
            
            UIManager.Instance.InstantiateBackgroundObject(BackgroundImageSprite);
            _runtimeInitialized = true;
        }

        private void ComputeGridBasis(
            out Vector2 cellWorld,
            out Vector2 originWorld,
            out Vector2 sizeWorld)
        {
            cellWorld = new Vector2(
                _grid.cellSize.x * transform.lossyScale.x,
                _grid.cellSize.y * transform.lossyScale.y
            );
            
            Vector2Int halfGridSize = gridSize / 2;
            originWorld = (Vector2)_grid.GetCellCenterWorld(Vector3Int.zero) - 
                              new Vector2(halfGridSize.x + 0.5f, halfGridSize.y + 0.5f) * cellWorld;
            
            sizeWorld = new Vector2(gridSize.x * cellWorld.x,  gridSize.y * cellWorld.y);
        }

        private Transform GetOrCreateRoot(string rootName)
        {
            Transform root = transform.Find(rootName);
            if (root)
                return root;

            GameObject rootObject = new(rootName);
            root = rootObject.transform;
            root.SetParent(transform, false);
            return root;
        }

        /// <summary>
        /// 런타임과 에디터가 동일한 Map Size, Cell Size, Origin으로 GridProvider를 구성한다.
        /// </summary>
        public void RefreshGridProviderSpec()
        {
            _grid = Grid;
            if (!_grid)
                return;

            if (!gridProvider)
                gridProvider = GetComponentInChildren<BattleGridProvider>(true);
            if (!gridProvider || gridSize.x <= 0 || gridSize.y <= 0)
                return;

            ComputeGridBasis(out _cellWorld, out _originWorld, out _);
            gridProvider.ApplySpec(gridSize, _cellWorld, _originWorld);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            // SpriteRenderer.size 변경은 내부적으로 SendMessage를 발생시키므로
            // Unity가 OnValidate를 처리하는 동안 직접 실행하지 않는다.
            EditorApplication.delayCall -= RefreshGridProviderSpecAfterValidation;
            EditorApplication.delayCall += RefreshGridProviderSpecAfterValidation;
        }

        private void RefreshGridProviderSpecAfterValidation()
        {
            EditorApplication.delayCall -= RefreshGridProviderSpecAfterValidation;
            if (!this || Application.isPlaying)
                return;

            RefreshGridProviderSpec();
        }
#endif
        
        #endregion

        #region Spawning Units for Initialization
        public void SpawnUnit(CharBase charBase, int squadOrder)
        {
            eCharType type = charBase.GetCharType();
            if (squadOrder >= _spawnDict[type].Count)
            {
                Debug.LogError("현재 편성 인원 기준을 맵이 수용하지 못합니다. 데이터 체크 요망");
                return;
            }
            BattleCharManager.Instance.CharGenerate(new CharParameter()
            {
                Scene = eScene.BattleScene,
                GeneratePos = _spawnDict[type][squadOrder].SpawnPosition,
                CharIndex = charBase.Index
            });
        }
        
        public void SpawnAllUnitsByType(eCharType type, List<CharBase> characters)
        {
            if (characters.Count > _spawnDict[type].Count)
            {
                Debug.LogError("현재 편성 인원 기준을 맵이 수용하지 못합니다. 데이터 체크 요망");
                return;
            }
            for (int i = 0; i < characters.Count; i++)
            {
                BattleCharManager.Instance.CharGenerate(new CharParameter()
                {
                    Scene = eScene.BattleScene,
                    GeneratePos = _spawnDict[type][i].SpawnPosition,
                    CharIndex = characters[i].Index
                });
            }
        }
        
        /// <summary>
        /// 스테이지에서 플레이어 파티와 적 캐릭터들을 인스턴스화하고, 그 전체 목록을 반환.
        /// </summary>
        /// <param name="playerParty"> 플레이어 측의 파티 </param>
        /// <returns> 스폰된 모든 캐릭터를 반환합니다. </returns>
        public async UniTask<List<CharBase>> SpawnAllUnits(Party playerParty)
        {
            Debug.Log("Spawning All Units...");
            List<CharBase> battleMembers = new();
            
            //PlayerSide
            List<CharacterModel> partyInfo = playerParty.partyMembers;
            for (int i = 0; i < partyInfo.Count; i++)
            {
                CharacterModel character = partyInfo[i];
                SpawnData data = _spawnDict[eCharType.Player][i];
                CharBase battlePrefab =
                    await BattleCharManager.Instance.CharGenerate(new CharBattleParameter(character, data.SpawnPosition));
                battleMembers.Add(battlePrefab);
            }

            //EnemySide
            foreach (var spawnData in _spawnDict[eCharType.Enemy])
            {
                long idx = spawnData.SpawnCharacterIndex;
                MonsterData data = DataManager.Instance.GetData<MonsterData>(idx);
                CharacterModel model = new(data);
                CharBase battlePrefab =
                    await BattleCharManager.Instance.CharGenerate(new CharBattleParameter(model, spawnData.SpawnPosition));
                battleMembers.Add(battlePrefab);
            }

            return battleMembers;
        }
        
        #endregion
        
        #region Grid Provider Util
        
        public void ShowGridOverlay(bool on)
        {
            gridProvider.Show(on);
            if (!on) { gridProvider.ClearHighlights(); gridProvider.ClearHover(); }
        }

    // 범위 칠하기(“가능 중 불가”만 빨강으로)
        public void PaintRange(IEnumerable<Vector2Int> possible, IEnumerable<Vector2Int> blocked, Vector2Int? selected = null)
        {
            gridProvider.SetHighlights(possible, blocked, selected);
        }

    // 호버 업데이트(좌표 변환 포함 예시)
        public void UpdateHoverFromWorld(Vector2 worldPos)
        {
            var cell = WorldToCell(worldPos);
            if (InBounds(cell)) gridProvider.SetHoverCell(cell);
            else                gridProvider.ClearHover();
        }

    // 좌표 유틸
        public bool InBounds(Vector2Int c) =>
            c.x >= 0 && c.y >= 0 && c.x < gridSize.x && c.y < gridSize.y;

        public bool HasPlatformCell(Vector2Int cell) =>
            PlatformGridCells.Contains(cell);

        public bool HasObstacleCell(Vector2Int cell) =>
            ObstacleGridCells.Exists(entry => entry != null && entry.obstacle && entry.cell == cell);

        public bool HasCoverCell(Vector2Int cell) =>
            CoverageGridCells.Exists(entry => entry != null && entry.cover && entry.cell == cell);

        /// <summary>
        /// BattleStageGrid.IsWalkable의 정적 조건과 같은 에디터용 판정.
        /// 런타임 유닛 점유 여부는 에디터 데이터에 없으므로 여기서는 검사하지 않는다.
        /// 커버는 이동을 막지 않으므로 유효한 셀로 유지한다.
        /// </summary>
        public bool IsEditorWalkableCell(Vector2Int cell) =>
            InBounds(cell) && HasPlatformCell(cell) && !HasObstacleCell(cell);

        public Vector2Int WorldToCell(Vector2 world)
        {
            if (!RefreshGridBasis())
                return default;

            var p = new Vector2((world.x - _originWorld.x) / _cellWorld.x,
                (world.y - _originWorld.y) / _cellWorld.y);
            return new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
        }

        public bool TryWorldToCell(Vector2 world, out Vector2Int cell)
        {
            cell = WorldToCell(world);
            return RefreshGridBasis() && InBounds(cell);
        }

        public Vector2 CellToWorldCenter(Vector2Int cell)
        {
            if (!RefreshGridBasis())
                return transform.position;

            return _originWorld + (Vector2)(cell + Vector2.one * 0.5f) * _cellWorld;
        }

        public Vector2 CellToWorldBottomCenter(Vector2Int cell)
        {
            if (!RefreshGridBasis())
                return transform.position;

            return _originWorld + new Vector2((cell.x + 0.5f) * _cellWorld.x, cell.y * _cellWorld.y);
        }

        private bool RefreshGridBasis()
        {
            _grid = Grid;
            if (!_grid)
                return false;

            ComputeGridBasis(out _cellWorld, out _originWorld, out _);
            return !Mathf.Approximately(_cellWorld.x, 0f) &&
                   !Mathf.Approximately(_cellWorld.y, 0f);
        }
        #endregion
        
        
        #region 에디터 툴용
    #if UNITY_EDITOR
        
        public BattleFieldSpawnInfo LoadSpawnerOnlyInEditor()
        {
            return battleSpawnerData;
        }

        private void OnDrawGizmosSelected()
        {
            _grid = GetComponent<Grid>();
            if (!_grid) return;
            
            ComputeGridBasis(out Vector2 cellW, out Vector2 originW, out Vector2 sizeW);
            
            // Grid 컴포넌트의 원점
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_grid.GetCellCenterWorld(Vector3Int.zero), Mathf.Min(cellW.x, cellW.y) * 0.1f);
            
            // 박스
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(originW + 0.5f * sizeW, sizeW);
            
            // 원점
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(originW, Mathf.Min(cellW.x, cellW.y) * 0.1f);

            var pad = 0.95f;
            // Vector2 -> Vector3 로 사이즈 한 번만 만들어 두기
            var cellSize3 = new Vector3(cellW.x, cellW.y, 0f) * pad;

            HashSet<Vector2Int> platformCells = new(platformGridCells);
            HashSet<Vector2Int> blockedCells = new();
            foreach (ObstacleEntry entry in obstacleGridCells)
            {
                if (entry != null)
                    blockedCells.Add(entry.cell);
            }

            // 에디터 셀 색은 종류가 아니라 이동/유닛 배치 가능 여부만 표현한다.
            foreach (Vector2Int cell in platformCells)
            {
                bool walkable = !blockedCells.Contains(cell);
                Vector2 center2D = originW + (cell + Vector2.one * 0.5f) * cellW;
                Vector3 center3D = new(center2D.x, center2D.y, walkable ? 0f : -0.01f);
                Gizmos.color = walkable
                    ? new Color(0.12f, 0.9f, 0.25f, 0.18f)
                    : new Color(0.95f, 0.12f, 0.12f, 0.18f);
                Gizmos.DrawCube(center3D, cellSize3);
            }

            // 잘못된 과거 데이터처럼 플랫폼 밖에 놓인 장애물도 빨강으로 드러낸다.
            foreach (Vector2Int cell in blockedCells)
            {
                if (platformCells.Contains(cell))
                    continue;

                Vector2 center2D = originW + (cell + Vector2.one * 0.5f) * cellW;
                Gizmos.color = new Color(0.95f, 0.12f, 0.12f, 0.18f);
                Gizmos.DrawCube(new Vector3(center2D.x, center2D.y, -0.01f), cellSize3);
            }
            
            // 이하 스폰 위치 그리기 그대로 유지
            List<FieldSpawnInfo> fieldInfo = battleSpawnerData.fieldSpawnInfos;
            foreach (FieldSpawnInfo tp in fieldInfo)
            {
                if (tp.SpawnType == eCharType.Player)
                    Gizmos.color = new Color(1, 0.5f, 0);
                else if (tp.SpawnType == eCharType.Enemy)
                    Gizmos.color = new Color(0, 0, 1);

                foreach (var info in tp.UnitSpawnList)
                    Gizmos.DrawSphere(transform.TransformPoint(info.SpawnPosition), 1f);
            }
        }

        [ContextMenu("좌표 셀 유효성 검사(플랫폼 및 장애물)")]
        private void ValidateGridCells()
        {
            bool ok = true;
            ok &= ValidateList(platformGridCells, "platformGridCells");
            //ok &= ValidateList(obstacleGridCells, "obstacleGridCells");
            if (ok) Debug.Log("[StageField] Grid cell lists are valid.");
        }
        
        bool ValidateList(List<Vector2Int> list, string listName)
        {
            var set = new HashSet<Vector2Int>();
            bool ok = true;
            foreach (var c in list)
            {
                if (!(c is { x: >= 0, y: >= 0 } && c.x < gridSize.x && c.y < gridSize.y))
                {
                    Debug.LogWarning($"[StageField] {listName}: out of bounds {c} (grid {gridSize}).");
                    ok = false;
                }
                if (!set.Add(c))
                {
                    Debug.LogWarning($"[StageField] {listName}: duplicated cell {c}.");
                    ok = false;
                }
            }
            return ok;
        }
        
    #endif
        #endregion


    }





}
