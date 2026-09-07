using Core.Scripts.GameSave;
using Core.Scripts.Managers;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GamePlay.Features.Lobby.Scripts.Test
{
    public class SaveIntegrationTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private string testSlotName = "TestSlot";
        
        [Header("Editor Settings")]
        [SerializeField] private bool initializeInEditor = true;
        
        #region 초기화 관련
        
        /// <summary>
        /// SaveLoadManager가 초기화되었는지 확인하고, 필요시 초기화
        /// </summary>
        private bool EnsureSaveLoadManagerInitialized()
        {
            try
            {
                // SaveLoadManager 인스턴스 접근 시도
                var slm = SaveLoadManager.Instance;
                
                if (slm == null)
                {
                    Debug.LogError("❌ SaveLoadManager.Instance is null!");
                    return false;
                }
                
                // GlobalSave가 초기화되었는지 확인
                if (slm.GlobalSave == null)
                {
                    Debug.LogWarning("⚠️ SaveLoadManager not fully initialized. Initializing...");
                    
                    #if UNITY_EDITOR
                    if (!Application.isPlaying && initializeInEditor)
                    {
                        // 에디터 모드에서 강제 초기화
                        ForceInitializeForEditor();
                        return true;
                    }
                    #endif
                    
                    if (Application.isPlaying)
                    {
                        // 플레이 모드에서는 Init() 호출
                        slm.Init();
                        return true;
                    }
                    
                    Debug.LogError("❌ Cannot initialize SaveLoadManager in current context!");
                    return false;
                }
                
                Debug.Log("✅ SaveLoadManager is properly initialized");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error accessing SaveLoadManager: {e.Message}");
                return false;
            }
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// 에디터 모드에서 SaveLoadManager를 강제 초기화
        /// 주의: 이는 테스트 전용이며, 실제 게임에서는 사용하지 않아야 함
        /// </summary>
        private void ForceInitializeForEditor()
        {
            try
            {
                Debug.LogWarning("⚠️ Force initializing SaveLoadManager for editor testing...");
                
                // 임시 디렉토리 설정
                string tempRoot = System.IO.Path.Combine(Application.temporaryCachePath, "editor_test_userdata");
                Core.Scripts.GameSave.IO.SlotIO.InitUserRoot(tempRoot);
                
                // SaveLoadManager 강제 초기화
                var slm = SaveLoadManager.Instance;
                slm.Init();
                
                Debug.Log("✅ SaveLoadManager force-initialized for editor testing");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Failed to force-initialize SaveLoadManager: {e.Message}");
            }
        }
        #endif
        
        #endregion
        
        #region 기본 테스트들
        
        [ContextMenu("1. Test New Game Flow")]
        public void TestNewGameFlow()
        {
            Debug.Log("=== Testing New Game Flow ===");
            
            if (!EnsureSaveLoadManagerInitialized())
            {
                Debug.LogError("❌ SaveLoadManager not available. Test aborted.");
                return;
            }
            
            // 1. 새 슬롯 생성
            bool success = SaveLoadManager.Instance.CreateNewSlot(testSlotName, out int slotIndex);
            
            if (!success)
            {
                Debug.LogError("Failed to create new slot!");
                return;
            }
            
            Debug.Log($"✅ Slot created: Index {slotIndex}");
            
            // 2. 슬롯 데이터 확인
            var slot = SaveLoadManager.Instance.CurrentSlot;
            Debug.Log($"Slot Name: {slot.slotName}");
            Debug.Log($"Slot Seed: {slot.slotSeed}");
            Debug.Log($"RNG Instance: {slot.RNG != null}");
            Debug.Log($"RngCounters Initialized: {slot.RngCounters != null}");
            
            // 3. RNG 동작 테스트
            TestRngOperations(slot);
        }
        
        [ContextMenu("2. Test Continue Game Flow")]
        public void TestContinueGameFlow()
        {
            Debug.Log("=== Testing Continue Game Flow ===");
            
            if (!EnsureSaveLoadManagerInitialized())
            {
                Debug.LogError("❌ SaveLoadManager not available. Test aborted.");
                return;
            }
            
            // 1. 마지막 플레이한 슬롯 확인
            if (!SaveLoadManager.Instance.HasLastPlayed)
            {
                Debug.LogWarning("⚠️ No last played slot. Create one first!");
                return;
            }
            
            var lastSlotIndex = SaveLoadManager.Instance.GlobalSave.LastPlayedSlotIndex;
            Debug.Log($"Last played slot index: {lastSlotIndex}");
            
            // 2. 슬롯 로드
            bool success = SaveLoadManager.Instance.LoadSlot(lastSlotIndex);
            
            if (!success)
            {
                Debug.LogError("❌ Failed to load slot!");
                return;
            }
            
            Debug.Log("✅ Slot loaded successfully");
            
            // 3. 로드된 슬롯 데이터 확인
            var slot = SaveLoadManager.Instance.CurrentSlot;
            Debug.Log($"✅ Slot Name: {slot.slotName}");
            Debug.Log($"✅ Slot Seed: {slot.slotSeed}");
            Debug.Log($"✅ RNG Instance: {slot.RNG != null}");
            Debug.Log($"✅ Play Time: {slot.PlayTime:hh\\:mm\\:ss}");
            Debug.Log($"✅ Last Game State: {slot.lastGameState}");
            
            // 4. RNG 카운터 확인
            if (slot.RngCounters != null && slot.RngCounters.Count > 0)
            {
                Debug.Log($"✅ RNG Counters ({slot.RngCounters.Count}):");
                foreach (var kvp in slot.RngCounters)
                {
                    Debug.Log($"   {kvp.Key}: {kvp.Value}");
                }
            }
            else
            {
                Debug.Log("✅ No RNG counters yet (fresh slot)");
            }
        }
        
        #endregion
        
        #region RNG 동작 테스트
        
        private void TestRngOperations(GameSlotData slot)
        {
            Debug.Log("=== Testing RNG Operations ===");
            
            // 1. 기본 랜덤 생성
            int randomInt = slot.RNG.NextInt("TestCategory1", 1, 100);
            Debug.Log($"✅ Random Int (1-100): {randomInt}");
            
            float randomFloat = slot.RNG.NextFloat01("TestCategory2");
            Debug.Log($"✅ Random Float (0-1): {randomFloat}");
            
            bool chance = slot.RNG.Chance("TestCategory3", 50f);
            Debug.Log($"✅ 50% Chance: {chance}");
            
            // 2. 가중치 선택 테스트
            float[] weights = { 10f, 30f, 40f, 20f };
            int weightedChoice = slot.RNG.WeightedChoice("TestCategory4", weights);
            Debug.Log($"✅ Weighted Choice (weights: [10,30,40,20]): {weightedChoice}");
            
            // 3. 카운터 확인
            Debug.Log($"✅ Total categories used: {slot.RngCounters.Count}");
            foreach (var kvp in slot.RngCounters)
            {
                Debug.Log($"   {kvp.Key}: {kvp.Value}");
            }
        }
        
        [ContextMenu("3. Test RNG Determinism")]
        public void TestRngDeterminism()
        {
            Debug.Log("=== Testing RNG Determinism ===");
            
            // 동일한 시드로 두 개의 GameRandom 생성
            ulong testSeed = 12345678901234567890UL;
            
            var rng1 = new Core.Scripts.Foundation.Utils.GameRandom(testSeed);
            var rng2 = new Core.Scripts.Foundation.Utils.GameRandom(testSeed);
            
            // 동일한 랜덤 값이 나오는지 확인
            for (int i = 0; i < 10; i++)
            {
                int val1 = rng1.Next(1, 100);
                int val2 = rng2.Next(1, 100);
                
                if (val1 == val2)
                {
                    Debug.Log($"✅ Iteration {i}: {val1} == {val2}");
                }
                else
                {
                    Debug.LogError($"❌ Iteration {i}: {val1} != {val2} (Determinism broken!)");
                    return;
                }
            }
            
            Debug.Log("✅ All iterations matched! Determinism verified.");
        }
        
        #endregion
        
        #region 저장/로드 주기 테스트
        
        [ContextMenu("4. Test Save-Load Cycle")]
        public void TestSaveLoadCycle()
        {
            Debug.Log("=== Testing Save-Load Cycle ===");
            
            if (!EnsureSaveLoadManagerInitialized())
            {
                Debug.LogError("❌ SaveLoadManager not available. Test aborted.");
                return;
            }
            
            // 1. 새 슬롯 생성
            string uniqueName = $"TestSlot_{System.DateTime.Now.Ticks}";
            SaveLoadManager.Instance.CreateNewSlot(uniqueName, out int slotIndex);
            
            var originalSlot = SaveLoadManager.Instance.CurrentSlot;
            Debug.Log($"✅ Created slot: {uniqueName}, Index: {slotIndex}");
            
            // 2. RNG 사용 (카운터 증가)
            originalSlot.RNG.NextInt("TestCategory_A", 1, 100);
            originalSlot.RNG.NextInt("TestCategory_A", 1, 100);
            originalSlot.RNG.NextFloat01("TestCategory_B");
            
            ulong counterA = originalSlot.RngCounters.GetValueOrDefault<string, ulong>("TestCategory_A", 0);
            ulong counterB = originalSlot.RngCounters.GetValueOrDefault<string, ulong>("TestCategory_B", 0);
            
            Debug.Log($"✅ Original Counter A: {counterA}");
            Debug.Log($"✅ Original Counter B: {counterB}");
            
            // 3. 저장
            SaveLoadManager.Instance.SaveSlotByCurrentState();
            Debug.Log("✅ Slot saved");
            
            // 4. 다른 슬롯 로드 (캐시 초기화)
            SaveLoadManager.Instance.CreateNewSlot("DummySlot", out int dummyIndex);
            Debug.Log("✅ Loaded dummy slot to clear cache");
            
            // 5. 원래 슬롯 다시 로드
            bool loadSuccess = SaveLoadManager.Instance.LoadSlot(slotIndex);
            
            if (!loadSuccess)
            {
                Debug.LogError("❌ Failed to reload slot!");
                return;
            }
            
            var reloadedSlot = SaveLoadManager.Instance.CurrentSlot;
            Debug.Log($"✅ Reloaded slot: {reloadedSlot.slotName}");
            
            // 6. 카운터 복원 확인
            ulong reloadedCounterA = reloadedSlot.RngCounters.GetValueOrDefault<string, ulong>("TestCategory_A", 0);
            ulong reloadedCounterB = reloadedSlot.RngCounters.GetValueOrDefault<string, ulong>("TestCategory_B", 0);
            
            Debug.Log($"✅ Reloaded Counter A: {reloadedCounterA}");
            Debug.Log($"✅ Reloaded Counter B: {reloadedCounterB}");
            
            // 7. 검증
            if (counterA == reloadedCounterA && counterB == reloadedCounterB)
            {
                Debug.Log("✅ Counters preserved correctly!");
            }
            else
            {
                Debug.LogError($"❌ Counters mismatch! A: {counterA} vs {reloadedCounterA}, B: {counterB} vs {reloadedCounterB}");
            }
            
            // 8. 정리
            SaveLoadManager.Instance.DeleteSlot(slotIndex);
            SaveLoadManager.Instance.DeleteSlot(dummyIndex);
            Debug.Log("✅ Test slots cleaned up");
        }
        
        #endregion
        
        #region LobbyManager 테스트
        
        [ContextMenu("5. Test LobbyManager New Game")]
        public void TestLobbyManagerNewGame()
        {
            Debug.Log("=== Testing LobbyManager New Game ===");
            
            if (!EnsureSaveLoadManagerInitialized())
            {
                Debug.LogError("❌ SaveLoadManager not available. Test aborted.");
                return;
            }
            
            // 슬롯 생성 가능 여부 확인
            bool canCreate = LobbyManager.Instance.CanCreateNewSlot();
            Debug.Log($"Can create new slot: {canCreate}");
            
            if (!canCreate)
            {
                Debug.LogWarning("⚠️ Cannot create new slot (slots full)");
                return;
            }
            
            // 새 게임 시작 (씬 전환 제외)
            string testName = $"LobbyTest_{System.DateTime.Now.Ticks}";
            
            // 실제로는 LobbyManager.Instance.StartNewGame(testName);
            // 여기서는 씬 전환을 막기 위해 직접 슬롯만 생성
            SaveLoadManager.Instance.CreateNewSlot(testName, out int slotIndex);
            
            Debug.Log($"✅ Slot created via lobby flow: {testName}, Index: {slotIndex}");
            
            // 슬롯 정보 확인
            var slots = LobbyManager.Instance.GetAllSlots();
            Debug.Log($"✅ Total slots: {slots.Count}");
            
            foreach (var slot in slots)
            {
                if (!slot.isEmpty)
                {
                    Debug.Log($"   - {slot.slotName} (Last: {slot.lastGameState})");
                }
            }
        }
        
        [ContextMenu("6. Test LobbyManager Continue Game")]
        public void TestLobbyManagerContinueGame()
        {
            Debug.Log("=== Testing LobbyManager Continue Game ===");
            
            if (!EnsureSaveLoadManagerInitialized())
            {
                Debug.LogError("❌ SaveLoadManager not available. Test aborted.");
                return;
            }
            
            // 마지막 플레이한 슬롯 확인
            if (!SaveLoadManager.Instance.HasLastPlayed)
            {
                Debug.LogWarning("⚠️ No last played slot!");
                return;
            }
            
            var lastIndex = SaveLoadManager.Instance.GlobalSave.LastPlayedSlotIndex;
            Debug.Log($"Last played slot index: {lastIndex}");
            
            // 이어하기 (씬 전환 제외)
            SaveLoadManager.Instance.LoadSlot(lastIndex);
            
            var slot = SaveLoadManager.Instance.CurrentSlot;
            Debug.Log($"✅ Continued: {slot.slotName}");
            Debug.Log($"✅ Last State: {slot.lastGameState}");
            Debug.Log($"✅ Play Time: {slot.PlayTime:hh\\:mm\\:ss}");
        }
        
        #endregion
        
        #region 통합 시나리오 테스트
        
        [ContextMenu("7. Full Integration Test")]
        public void FullIntegrationTest()
        {
            Debug.Log("=== FULL INTEGRATION TEST ===");
            
            if (!EnsureSaveLoadManagerInitialized())
            {
                Debug.LogError("❌ SaveLoadManager not available. Test aborted.");
                return;
            }
            
            // 시나리오: 플레이어가 새 게임 → 탐사 → 저장 → 종료 → 재실행 → 이어하기
            
            // 1. 새 게임 시작
            Debug.Log("\n[Step 1] Starting new game...");
            string playerName = $"IntegrationTest_{System.DateTime.Now.Ticks}";
            SaveLoadManager.Instance.CreateNewSlot(playerName, out int slotIndex);
            var slot = SaveLoadManager.Instance.CurrentSlot;
            Debug.Log($"✅ New game started: {playerName}");
            
            // 2. 탐사 맵 시드 생성 시뮬레이션
            Debug.Log("\n[Step 2] Entering dungeon...");
            string exploreKey = "Explore_MOUNTAIN_BACK_1";
            slot.RNG.NextInt(exploreKey, 1, 1000000); // 맵 시드 대용
            Debug.Log($"✅ Map seed generated using category: {exploreKey}");
            Debug.Log($"✅ Explore counter: {slot.RngCounters.GetValueOrDefault<string, ulong>(exploreKey, 0)}");
            
            // 3. 게임 진행 시뮬레이션
            Debug.Log("\n[Step 3] Playing game...");
            slot.RNG.NextInt("Combat_Enemy1", 10, 50); // 전투 시뮬레이션
            slot.RNG.Chance("Loot_Rare", 25f); // 루트 시뮬레이션
            Debug.Log($"✅ Game progress simulated");
            
            // 4. 저장
            Debug.Log("\n[Step 4] Saving game...");
            SaveLoadManager.Instance.SaveSlotByCurrentState();
            Debug.Log($"✅ Game saved");
            
            // 5. 종료 시뮬레이션 (캐시 클리어)
            Debug.Log("\n[Step 5] Simulating game exit...");
            SaveLoadManager.Instance.CreateNewSlot("TempSlot", out int tempIndex);
            Debug.Log($"✅ Cache cleared");
            
            // 6. 재실행 및 이어하기
            Debug.Log("\n[Step 6] Restarting and continuing...");
            SaveLoadManager.Instance.LoadSlot(slotIndex);
            var reloadedSlot = SaveLoadManager.Instance.CurrentSlot;
            Debug.Log($"✅ Game continued: {reloadedSlot.slotName}");
            
            // 7. 데이터 무결성 검증
            Debug.Log("\n[Step 7] Verifying data integrity...");
            ulong reloadedCounter = reloadedSlot.RngCounters.GetValueOrDefault<string, ulong>(exploreKey, 0);
            Debug.Log($"✅ Explore counter after reload: {reloadedCounter}");
            
            if (reloadedCounter == 1) // NextInt 한 번 호출했으므로
            {
                Debug.Log("✅ Counter preserved correctly!");
            }
            else
            {
                Debug.LogError($"❌ Counter mismatch! Expected 1, got {reloadedCounter}");
            }
            
            // 8. 정리
            Debug.Log("\n[Step 8] Cleaning up...");
            SaveLoadManager.Instance.DeleteSlot(slotIndex);
            SaveLoadManager.Instance.DeleteSlot(tempIndex);
            Debug.Log($"✅ Test slots deleted");
            
            Debug.Log("\n=== INTEGRATION TEST COMPLETE ===");
        }
        
        #endregion
        
        #region 추가 유틸리티
        
        [ContextMenu("8. Clean All Test Slots")]
        public void CleanAllTestSlots()
        {
            Debug.Log("=== Cleaning Test Slots ===");
            
            if (!EnsureSaveLoadManagerInitialized())
            {
                Debug.LogError("❌ SaveLoadManager not available. Cannot clean.");
                return;
            }
            
            var slots = SaveLoadManager.Instance.GetAllSlots();
            int cleaned = 0;
            
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                var slot = slots[i];
                if (!slot.isEmpty && (slot.slotName.Contains("Test") || slot.slotName.Contains("Integration")))
                {
                    SaveLoadManager.Instance.DeleteSlot(i);
                    Debug.Log($"✅ Deleted test slot: {slot.slotName}");
                    cleaned++;
                }
            }
            
            Debug.Log($"✅ Cleaned {cleaned} test slots");
        }
        
        [ContextMenu("9. Print System Info")]
        public void PrintSystemInfo()
        {
            Debug.Log("=== System Information ===");
            Debug.Log($"Application.isPlaying: {Application.isPlaying}");
            Debug.Log($"Application.persistentDataPath: {Application.persistentDataPath}");
            Debug.Log($"Application.temporaryCachePath: {Application.temporaryCachePath}");
            
            #if UNITY_EDITOR
            Debug.Log($"UNITY_EDITOR: true");
            #endif
            
            if (EnsureSaveLoadManagerInitialized())
            {
                Debug.Log($"SaveLoadManager.Instance: ✅ Available");
                Debug.Log($"GlobalSave: ✅ Available");
                Debug.Log($"Current working directory: {System.IO.Directory.GetCurrentDirectory()}");
            }
            else
            {
                Debug.Log($"SaveLoadManager.Instance: ❌ Not available");
            }
        }
        
        #endregion
    }
}