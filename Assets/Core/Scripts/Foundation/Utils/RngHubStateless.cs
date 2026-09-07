using System.Collections.Generic;
using static Core.Scripts.Foundation.Utils.RandomUtil;

namespace Core.Scripts.Foundation.Utils
{
    public sealed class RngHubStateless
    {
        private readonly ulong _slotSeed;
        private readonly Dictionary<string, ulong> _counters;

        public RngHubStateless(ulong slotSeed, Dictionary<string, ulong> counters)
        { _slotSeed = slotSeed; _counters = counters ?? new(); }

        private ulong Bump(string name)
        {
            if (!_counters.TryGetValue(name, out var c)) c = 0;
            c++; _counters[name] = c;
            return Mix3(_slotSeed, StringHash64(name), c);
        }

        public int NextInt(string name, int min, int max)
        {
            var g = new GameRandom(Bump(name));
            return g.Next(min, max);
        }

        public float NextFloat01(string name)
        {
            var g = new GameRandom(Bump(name));
            return g.NextFloat();
        }

        public bool Chance(string name, float percent)
        {
            var g = new GameRandom(Bump(name));
            return g.Chance(percent);
        }

        public int WeightedChoice(string name, float[] weights)
        {
            var g = new GameRandom(Bump(name));
            return g.WeightedChoice(weights);
        }
        
        #region 새로 추가된 메서드들 (시드 및 카운터 관리)
        
        /// <summary>
        /// 특정 카테고리의 현재 카운터 값 조회
        /// </summary>
        public ulong GetCounter(string category)
        {
            return _counters.TryGetValue(category, out var count) ? count : 0;
        }

        /// <summary>
        /// 특정 카테고리의 카운터를 수동으로 증가
        /// (주의: 일반적으로는 DeriveAndIncrementSeed 사용 권장)
        /// </summary>
        public void IncrementCounter(string category)
        {
            if (!_counters.ContainsKey(category))
                _counters[category] = 0;
            
            _counters[category]++;
        }

        /// <summary>
        /// 특정 카테고리의 시드를 파생하고 카운터를 증가시킴
        /// 독립적인 시스템(맵 생성, 전투, 상점 등)에서 사용
        /// </summary>
        /// <param name="category">카테고리 이름 (예: "Explore_MOUNTAIN_BACK_1")</param>
        /// <returns>파생된 시드</returns>
        public ulong DeriveAndIncrementSeed(string category)
        {
            ulong counter = GetCounter(category);
            
            // 시드 믹싱: slotSeed + categoryHash + counter
            ulong seed = Mix3(
                _slotSeed,
                StringHash64(category),
                counter
            );
            
            IncrementCounter(category);
            return seed;
        }

        /// <summary>
        /// 카테고리별 독립적인 RNG 인스턴스 생성 (카운터 증가 없음)
        /// 동일한 카테고리로 여러 번 호출해도 같은 RNG 반환
        /// 주의: 상태가 유지되지 않으므로, 매번 새 인스턴스 생성됨
        /// </summary>
        public GameRandom GetCategoryRng(string category)
        {
            ulong counter = GetCounter(category);
            ulong categorySeed = Mix3(
                _slotSeed,
                StringHash64(category),
                counter
            );
            return new GameRandom(categorySeed);
        }

        /// <summary>
        /// 시드 파생 (카운터 증가 없음) - 읽기 전용
        /// </summary>
        public ulong DeriveSeedWithoutIncrement(string category)
        {
            ulong counter = GetCounter(category);
            return Mix3(
                _slotSeed,
                StringHash64(category),
                counter
            );
        }
        
        #endregion

        #region 디버깅 및 유틸리티
        
        /// <summary>
        /// 모든 카운터 정보 출력
        /// </summary>
        public string GetCountersDebugInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[RngHubStateless] SlotSeed: {_slotSeed}");
            sb.AppendLine($"Counters ({_counters.Count}):");
            foreach (var kvp in _counters)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 특정 카테고리 카운터 리셋
        /// </summary>
        public void ResetCounter(string category)
        {
            if (_counters.ContainsKey(category))
                _counters[category] = 0;
        }
        
        #endregion
    }
}