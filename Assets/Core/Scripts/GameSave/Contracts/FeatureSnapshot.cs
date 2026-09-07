using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

namespace Core.Scripts.GameSave.Contracts
{
    [Serializable]
    public abstract class FeatureSnapshot
    {
        /// <summary>
        /// 어떤 종류의 스냅샷 데이터인가?(슬롯데이터의 key로 활용)
        /// </summary>
        [JsonProperty] public string Feature { get; private set; }
        [JsonProperty] public int    Version { get; private set; }

        protected FeatureSnapshot(string feature, int version)
        {
            Feature = feature;
            Version = version;
        }

        // 각 파생 클래스에서 현재 버전 상수 노출
        [JsonIgnore] public abstract int CurrentVersion { get; }

        // 필요하면 버전 마이그레이션
        public abstract void MigrateIfNeeded();

        /// <summary>
        /// 저장 캐시와 런타임 상태가 같은 가변 객체를 공유하지 않도록 깊은 복사본을 만든다.
        /// </summary>
        public abstract FeatureSnapshot DeepCopy();

        // JSON 역직렬화 뒤 자동으로 마이그레이션 한 번 호출
        [OnDeserialized]
        private void OnDeserialized(StreamingContext _)
        {
            MigrateIfNeeded();
        }

        // 파생 클래스에서 마이그레이션 후 실제 Version을 현재로 맞출 때 호출
        protected void BumpVersion() => Version = CurrentVersion;
    }
}
