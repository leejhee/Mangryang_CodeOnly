using Core.Scripts.Foundation.Define;
using Newtonsoft.Json;
using System;

namespace Core.Scripts.GameSave
{
    /// <summary>
    /// SaveLoadManager가 Slot을 찾기 위한 key 역할의 클래스
    /// </summary>
    [Serializable]
    public class SlotMetaData
    {
        [JsonProperty("SlotName")]
        public string slotName;
        [JsonProperty("lastSavedTime")]
        public DateTime lastSavedTime;
        [JsonProperty("lastGameState")]
        public SystemEnum.GameState lastGameState;
        [JsonProperty("playTime")]
        public long playTimeTicks;
        
        [JsonIgnore]
        public TimeSpan PlayTime 
        { 
            get => new TimeSpan(playTimeTicks);
            set => playTimeTicks = value.Ticks;
        }
        
        [JsonProperty("isEmpty")]
        public bool isEmpty = true;
        
        [JsonProperty("createdTime")]
        public DateTime createdTime;

        #region Constructors
        /// <summary>
        /// 기본 생성자 (빈 슬롯용)
        /// </summary>
        public SlotMetaData()
        {
            createdTime = DateTime.Now;
            lastSavedTime = DateTime.Now;
        }

        /// <summary>
        /// 새 게임 슬롯 생성용 생성자
        /// </summary>
        public SlotMetaData(string slotName)
        {
            this.slotName = slotName;
            createdTime = DateTime.Now;
            lastSavedTime = DateTime.Now;
            isEmpty = false;
        }

        /// <summary>
        /// GameSlotData에서 메타데이터 추출용
        /// </summary>
        public SlotMetaData(GameSlotData gameSlot)
        {
            slotName = gameSlot.slotName;
            lastSavedTime = gameSlot.lastSavedTime;
            lastGameState = gameSlot.lastGameState;
            playTimeTicks = gameSlot.playTimeTicks;
            isEmpty = false;
            createdTime = DateTime.Now; // 메타데이터 생성 시점
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// UI 표시용 슬롯 요약 정보
        /// </summary>
        public string GetSlotSummary()
        {
            if (isEmpty)
            {
                return $"{slotName} | Empty Slot";
            }

            return $"{slotName} | {lastGameState} | {PlayTime:hh\\:mm\\:ss} | {lastSavedTime:yyyy-MM-dd HH:mm}";
        }

        /// <summary>
        /// 슬롯이 플레이 가능한 상태인지 확인
        /// </summary>
        public bool IsPlayable()
        {
            return !isEmpty && !string.IsNullOrEmpty(slotName);
        }

        /// <summary>
        /// GameSlotData와 동기화
        /// </summary>
        public void SyncWithGameSlot(GameSlotData gameSlot)
        {
            slotName = gameSlot.slotName;
            lastSavedTime = gameSlot.lastSavedTime;
            lastGameState = gameSlot.lastGameState;
            playTimeTicks = gameSlot.playTimeTicks;
            isEmpty = false;
        }

        /// <summary>
        /// 슬롯 초기화 (빈 슬롯으로 만들기)
        /// </summary>
        public void Clear()
        {
            slotName = "New Game";
            lastGameState = SystemEnum.GameState.Village;
            playTimeTicks = 0;
            isEmpty = true;
            lastSavedTime = DateTime.Now;
        }
        #endregion
    }
}