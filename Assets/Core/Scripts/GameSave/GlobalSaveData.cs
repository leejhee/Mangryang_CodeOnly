using Core.Scripts.Foundation.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Scripts.GameSave
{
    [Serializable]
    public class GlobalSaveData
    {
        [JsonProperty("UID")]
        public readonly string UID;
        
        [JsonProperty("FirstInstallTime")]
        public readonly DateTime FirstInstallTime;
        
        [JsonProperty("GameSlots")]
        public List<SlotMetaData> GameSlots;
        
        [JsonProperty("LastPlayedSlotIndex")]
        public int LastPlayedSlotIndex;
        
        [JsonProperty("maxSlotCount")]
        public readonly int maxSlotCount = 10; 
        
        [JsonProperty("GameSettings")]
        public GameSettings GameSettings;

        [JsonProperty("MasterSeed")] 
        public ulong MasterSeed;

        [JsonProperty("SlotGeneration")]
        public ulong SlotGeneration;
        
        
        #region Properties
        /// <summary>
        /// 마지막으로 플레이한 게임 데이터
        /// </summary>
        [JsonIgnore]
        public SlotMetaData LastPlayedSlotData
        {
            get
            {
                if (LastPlayedSlotIndex == -1 || LastPlayedSlotIndex >= GameSlots.Count)
                {
                    Debug.Log("No Slot Data Yet");
                    return null;
                }
                return GameSlots[LastPlayedSlotIndex];
            }
        }
        
        [JsonIgnore]
        public bool HasEmptySlot => GameSlots.Count < maxSlotCount || GameSlots.Exists(slot => slot.isEmpty);

        [JsonIgnore] 
        public int PlayableSlotCount => GameSlots.Count(slot => slot.IsPlayable());
        
        #endregion
        
        #region Constructors
        /// <summary>
        /// 어플리케이션 인스턴스 단위 전역 세이브 데이터
        /// </summary>
        public GlobalSaveData()
        {
            UID = Guid.NewGuid().ToString();
            FirstInstallTime = DateTime.Now;
            
            GameSlots = new List<SlotMetaData>();
            GameSettings = new GameSettings();
            LastPlayedSlotIndex = -1;

            MasterSeed = RandomUtil.Mix3(
                (ulong)FirstInstallTime.Ticks,
                RandomUtil.StringHash64(UID),
                1UL);
            SlotGeneration = 0;
        }

        [JsonConstructor]
        public GlobalSaveData(
            [JsonProperty("UID")] string uid,
            [JsonProperty("FirstInstallTime")] DateTime firstInstallTime,
            [JsonProperty("GameSettings")] GameSettings gameSettings,
            [JsonProperty("LastPlayedSlotIndex")] int lastPlayedSlotIndex,
            [JsonProperty("GameSlots")] List<SlotMetaData> gameSlots,
            [JsonProperty("MasterSeed")] ulong masterSeed = 0,
            [JsonProperty("SlotGeneration")] ulong slotGeneration = 0)
        {
            UID = uid ?? Guid.NewGuid().ToString();
            FirstInstallTime = firstInstallTime;
            GameSlots = gameSlots ?? new List<SlotMetaData>();
            LastPlayedSlotIndex = lastPlayedSlotIndex;
            GameSettings = gameSettings ?? new GameSettings();
            MasterSeed = masterSeed != 0 
                ? masterSeed 
                : RandomUtil.Mix3(
                (ulong)FirstInstallTime.Ticks,
                RandomUtil.StringHash64(UID),
                1UL);
            SlotGeneration = slotGeneration;
        }

        #endregion
        
        #region Slot Management Methods
        /// <summary>
        /// 새 슬롯 메타데이터 추가
        /// </summary>
        public int AddNewSlot(string slotName)
        {
            if (GameSlots.Count >= maxSlotCount)
            {
                throw new InvalidOperationException($"Cannot create more than {maxSlotCount} slots");
            }

            var newSlot = new SlotMetaData(slotName);
            GameSlots.Add(newSlot);
            return GameSlots.Count - 1;
        }

        /// <summary>
        /// 기존 빈 슬롯 재사용 또는 새 슬롯 생성
        /// </summary>
        public int GetOrCreateSlot(string slotName)
        {
            // 빈 슬롯 찾기
            for (int i = 0; i < GameSlots.Count; i++)
            {
                if (GameSlots[i].isEmpty)
                {
                    GameSlots[i] = new SlotMetaData(slotName);
                    return i;
                }
            }

            // 빈 슬롯이 없으면 새로 생성
            return AddNewSlot(slotName);
        }

        /// <summary>
        /// 슬롯 메타데이터 업데이트
        /// </summary>
        public void UpdateSlotMetadata(int slotIndex, GameSlotData gameSlot)
        {
            if (slotIndex >= 0 && slotIndex < GameSlots.Count)
            {
                GameSlots[slotIndex].SyncWithGameSlot(gameSlot);
            }
            else
            {
                Debug.LogError("Slot Index out of range");
            }
        }

        /// <summary>
        /// 슬롯 삭제 (메타데이터만)
        /// </summary>
        public bool DeleteSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= GameSlots.Count)
            {
                return false;
            }

            // 슬롯을 빈 상태로 만들기 (완전 삭제는 안함 - 인덱스 유지)
            GameSlots[slotIndex].Clear();

            // 마지막 플레이 슬롯이 삭제된 경우 초기화
            if (LastPlayedSlotIndex == slotIndex)
            {
                LastPlayedSlotIndex = -1;
            }

            return true;
        }

        public void ClearAllSlots()
        {
            GameSlots.Clear();
            LastPlayedSlotIndex = -1;
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// 글로벌 데이터 요약 정보
        /// </summary>
        public string GetSummary()
        {
            return $"UID: {UID}\n" +
                   $"Install Date: {FirstInstallTime : yyyy-MM-dd}\n" +
                   $"Total Slots: {GameSlots.Count}/{maxSlotCount}\n" +
                   $"Playable Slots: {PlayableSlotCount}\n" +
                   $"Last Played: {(LastPlayedSlotData?.slotName ?? "None")}";
        }

        /// <summary>
        /// 디버깅용 정보 출력
        /// </summary>
        public void PrintDebugInfo()
        {
            Debug.Log("=== GlobalSaveData Debug Info ===");
            Debug.Log(GetSummary());
            
            for (int i = 0; i < GameSlots.Count; i++)
            {
                var slot = GameSlots[i];
                Debug.Log($"Slot {i}: {slot.GetSlotSummary()}");
            }
        }
        #endregion
    }
}
