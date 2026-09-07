using Core.Scripts.Foundation.Define;
using Core.Scripts.Foundation.Singleton;
using Core.Scripts.Foundation.Utils;
using Core.Scripts.GameSave;
using Core.Scripts.GameSave.Contracts;
using Core.Scripts.GameSave.IO;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Core.Scripts.Managers
{
    public class SaveLoadManager : SingletonObject<SaveLoadManager>
    {
        #if UNITY_INCLUDE_TESTS
        public static bool suppressSlotLoadEvent = false;
        public void FlushCurrentSlotSync()
        {
            if (!HasCurrentSlot) return;
            string path = SystemString.GetSlotName(_globalSave.LastPlayedSlotIndex) + SystemString.JsonExtension;
            SlotIO.SaveAsync(path, _cachedSlotData, CancellationToken.None).GetAwaiter().GetResult();
        }
        #endif
        
        
        #region Constructor
        private SaveLoadManager() { }
        #endregion
        
        #region Fields
        private const int SINGLE_SLOT_INDEX = 0;
        private GlobalSaveData _globalSave;
        private GameSlotData _cachedSlotData;
        private bool _initialized;
        public event Action<GameSlotData> SlotLoaded;

        private readonly Dictionary<string, IFeatureSaveProvider> _providers = new();
        private readonly Dictionary<SystemEnum.GameState, List<string>> _stateToFeatures = new()
        {
            [SystemEnum.GameState.Explore] = new() {"Explore"},
            [SystemEnum.GameState.Battle] = new() {"Battle"},
            [SystemEnum.GameState.Village] = new() {"Village"},
        };
        private DateTime _lastAutoSave = DateTime.MinValue;
        private const double AutoSaveIntervalSec = 5d;
        
        
        
        #endregion
        
        #region Properties
        public GlobalSaveData GlobalSave => _globalSave;
        public GameSlotData CurrentSlot => _cachedSlotData;
        public bool IsInitialized => _initialized;
        public bool HasCurrentSlot => _cachedSlotData != null;
        public bool HasLastPlayed => _globalSave?.LastPlayedSlotData is { isEmpty: false };
        
        #endregion
        
        public override void Init()
        {
            if (_initialized) return;

            base.Init();
            string root = System.IO.Path.Combine(Application.persistentDataPath, "userdata");
            SlotIO.InitUserRoot(root);
            LoadGlobalData();
            _initialized = true;
        }
        
        #region Synchronous Save & Load
        
        #region Global Data Management
        
        #region Events
        public void OnApplicationQuit()
        {
            if (HasCurrentSlot)
            {
                try
                {
                    CaptureAllRegisteredFeatures();
                    SystemEnum.GameState state = GameManager.Instance.GameState;
                    if (state is SystemEnum.GameState.Village or
                        SystemEnum.GameState.Explore or
                        SystemEnum.GameState.Battle)
                    {
                        _cachedSlotData.lastGameState = state;
                    }
                    _cachedSlotData.lastSavedTime = DateTime.Now;

                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(150); //밀리세컨
                    string fileName = SystemString.GetSlotName(_globalSave.LastPlayedSlotIndex) + SystemString.JsonExtension;
                    SlotIO.SaveAsync(fileName, _cachedSlotData, cts.Token).GetAwaiter().GetResult();
                }
                catch{/* 일이 잘못돼도 소란피우지 맙시다*/}
                Debug.Log("강제 종료 관계로 저장.");
            }
        }
        
        public void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus) return;
            if (HasCurrentSlot)
            {
                // 앱 내릴 시 자동저장인데... 이거 맞나? 아닌거같으면 바로 없애자.
                if ((DateTime.Now - _lastAutoSave).TotalSeconds > AutoSaveIntervalSec)
                {
                    _lastAutoSave = DateTime.Now;
                    SaveSlotByState(GameManager.Instance.GameState);
                }
            }
        }
        #endregion

        private void LoadGlobalData()
        {
            _globalSave = Util.LoadSaveDataNewtonsoft<GlobalSaveData>(SystemString.GlobalSaveDataPath);
            if (_globalSave == null)
            {
                _globalSave = new GlobalSaveData();
                SaveGlobalData();
                Debug.Log("첫 설치이므로, 로비 데이터 생성합니다.");
            }
            else
            {
                Debug.Log($"Successfully loaded global save data : {_globalSave.UID}");
            }
        }

        private void SaveGlobalData()
        {
            if (_globalSave == null)
            {
                Debug.LogError("자네 지금 null을 저장하려고 했는가?");
                return;
            }
            
            Util.SaveJsonNewtonsoft(_globalSave, SystemString.GlobalSaveDataPath);
            Debug.Log("Global Data Saved!");
        }
        #endregion
        
        #region Slot Data Management
        
        // 새로 게임을 시작할 때는 세이브가 필요한가? - 편의성 상 있는게 나을거같긴 함.
        
        /// <summary>
        /// 새 슬롯을 생성
        /// </summary>
        /// <param name="slotName"> 새로 지정할 슬롯의 이름(저장 위치로 할거같긴 함) </param>
        /// <param name="slotIndex"> 새로 저장할 슬롯의 인덱스 </param>
        /// <returns> 잘 되었는가? </returns>
        public bool CreateNewSlot(string slotName, out int slotIndex)
        {
            // slotIndex = -1;
            // if (!_globalSave.HasEmptySlot)
            // {
            //     Debug.LogWarning($"[CreateNewSlot] 슬롯이 가득 찼습니다. (현재: {_globalSave.PlayableSlotCount}/{_globalSave.maxSlotCount})");
            //     return false;
            // }
            //
            // var newSlot = new GameSlotData(slotName);
            //
            // // 새 슬롯 만들었으므로 전역 데이터 저장하기
            // slotIndex = _globalSave.GetOrCreateSlot(slotName);
            //
            // _globalSave.LastPlayedSlotIndex = slotIndex;
            // SaveGlobalData();
            
            slotIndex = SINGLE_SLOT_INDEX;

            if (_globalSave == null)
            {
                Debug.LogError("[CreateNewSlot] Global save data is not initialized.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(slotName))
            {
                Debug.LogWarning("[CreateNewSlot] Slot name must not be empty.");
                return false;
            }
    
            Debug.Log($"[CreateNewSlot] Creating new game: '{slotName}'");

            // 새 GameSlotData 생성
            var newSlot = new GameSlotData(slotName);
            if (_globalSave.SlotGeneration == ulong.MaxValue)
            {
                Debug.LogError("[CreateNewSlot] Slot generation counter is exhausted.");
                return false;
            }

            ulong nextGeneration = _globalSave.SlotGeneration + 1;
            newSlot.slotSeed = RandomUtil.DeriveSlotSeed(
                _globalSave.MasterSeed,
                slotName,
                nextGeneration
            );
            newSlot.RNG = new(newSlot.slotSeed, newSlot.RngCounters);

            try
            {
                AsyncJobQueue.CancelKey("save-slot");
                string slotFileName = SystemString.GetSlotName(slotIndex) + SystemString.JsonExtension;
                SlotIO.SaveAsync(slotFileName, newSlot, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Debug.LogError($"[New Slot Save Error] {slotName} : {e}");
                return false;
            }

            // 슬롯 파일 저장 성공 후에만 메타데이터와 현재 런타임 슬롯을 확정한다.
            _globalSave.GameSlots.Clear();
            _globalSave.GameSlots.Add(new SlotMetaData(newSlot));
            _globalSave.LastPlayedSlotIndex = slotIndex;
            _globalSave.SlotGeneration = nextGeneration;
            _cachedSlotData = newSlot;
            SaveGlobalData();

            Debug.Log($"[New Slot Created]: {slotName}, Index : {slotIndex}");
            return true;
        }
        
        /// <summary>
        /// 선택된 인덱스의 슬롯을 로드한다.
        /// </summary>
        /// <param name="slotIndex">선택한 슬롯의 인덱스(UI에서 받아올 거임)</param>
        /// <returns> 로드가 잘 되었는가? </returns>
        public bool LoadSlot(int slotIndex=SINGLE_SLOT_INDEX)
        {
            // if (slotIndex < 0 || slotIndex >= _globalSave.maxSlotCount)
            // {
            //     Debug.LogError($"[로드 실패] : 유효하지 않은 슬롯 넘버 {slotIndex}");
            //     return false;
            // }
            // var slotMetaData = _globalSave.GameSlots[slotIndex];
            // if (slotMetaData.isEmpty)
            // {
            //     Debug.LogWarning($"[로드 실패] : 비어있는데요? {slotIndex}번 슬롯입니다.");
            //     return false;
            // }
            //
            // string slotFileName = $"{SystemString.SlotPrefix}{slotIndex}.json";
            // var gameSlot = SlotIO.LoadAsync(slotFileName, CancellationToken.None).GetAwaiter().GetResult();
            // if (gameSlot == null)
            // {
            //     // TODO : 메타데이터가 있는데 게임 슬롯이 없는 경우는 흔하지는 않다. 이러면 그냥 유효하지 않다 하고 튕겨버리자.
            //     Debug.LogError("[로드 실패] : 슬롯 파일이 없어 로드할 수 없습니다. 해당 슬롯을 삭제해주세요.");
            // }
            //
            // // 할당부
            // _cachedSlotData = gameSlot;
            // _globalSave.LastPlayedSlotIndex = slotIndex;
            // SaveGlobalData();
            //
            // #if UNITY_INCLUDE_TESTS
            // if(!suppressSlotLoadEvent)
            //     SlotLoaded?.Invoke(_cachedSlotData); //할당할거 다 하고 호출!
            // #else
            // SlotLoaded?.Invoke(_cachedSlotData);
            // #endif
            //
            // return true;
            
            // 단일 슬롯이므로 항상 인덱스 0 사용
            slotIndex = SINGLE_SLOT_INDEX;
    
            Debug.Log($"[LoadSlot] Loading slot {slotIndex}...");
    
            // 메타데이터 확인
            if (_globalSave.GameSlots.Count == 0)
            {
                Debug.LogWarning($"[LoadSlot] No slot metadata found.");
                return false;
            }
    
            var slotMetaData = _globalSave.GameSlots[slotIndex];
            if (slotMetaData.isEmpty)
            {
                Debug.LogWarning($"[LoadSlot] Slot {slotIndex} is empty.");
                return false;
            }
    
            // 슬롯 파일 로드
            string slotFileName = $"{SystemString.SlotPrefix}{slotIndex}.json";
            var gameSlot = SlotIO.LoadAsync(slotFileName, CancellationToken.None).GetAwaiter().GetResult();
    
            if (gameSlot == null)
            {
                Debug.LogError("[LoadSlot] Slot file not found. Please delete this slot or create a new game.");
                return false;
            }
    
            // 슬롯 데이터 할당
            _cachedSlotData = gameSlot;
            _globalSave.LastPlayedSlotIndex = slotIndex;
            SaveGlobalData();
    
            Debug.Log($"[LoadSlot] Successfully loaded: '{gameSlot.slotName}'");
    
            // 이벤트 발생
#if UNITY_INCLUDE_TESTS
            if(!suppressSlotLoadEvent)
                SlotLoaded?.Invoke(_cachedSlotData);
#else
    SlotLoaded?.Invoke(_cachedSlotData);
#endif
    
            return true;
        }
        
        /// <summary>
        /// 현재 게임에 로드되어있는 슬롯에 내용을 덮어씌워서 저장한다.
        /// </summary>
        public void SaveCurrentSlot(FeatureSnapshot snapshot)
        {
            if (!HasCurrentSlot)
            {
                Debug.LogWarning("[저장 실패] : 호출되면 안되는 로그. 지금 저장할 슬롯이 따로 없습니다?");
                return;
            }
            
            _cachedSlotData.WriteSnapshot(snapshot); // 쓰기

            string slotFileName = SystemString.GetSlotName(SINGLE_SLOT_INDEX) + SystemString.JsonExtension;
            AsyncJobQueue.EnqueueKeyed("save-slot",
                ct => SlotIO.SaveAsync(slotFileName, _cachedSlotData, ct));
            
            //if (_globalSave.LastPlayedSlotIndex >= 0 &&
            //    _globalSave.LastPlayedSlotIndex < _globalSave.GameSlots.Count)
            //{
            //    UpdateSlotMetadata(); // 연결용 메타데이터 업데이트 및 글로벌 데이터 저장
            //    SaveGlobalData();
            //}
            
            UpdateSlotMetadata();
            SaveGlobalData();
            
            Debug.Log($"Current Slot Saved : {_cachedSlotData.slotName}");
        }
        
        /// <summary>
        /// 외부 요인으로 어쩔 수 없이 저장을 해야할 때 호출하는 저장함수입니다.
        /// </summary>
        /// <param name="state">인게임 상태를 캡처해서 저장하므로, GameState에 따라 저장됩니다.</param>
        public void SaveSlotByState(SystemEnum.GameState state)
        {
            if (!HasCurrentSlot) return;

            CaptureFeaturesForState(state);
            
            _cachedSlotData.lastGameState = state;
            _cachedSlotData.lastSavedTime = DateTime.Now;

            string slotFileName = SystemString.GetSlotName(SINGLE_SLOT_INDEX) + SystemString.JsonExtension;
            AsyncJobQueue.EnqueueKeyed("save-slot",
                ct => SlotIO.SaveAsync(slotFileName, _cachedSlotData, ct));
            
            UpdateSlotMetadata();
            SaveGlobalData();
        }

        public void SaveSlotByCurrentState() => SaveSlotByState(GameManager.Instance.GameState);
        
        
        private void UpdateSlotMetadata()
        {
            //if (_globalSave.LastPlayedSlotIndex >= 0 && 
            //    _globalSave.LastPlayedSlotIndex < _globalSave.GameSlots.Count)
            //{
            //    _globalSave.UpdateSlotMetadata(_globalSave.LastPlayedSlotIndex, _cachedSlotData);
            //}
            if (_globalSave.GameSlots.Count > 0)
            {
                _globalSave.UpdateSlotMetadata(SINGLE_SLOT_INDEX, _cachedSlotData);
            }
        }
        
        /// <summary>
        /// 슬롯 삭제 시 호출되는 메소드. 메타데이터 및 실제 파일 삭제.
        /// </summary>
        /// <param name="slotIndex"> 삭제할 슬롯 인덱스 </param>
        /// <returns> 잘 되었는가? </returns>
        public bool DeleteSlot(int slotIndex = SINGLE_SLOT_INDEX)
        {
            //if (slotIndex < 0 || slotIndex >= _globalSave.GameSlots.Count)
            //{
            //    Debug.LogError($"Invalid slot index: {slotIndex}");
            //    return false;
            //}
//
            //string deletedSlotName = _globalSave.GameSlots[slotIndex].slotName;
            //
            //// 개별 슬롯 파일 삭제
            //string slotFileName = $"{SystemString.SlotPrefix}{slotIndex}";
            //DeleteSlotFile(slotFileName);
            //
            //// 글로벌 데이터에서의 슬롯 메타데이터 삭제
            //bool success = _globalSave.DeleteSlot(slotIndex);
            //
            //// 현재 캐시된 슬롯이 삭제된 경우 정리
            //if (_globalSave.LastPlayedSlotIndex == slotIndex)
            //{
            //    _cachedSlotData = null;
            //}
            //
            //SaveGlobalData();
            //Debug.Log($"Slot deleted: {deletedSlotName}");
            //return success;
            
            Debug.Log("[DeleteSlot] Clearing single slot...");
    
            slotIndex = SINGLE_SLOT_INDEX;
    
            // 슬롯 파일 삭제
            string slotFileName = $"{SystemString.SlotPrefix}{slotIndex}";
            DeleteSlotFile(slotFileName);
    
            // 메타데이터 초기화
            _globalSave.GameSlots.Clear();
            _globalSave.LastPlayedSlotIndex = -1;
    
            // 캐시된 슬롯 정리
            _cachedSlotData = null;
    
            SaveGlobalData();
            Debug.Log("[DeleteSlot] Slot cleared successfully.");
            return true;
        }
        
        private void DeleteSlotFile(string fileName)
        {
            try
            {
                string filePath = System.IO.Path.Combine(GetSaveDirectory(), $"{fileName}.json");
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    Debug.Log($"Deleted slot file: {fileName}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to delete slot file {fileName}: {ex.Message}");
            }
        }
        
        private string GetSaveDirectory()
        {
            return System.IO.Path.Combine(Application.persistentDataPath, "userdata");
        }
        
        public List<SlotMetaData> GetAllSlots()
        {
            return _globalSave.GameSlots;
        }
        
        #endregion
        
        #endregion
        
        #region Asynchronous Save & Load
        public async Task<bool> LoadSlotAsync(int slotIndex, CancellationToken ct)
        {
            string path = SystemString.GetSlotName(slotIndex) + SystemString.JsonExtension;
            GameSlotData loaded = await SlotIO.LoadAsync(path, ct);

            _cachedSlotData = loaded;
            _globalSave.LastPlayedSlotIndex = slotIndex;
            SaveGlobalData();
            
            SlotLoaded?.Invoke(_cachedSlotData);
            return true;
        }

        public async Task SaveCurrentSlotAsync(SystemEnum.GameState state)
        {
            if (!HasCurrentSlot) return;
            CaptureFeaturesForState(state);
            
            _cachedSlotData.lastGameState = state;
            _cachedSlotData.lastSavedTime = DateTime.Now;
            
            string path = SystemString.GetSlotName(_globalSave.LastPlayedSlotIndex) + SystemString.JsonExtension;
            await AsyncJobQueue.EnqueueKeyedWithCompletion("save-slot", token => SlotIO.SaveAsync(path, _cachedSlotData, token));
            
            UpdateSlotMetadata();
            SaveGlobalData();
        }
        #endregion


        public void RegisterProvider(IFeatureSaveProvider featureProvider)
        {
            if (featureProvider == null) return;
            _providers[featureProvider.FeatureName] = featureProvider;
        }

        public void UnregisterProvider(IFeatureSaveProvider featureProvider)
        {
            if (featureProvider == null) return;
            _providers.Remove(featureProvider.FeatureName);
        }

        private void CaptureFeaturesForState(SystemEnum.GameState state)
        {
            if (!_stateToFeatures.TryGetValue(state, out List<string> features) || features == null)
                return;

            foreach (string key in features)
            {
                if (_providers.TryGetValue(key, out IFeatureSaveProvider provider))
                    CaptureProvider(provider);
            }
        }

        private void CaptureAllRegisteredFeatures()
        {
            foreach (IFeatureSaveProvider provider in _providers.Values)
                CaptureProvider(provider);
        }

        private void CaptureProvider(IFeatureSaveProvider provider)
        {
            FeatureSnapshot snapshot = provider?.Capture();
            if (snapshot == null)
            {
                Debug.LogWarning($"[SaveLoadManager] Provider '{provider?.FeatureName}' returned no snapshot.");
                return;
            }

            if (!string.Equals(snapshot.Feature, provider.FeatureName, StringComparison.Ordinal))
            {
                Debug.LogError(
                    $"[SaveLoadManager] Provider key mismatch: provider='{provider.FeatureName}', snapshot='{snapshot.Feature}'.");
                return;
            }

            _cachedSlotData.WriteSnapshot(snapshot);
        }
    }
}
