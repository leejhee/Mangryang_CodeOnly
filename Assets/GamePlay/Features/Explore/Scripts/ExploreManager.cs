using Core.Scripts.Foundation.Define;
using Core.Scripts.GameSave;
using Core.Scripts.GameSave.Contracts;
using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Entities.Character;
using GamePlay.Features.Explore.Scripts.Map.Logic;
using GamePlay.Features.Explore.Scripts.Models;
using System;
using System.Linq;
using UIs.Runtime;
using UnityEngine;

namespace GamePlay.Features.Explore.Scripts
{
    /// <summary>
    /// 탐사 관리
    /// </summary>
    public class ExploreManager : MonoBehaviour, IFeatureSaveProvider
    {
        #region Singleton
        private static ExploreManager instance;
        public static ExploreManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        
        #endregion

        [SerializeField] private GameObject tutorialMap;
        [SerializeField] private ExploreController controller;
        [Header("Map References")] 
        public ExploreMap exploreMap;
        
        [Header("Current Exploration State")]
        public SystemEnum.Dungeon currentDungeon;
        public int currentFloor;
        public Party playerParty;
        public Vector2Int playerPosition;
        public ulong currentMapSeed;
        
        public event Action<Vector2Int> OnPlayerMoved;
        public event Action<SystemEnum.Dungeon, int> OnExplorationStarted;
        public event Action OnExplorationCompleted;
        
        private ExploreSnapshot _currentSnapshot;
        private bool _isInitialized = false;
        
        #region Unity Events
        
        private void OnEnable()
        {
            SaveLoadManager.Instance.RegisterProvider(this);
            SaveLoadManager.Instance.SlotLoaded += OnSlotLoaded;
        }

        private void OnDisable()
        {
            if (SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.SlotLoaded -= OnSlotLoaded;
                SaveLoadManager.Instance.UnregisterProvider(this);
            }
        }
        #endregion
        
        #region Initialization

        public async UniTask ExploreInitialize()
        {
            // 두 번 호출 방지
            if (_isInitialized) return;
            try
            {
                ExploreSession session = ExploreSession.Instance;
                currentDungeon = session.TargetDungeon;
                currentFloor = session.TargetFloor;
                playerParty = session.PlayerParty;
                
                if (currentDungeon == SystemEnum.Dungeon.TUTORIAL)
                {
                    Instantiate(tutorialMap, exploreMap.transform);
                    GameObject startPoint = GameObject.Find("StartPoint");
                    Instantiate(controller, startPoint.transform.position, startPoint.transform.rotation);
                }
                else
                {
                    await HandlePayloadExploration(session);
                    Vector3 spawnPos;
                    if (session.IsNewExplore)
                    {
                        spawnPos = exploreMap.CellToWorld(playerPosition);
                    }
                    else
                    {
                        // 배틀에서 돌아오는 경우 → 떠나기 직전 월드 좌표
                        spawnPos = session.PlayerRecentPosition;
                        if (spawnPos == Vector3.zero)
                        {
                            spawnPos = exploreMap.CellToWorld(playerPosition);
                        }
                    }
                    Instantiate(controller, spawnPos, Quaternion.identity);
                }

                
                _isInitialized = true;
                Debug.Log($"[ExploreManager] Exploration initialized: {currentDungeon} Floor {currentFloor}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExploreManager] Failed to initialize exploration: {ex}");
                throw;
            }
        }
        
        /// <summary>
        /// Payload 기반 탐사 처리
        /// </summary>
        private async UniTask HandlePayloadExploration(ExploreSession session)
        {
            currentDungeon = session.TargetDungeon;
            currentFloor = session.TargetFloor;
            playerParty = session.PlayerParty;

            if (session.IsNewExplore)
            {
                // 새 탐사 시작
                await StartNewExploration();
            }
            else
            {
                // 기존 탐사 이어하기
                await ContinueExistingExploration();
            }
        }

        /// <summary>
        /// 저장된 탐사 상태 복원
        /// </summary>
        private async UniTask HandleSavedExploration()
        {
            if (SaveLoadManager.Instance.HasCurrentSlot &&
                SaveLoadManager.Instance.CurrentSlot.TryGet("Explore", out ExploreSnapshot snapshot))
            {
                await RestoreFromSnapshot(snapshot);
            }
            else
            {
                Debug.LogWarning("[ExploreManager] No saved exploration found. Starting default exploration.");
                // 기본값으로 시작 (개발/테스트용)
                currentDungeon = SystemEnum.Dungeon.MOUNTAIN_BACK;
                currentFloor = 1;
                await StartNewExploration();
            }
        }

