using Core.Scripts.Foundation.Define;
using Core.Scripts.Foundation.SceneUtil;
using Core.Scripts.GameSave;
using Core.Scripts.Managers;
using GamePlay.Common.Scripts.NewGameUtil;
using GamePlay.Common.Scripts.Scene;
using GamePlay.Features.Battle.Scripts;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Features.Lobby.Scripts
{
    public class LobbyManager : MonoBehaviour
    {
        private static LobbyManager instance;
        public static LobbyManager Instance
        {
            get
            {
                instance = FindObjectOfType<LobbyManager>();
                if(!instance)
                    instance = new GameObject("LobbyManager").AddComponent<LobbyManager>();

                return instance;
            }
        }
        
        [SerializeField] private string defaultSaveName = "새 여정 시작";
        
        [Header("Debug 용도")]
        [SerializeField] private bool autoLoadLastSlot = false;
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // SaveLoadManager 초기화 확인
            if (SaveLoadManager.Instance == null)
            {
                Debug.LogError("[LobbyManager] SaveLoadManager not initialized!");
                return;
            }

            // 디버그 모드: 마지막 플레이한 슬롯 자동 로드
            if (autoLoadLastSlot && SaveLoadManager.Instance.HasLastPlayed)
            {
                //var lastSlotIndex = SaveLoadManager.Instance.GlobalSave.LastPlayedSlotIndex;
                Debug.Log($"[LobbyManager] Auto-loading...");
                ContinueGame();
            }
        }
        
        /// <summary>
        /// 저장된 슬롯들을 뷰로 보여줍니다.
        /// </summary>
        private void ShowSlots()
        {
            List<SlotMetaData> list = SaveLoadManager.Instance.GetAllSlots();
            for (int i = 0; i < list.Count; i++)
            {
                var meta = list[i];
                Debug.Log($"[Slot {i}] {(meta.isEmpty ? "(빈 슬롯)" : meta.slotName)}");
            }
        }
        
        public void NewGameStart()
        {
            NewGameStart(defaultSaveName);
        }
        
        public async void NewGameStart(string slotName)
        {
            if (string.IsNullOrWhiteSpace(slotName))
            {
                Debug.LogWarning("[LobbyManager] Slot name is empty!");
                // TODO: UI에 오류 표시
                return;
            }

            // 슬롯 생성
            bool success = SaveLoadManager.Instance.CreateNewSlot(slotName, out int slotIndex);
            
            if (!success)
            {
                Debug.LogError($"[LobbyManager] Failed to create new slot: {slotName}");
                // TODO: UI에 오류 표시 (슬롯 가득 참 등)
                return;
            }

            Debug.Log($"[LobbyManager] New slot created: {slotName} (Index: {slotIndex})");
            
            
            // 게임 시작 씬 - 튜토리얼로 전환
            //GamePlaySceneUtil.LoadBattleScene();
            await NewGameStartUtil.StartNewGame();
            //SceneLoader.LoadSceneWithLoading(SystemEnum.eScene.BattleTestScene, BattleSceneInitializer.InitializeAsync);
        }
        
        /// <summary>
        /// 새 슬롯 생성 가능 여부 확인
        /// </summary>
        public bool CanCreateNewSlot()
        {
            var globalSave = SaveLoadManager.Instance.GlobalSave;
            return globalSave.HasEmptySlot;
        }

        /// <summary>
        /// 사용 가능한 슬롯 수 조회
        /// </summary>
        public int GetAvailableSlotCount()
        {
            var globalSave = SaveLoadManager.Instance.GlobalSave;
            return globalSave.maxSlotCount - globalSave.PlayableSlotCount;
        }

        public void ContinueGame()
        {
            bool success = SaveLoadManager.Instance.LoadSlot();

            if (!success)
            {
                Debug.LogError($"[LobbyManager] Failed to load slot!");
                // TODO: UI에 오류 표시
                return;
            }

            var currentSlot = SaveLoadManager.Instance.CurrentSlot;
            Debug.Log($"[LobbyManager] Game loaded: {currentSlot.slotName}");
            Debug.Log($"[LobbyManager] Last state: {currentSlot.lastGameState}");

            // 마지막 게임 상태에 따라 적절한 씬으로 전환
            LoadSceneByGameState(currentSlot.lastGameState);
        }
        
        public bool HasSavedGame()
        {
            return SaveLoadManager.Instance.HasLastPlayed;
        }

        /// <summary>
        /// 저장된 게임 정보 가져오기
        /// </summary>
        public SlotMetaData GetSavedGameInfo()
        {
            return SaveLoadManager.Instance.GlobalSave.LastPlayedSlotData;
        }

        /// <summary>
        /// 저장된 게임 삭제
        /// </summary>
        public void DeleteSavedGame()
        {
            bool success = SaveLoadManager.Instance.DeleteSlot();
            
            if (success)
            {
                Debug.Log($"[LobbyManager] Saved game deleted successfully.");
                // TODO: UI 갱신
            }
            else
            {
                Debug.LogError($"[LobbyManager] Failed to delete saved game");
            }
        }

        
       //public void QuickContinue()
       //{
       //    if (!SaveLoadManager.Instance.HasLastPlayed)
       //    {
       //        Debug.LogWarning("[LobbyManager] No last played slot found!");
       //        return;
       //    }

       //    var lastSlotIndex = SaveLoadManager.Instance.GlobalSave.LastPlayedSlotIndex;
       //    ContinueGame(lastSlotIndex);
       //}
        
        #region 슬롯 관리
        
        /// <summary>
        /// 모든 슬롯 메타데이터 조회 (UI 표시용)
        /// </summary>
        public List<SlotMetaData> GetAllSlots()
        {
            return SaveLoadManager.Instance.GetAllSlots();
        }

        /// <summary>
        /// 슬롯 삭제
        /// </summary>
        public void DeleteSlot(int slotIndex)
        {
            bool success = SaveLoadManager.Instance.DeleteSlot(slotIndex);
            
            if (success)
            {
                Debug.Log($"[LobbyManager] Slot {slotIndex} deleted successfully.");
                // TODO: UI 갱신
            }
            else
            {
                Debug.LogError($"[LobbyManager] Failed to delete slot {slotIndex}");
            }
        }

        /// <summary>
        /// 특정 슬롯이 비어있는지 확인
        /// </summary>
        public bool IsSlotEmpty(int slotIndex)
        {
            var slots = GetAllSlots();
            if (slotIndex < 0 || slotIndex >= slots.Count)
                return true;
            
            return slots[slotIndex].isEmpty;
        }
        
        #endregion

        
        #region 씬 전환
        
        /// <summary>
        /// 게임 상태에 따라 적절한 씬으로 전환
        /// </summary>
        private void LoadSceneByGameState(SystemEnum.GameState gameState)
        {
            switch (gameState)
            {
                case SystemEnum.GameState.Village:
                    SceneLoader.LoadSceneWithLoading(SystemEnum.eScene.VillageScene);
                    break;
                
                case SystemEnum.GameState.Explore:
                    Debug.LogError(
                        "[LobbyManager] Explore continue is not available yet: the saved slot does not contain party reconstruction data.");
                    break;
                
                case SystemEnum.GameState.Battle:
                    Debug.LogError(
                        "[LobbyManager] Battle continue is not available yet: the saved slot does not contain complete battle reconstruction data.");
                    break;
                
                case SystemEnum.GameState.Lobby:
                    SceneLoader.LoadSceneWithLoading(SystemEnum.eScene.LobbyScene);
                    break;
                
                default:
                    SceneLoader.LoadSceneWithLoading(SystemEnum.eScene.LobbyScene);
                    break;
            }
        }
        #endregion
        
        /// <summary>
        /// 슬롯 정보 출력 (디버깅용)
        /// </summary>
        [ContextMenu("Print Slot Info")]
        public void PrintSlotInfo()
        {
            if (!SaveLoadManager.Instance.HasLastPlayed)
            {
                Debug.Log("=== No Saved Game ===");
                return;
            }

            var slot = SaveLoadManager.Instance.GlobalSave.LastPlayedSlotData;
            Debug.Log("=== Saved Game Info ===");
            Debug.Log($"Slot Name: {slot.slotName}");
            Debug.Log($"Last State: {slot.lastGameState}");
            Debug.Log($"Play Time: {slot.PlayTime:hh\\:mm\\:ss}");
            Debug.Log($"Last Saved: {slot.lastSavedTime:yyyy-MM-dd HH:mm:ss}");
        }

        /// <summary>
        /// 글로벌 세이브 데이터 정보 출력
        /// </summary>
        [ContextMenu("Print Global Save Info")]
        public void PrintGlobalSaveInfo()
        {
            SaveLoadManager.Instance.GlobalSave.PrintDebugInfo();
        }

        /// <summary>
        /// 현재 슬롯의 RNG 카운터 정보 출력
        /// </summary>
        [ContextMenu("Print RNG Counters")]
        public void PrintRngCounters()
        {
            if (!SaveLoadManager.Instance.HasCurrentSlot)
            {
                Debug.LogWarning("[LobbyManager] No current slot loaded!");
                return;
            }

            var currentSlot = SaveLoadManager.Instance.CurrentSlot;
            Debug.Log($"=== RNG Counters: {currentSlot.slotName} ===");
            Debug.Log($"Slot Seed: {currentSlot.slotSeed}");
            
            if (currentSlot.RngCounters != null && currentSlot.RngCounters.Count > 0)
            {
                Debug.Log("Counters:");
                foreach (var kvp in currentSlot.RngCounters)
                {
                    Debug.Log($"  {kvp.Key}: {kvp.Value}");
                }
            }
            else
            {
                Debug.Log("(No counters yet)");
            }
        }
        
    }
}
