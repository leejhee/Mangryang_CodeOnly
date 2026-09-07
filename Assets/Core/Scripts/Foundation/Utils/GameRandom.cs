using System;
using UnityEngine;

namespace Core.Scripts.Foundation.Utils
{
    /// <summary>
    /// 이 게임 내의 랜덤 구현
    /// XoroShiro128++ 기반
    /// </summary>
    [Serializable]
    public sealed class GameRandom
    {
         #region Core State
         [SerializeField] private ulong s0, s1;
         #endregion
     
         #region Constructors
         public GameRandom() : this((ulong)DateTime.Now.Ticks) { }
         
         public GameRandom(ulong seed)
         {
             SetSeed(seed);
         }
     
         public GameRandom(string seedString)
         {
             SetSeed(StringToSeed(seedString));
         }
     
         public GameRandom(int legacySeed)
         {
             SetSeed((ulong)legacySeed);
         }
         #endregion
     
         #region Seed Management
         private void SetSeed(ulong seed)
         {
             // SplitMix64로 시드 분산하여 모든 비트 활용
             s0 = SplitMix64(ref seed);
             s1 = SplitMix64(ref seed);
             
             // 0,0 상태 방지
             if (s0 == 0 && s1 == 0)
             {
                 s0 = 1;
             }
         }
     
         private static ulong SplitMix64(ref ulong x)
         {
             ulong z = (x += 0x9e3779b97f4a7c15UL);
             z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9UL;
             z = (z ^ (z >> 27)) * 0x94d049bb133111ebUL;
             return z ^ (z >> 31);
         }
     
         private static ulong StringToSeed(string str)
         {
             if (string.IsNullOrEmpty(str)) return 1;
             
             ulong hash = 14695981039346656037UL; // FNV offset basis
             foreach (char c in str)
             {
                 hash ^= c;
                 hash *= 1099511628211UL; // FNV prime
             }
             return hash;
         }
         #endregion
     
         #region Core Generation
         public ulong NextULong()
         {
             ulong s0_local = s0;
             ulong s1_local = s1;
             ulong result = RotateLeft(s0_local + s1_local, 17) + s0_local;
     
             s1_local ^= s0_local;
             s0 = RotateLeft(s0_local, 49) ^ s1_local ^ (s1_local << 21);
             s1 = RotateLeft(s1_local, 28);
     
             return result;
         }
     