        /// <summary>
        /// 새 탐사 시작
        /// </summary>
        private async UniTask StartNewExploration()
        {
            if (SaveLoadManager.Instance.HasCurrentSlot)
            {
                var rng = SaveLoadManager.Instance.CurrentSlot.RNG;
                if (rng == null)
                    throw new InvalidOperationException("The loaded slot has no RNG state and cannot create a new exploration map.");
                currentMapSeed = rng.DeriveAndIncrementSeed(BuildMapSeedCategory(currentDungeon, currentFloor));
            }
            else
            {
                currentMapSeed = Core.Scripts.Foundation.Utils.RandomUtil.Mix3(
                    unchecked((ulong)DateTime.UtcNow.Ticks),
                    unchecked((ulong)Environment.TickCount),
                    (ulong)currentDungeon ^ unchecked((ulong)currentFloor));
                if (currentMapSeed == 0UL) currentMapSeed = 1UL;
            }

            await exploreMap.GenerateMap(
                currentMapSeed,
                currentFloor,
                currentDungeon,
                ExploreMapGenerator.CurrentVersion);
           
            
            playerPosition = FindStartPosition();

            Vector2Int mapSize = exploreMap.GetMapSize();
            _currentSnapshot = new ExploreSnapshot();
            _currentSnapshot.StartNewExploration(
                currentDungeon, 
                currentFloor, 
                currentMapSeed, 
                playerPosition,
                mapSize.x * mapSize.y,
                exploreMap.CurrentSkeleton.GenerationVersion,
                exploreMap.CurrentSkeleton.ConfigSignature
            );
            
            int startCellIndex = exploreMap.GetCellIndex(playerPosition);
            _currentSnapshot.UpdatePlayerPosition(playerPosition, startCellIndex);

            OnExplorationStarted?.Invoke(currentDungeon, currentFloor);
            Debug.Log($"[ExploreManager] New exploration started at {currentDungeon} Floor {currentFloor}, Seed: {currentMapSeed}");
            ExploreSession.Instance.SetCurrentSkeleton(exploreMap.CurrentSkeleton, exploreMap.CurrentSeed);
        }

        /// <summary>
        /// 기존 탐사 이어하기
        /// </summary>
        private async UniTask ContinueExistingExploration()
        {
            ExploreSession session = ExploreSession.Instance;

            // 이번 런에서 이미 스켈레톤을 가진 경우 
            if (session.CurrentSkeleton != null)
            {
                currentDungeon = session.TargetDungeon;
                currentFloor   = session.TargetFloor;
                currentMapSeed = session.RandomSeed;

                // 같은 seed로 맵 재생성 (씬이 바뀌었으니 오브젝트는 다시 찍어야 함)
                await exploreMap.GenerateMap(
                    currentMapSeed,
                    currentFloor,
                    currentDungeon,
                    session.CurrentSkeleton.GenerationVersion,
                    session.CurrentSkeleton.ConfigSignature);

                // 혹시 GenerateMap 안에서 새로운 스켈레톤이 만들어지면 다시 세션에 싱크
                session.SetCurrentSkeleton(exploreMap.CurrentSkeleton, currentMapSeed);

                Debug.Log("[ExploreManager] Continue exploration from session (battle return).");
                return;
            }
            
            
            if (SaveLoadManager.Instance.HasCurrentSlot &&
                SaveLoadManager.Instance.CurrentSlot.TryGet("Explore", out ExploreSnapshot snapshot))
            {
                await RestoreFromSnapshot(snapshot);
            }
            else
            {
                Debug.LogWarning("[ExploreManager] No existing exploration to continue. Starting new one.");
                await StartNewExploration();
            }
        }

        /// <summary>
        /// 스냅샷에서 탐사 상태 복원
        /// </summary>
        private async UniTask RestoreFromSnapshot(ExploreSnapshot snapshot)
        {
            _currentSnapshot = snapshot;
            currentDungeon = snapshot.currentDungeon;
            currentFloor = snapshot.currentFloor;
            currentMapSeed = snapshot.mapSeed;
            playerPosition = snapshot.playerPosition;

            // 동일한 시드로 맵 재생성
            await exploreMap.GenerateMap(
                currentMapSeed,
                currentFloor,
                currentDungeon,
                snapshot.mapGenerationVersion,
                snapshot.mapConfigSignature);
            ExploreSession.Instance.SetCurrentSkeleton(exploreMap.CurrentSkeleton, currentMapSeed);
            Debug.Log($"[ExploreManager] Exploration restored: {currentDungeon} Floor {currentFloor}");
        }

        /// <summary>
        /// 시작 위치 찾기 (StartPoint 심볼 위치)
        /// </summary>
        private Vector2Int FindStartPosition()
        {
            if (exploreMap == null || exploreMap.CurrentSkeleton == null)
                return Vector2Int.zero;

            ExploreMapSkeleton skel = exploreMap.CurrentSkeleton;
            SkeletonSymbol startSymbol = skel.GetSymbolsOfType(SystemEnum.MapSymbolType.StartPoint)
                .FirstOrDefault();

            if (startSymbol == null)
                return Vector2Int.zero;

            return new Vector2Int(startSymbol.X, startSymbol.Y);
        }

        public static string BuildMapSeedCategory(SystemEnum.Dungeon dungeon, int floor) =>
            $"Explore_{dungeon}_{floor}";

        #endregion

        #region Save/Load Events
        
        private void OnSlotLoaded(GameSlotData slotData)
        {
            if (slotData.TryGet("Explore", out ExploreSnapshot snapshot))
            {
                RebuildExploreState(snapshot);
            }
        }
        
        private void RebuildExploreState(ExploreSnapshot snapshot)
        {
            if (!_isInitialized) return;

            _currentSnapshot = snapshot;
            currentDungeon = snapshot.currentDungeon;
            currentFloor = snapshot.currentFloor;
            currentMapSeed = snapshot.mapSeed;
            playerPosition = snapshot.playerPosition;

            Debug.Log($"[ExploreManager] Exploration state rebuilt from save");
        }
        
        #endregion

        #region Player Movement
        
        /// <summary>
        /// 플레이어를 특정 위치로 이동
        /// </summary>
        public void MovePlayerTo(Vector2Int targetPosition)
        {
            if (!_isInitialized || _currentSnapshot == null) return;

            // 이동 가능 여부 검증
            if (!CanMoveTo(targetPosition))
            {
                Debug.LogWarning($"[ExploreManager] Cannot move to {targetPosition}");
                return;
            }

            // 위치 업데이트
            playerPosition = targetPosition;
            int cellIndex = exploreMap.GetCellIndex(targetPosition);
            _currentSnapshot.UpdatePlayerPosition(targetPosition, cellIndex);

            OnPlayerMoved?.Invoke(playerPosition);
            
            // 자동 저장
            SaveLoadManager.Instance.SaveSlotByCurrentState();
            
            Debug.Log($"[ExploreManager] Player moved to {targetPosition}");
        }

        /// <summary>
        /// 해당 위치로 이동 가능한지 확인
        /// </summary>
        private bool CanMoveTo(Vector2Int position)
        {
            return exploreMap.IsValidPosition(position) && exploreMap.IsFloorTile(position);
        }

        /// <summary>
        /// 타일 클릭 시 최단거리 이동
        /// </summary>
        public void OnTileClicked(Vector2Int clickedPosition)
        {
            if (!CanMoveTo(clickedPosition)) return;

            // TODO: 최단거리 경로 찾기 및 순차 이동
            // 일단은 직접 이동
            MovePlayerTo(clickedPosition);
        }

        #endregion

        #region Public Properties
        
        public bool IsExploring => _currentSnapshot?.isExploring ?? false;
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// 현재 맵에서 특정 셀이 방문되었는지 확인
        /// </summary>
        public bool IsCellVisited(Vector2Int position)
        {
            if (_currentSnapshot == null) return false;
            int cellIndex = exploreMap.GetCellIndex(position);
            return _currentSnapshot.IsCellVisited(cellIndex);
        }

        /// <summary>
        /// 탐사 완료 처리
        /// </summary>
        public void CompleteExploration()
        {
            if (_currentSnapshot != null)
            {
                _currentSnapshot.CompleteExploration();
                OnExplorationCompleted?.Invoke();
                
                // 최종 저장
                SaveLoadManager.Instance.SaveSlotByCurrentState();
                
                Debug.Log("[ExploreManager] Exploration completed!");
            }
        }
        
        #endregion
        
        #region IFeatureSaveProvider Members
        public string FeatureName => "Explore";
        
        public FeatureSnapshot Capture()
        {
            return _currentSnapshot ?? new ExploreSnapshot();
        }

        #endregion
        
        
        #region UI Events

        public CharacterModel SelectedCharacter;
        public void ShowCharacterInfoPopup(int idx)
        {
            SelectedCharacter = playerParty.partyMembers[idx];
            UIManager.Instance.ShowViewAsync(ViewID.CharacterInfoPopUpView);
        }
        
        public event Action<ExploreResourceModel> GetExploreResourceEvent;

        public void GetExploreResource(ExploreResourceModel talisman)
        {
            GetExploreResourceEvent?.Invoke(talisman);
        }

        #endregion
    }
}