         [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
         private static ulong RotateLeft(ulong x, int k)
         {
             return (x << k) | (x >> (64 - k));
         }
         #endregion
     
         #region Game-Optimized Methods

         public int Next()
         {
             return (int)(NextULong() >> 33);
         }
     
         /// <summary>
         /// System.Random.Next(max) 대체 - 편향 없는 구현
         /// </summary>
         public int Next(int maxValue)
         {
             if (maxValue <= 0) return 0;
             if (maxValue == 1) return 0;
     
             // 편향 제거를 위한 rejection sampling
             ulong range = (ulong)maxValue;
             ulong threshold = (0x100000000UL % range);
             
             ulong value;
             do
             {
                 value = NextULong() >> 32;
             } while (value < threshold);
             
             return (int)(value % range);
         }
     
         /// <summary>
         /// System.Random.Next(min, max) 대체
         /// </summary>
         public int Next(int minValue, int maxValue)
         {
             if (minValue >= maxValue) return minValue;
             return Next(maxValue - minValue) + minValue;
         }
     
         /// <summary>
         /// System.Random.NextDouble() 대체 - 더 높은 정밀도
         /// </summary>
         public double NextDouble()
         {
             return (NextULong() >> 11) * (1.0 / (1UL << 53));
         }
     
         /// <summary>
         /// UnityEngine.Random.Range(float, float) 대체
         /// </summary>
         public float NextFloat()
         {
             return (float)((NextULong() >> 40) * (1.0 / (1 << 24)));
         }
     
         public float NextFloat(float min, float max)
         {
             return NextFloat() * (max - min) + min;
         }
     
         /// <summary>
         /// 게임에서 자주 사용하는 확률 체크
         /// </summary>
         public bool NextBool() => (NextULong() & 1) == 1;
         
         public bool Chance(float percentage) => NextFloat() < (percentage / 100f);
         
         public bool Chance(double percentage) => NextDouble() < (percentage / 100.0);
         #endregion
     
         #region Game-Specific Utilities
         /// <summary>
         /// 배열에서 랜덤 요소 선택
         /// </summary>
         public T Choice<T>(T[] array)
         {
             if (array == null || array.Length == 0) return default(T);
             return array[Next(array.Length)];
         }
     
         /// <summary>
         /// Unity Vector2 범위 내 랜덤 위치
         /// </summary>
         public Vector2 NextVector2(Vector2 min, Vector2 max)
         {
             return new Vector2(
                 NextFloat(min.x, max.x),
                 NextFloat(min.y, max.y)
             );
         }
     
         /// <summary>
         /// Unity Vector3 범위 내 랜덤 위치
         /// </summary>
         public Vector3 NextVector3(Vector3 min, Vector3 max)
         {
             return new Vector3(
                 NextFloat(min.x, max.x),
                 NextFloat(min.y, max.y),
                 NextFloat(min.z, max.z)
             );
         }
     
         /// <summary>
         /// 원 내부의 랜덤 점 (UnityEngine.Random.insideUnitCircle 대체)
         /// </summary>
         public Vector2 InsideUnitCircle()
         {
             float angle = NextFloat() * 2f * Mathf.PI;
             float radius = Mathf.Sqrt(NextFloat());
             return new Vector2(
                 Mathf.Cos(angle) * radius,
                 Mathf.Sin(angle) * radius
             );
         }
     
         /// <summary>
         /// 구 내부의 랜덤 점 (UnityEngine.Random.insideUnitSphere 대체)
         /// </summary>
         public Vector3 InsideUnitSphere()
         {
             float phi = NextFloat() * 2f * Mathf.PI;
             float costheta = NextFloat() * 2f - 1f;
             float u = NextFloat();
     
             float theta = Mathf.Acos(costheta);
             float r = Mathf.Pow(u, 1f/3f);
     
             float x = r * Mathf.Sin(theta) * Mathf.Cos(phi);
             float y = r * Mathf.Sin(theta) * Mathf.Sin(phi);
             float z = r * Mathf.Cos(theta);
     
             return new Vector3(x, y, z);
         }
         #endregion
     
         #region Advanced Features
         /// <summary>
         /// 독립적인 서브스트림 생성 - 병렬 생성에 유용
         /// </summary>
         public GameRandom CreateSubstream()
         {
             var substreamSeed = NextULong();
             return new GameRandom(substreamSeed);
         }
     
         /// <summary>
         /// Jump 기능 - 2^64개의 값을 건너뛰어 독립적인 시퀀스 생성
         /// 멀티스레딩이나 독립적인 시스템에 유용
         /// </summary>
         public void Jump()
         {
             ulong[] JUMP = { 0x2bd7a6a6e99c2ddcUL, 0x0992ccaf6a6fca05UL };
             
             ulong s0_temp = 0;
             ulong s1_temp = 0;
             
             for (int i = 0; i < JUMP.Length; i++)
             {
                 for (int b = 0; b < 64; b++)
                 {
                     if ((JUMP[i] & (1UL << b)) != 0)
                     {
                         s0_temp ^= s0;
                         s1_temp ^= s1;
                     }
                     NextULong();
                 }
             }
             
             s0 = s0_temp;
             s1 = s1_temp;
         }
     
         /// <summary>
         /// 가중치 기반 선택 - 게임의 루트 시스템 등에 유용
         /// </summary>
         public int WeightedChoice(float[] weights)
         {
             if (weights == null || weights.Length == 0) return -1;
             
             float totalWeight = 0f;
             for (int i = 0; i < weights.Length; i++)
             {
                 totalWeight += weights[i];
             }
             
             if (totalWeight <= 0f) return -1;
             
             float randomValue = NextFloat() * totalWeight;
             float currentWeight = 0f;
             
             for (int i = 0; i < weights.Length; i++)
             {
                 currentWeight += weights[i];
                 if (randomValue <= currentWeight)
                 {
                     return i;
                 }
             }
             
             return weights.Length - 1;
         }
         #endregion
     
         #region State Management
         /// <summary>
         /// 현재 상태 저장 (세이브 시스템용)
         /// </summary>
         public RandomState GetState() => new RandomState(s0, s1);
     
         /// <summary>
         /// 상태 복원 (로드 시스템용)
         /// </summary>
         public void SetState(RandomState state)
         {
             s0 = state.s0;
             s1 = state.s1;
         }
     
         /// <summary>
         /// 현재 시드 문자열로 변환 (디버깅용)
         /// </summary>
         public override string ToString()
         {
             return $"GameRandom[s0={s0:X16}, s1={s1:X16}]";
         }
         #endregion
    }

    /// <summary>
    /// 랜덤 생성기 상태 저장용 구조체
    /// </summary>
    [Serializable]
    public struct RandomState
    {
        public ulong s0, s1;
        
        public RandomState(ulong s0, ulong s1)
        {
            this.s0 = s0;
            this.s1 = s1;
        }
    }
}
