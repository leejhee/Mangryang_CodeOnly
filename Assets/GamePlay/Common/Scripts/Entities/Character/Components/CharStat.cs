using Core.Scripts.Data;
using GamePlay.Features.Battle.Scripts.Models;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Core.Scripts.Foundation.Define.SystemEnum;

namespace GamePlay.Common.Scripts.Entities.Character.Components
{
    [Serializable]
    public class CharStat
    {
        private long[] _charStat = new long[(int)eStats.eMax];
        
        private Dictionary<eStats, List<StatModifier>> _modifiers = new Dictionary<eStats, List<StatModifier>>();
        
        private Dictionary<eStats, DerivedStatRule> _derivedRules = new Dictionary<eStats, DerivedStatRule>();

        #region Modifier System
        
        public class StatModifier
        {
            public string Id;
            public ModifierType Type;
            public float Value;
            
            public enum ModifierType
            {
                FlatAdd,        // 고정값 더하기
                PercentAdd,     // 퍼센트 더하기
                PercentMult     // 퍼센트 곱하기
            }
        }
        
        /// <summary> 버프/디버프 </summary>
        public void AddModifier(eStats stat, StatModifier modifier)
        {
            eStats realStat = GetProperStatAttribute(stat);
            if (!_modifiers.ContainsKey(realStat))
                _modifiers[realStat] = new List<StatModifier>();
            
            _modifiers[realStat].Add(modifier);
            RecalculateStat(realStat);
        }
        
        /// <summary> 버프/디버프 제거 </summary>
        public void RemoveModifier(eStats stat, string modifierId)
        {
            eStats realStat = GetProperStatAttribute(stat);
            if (_modifiers.ContainsKey(realStat))
            {
                _modifiers[realStat].RemoveAll(m => m.Id == modifierId);
                RecalculateStat(realStat);
            }
        }
        
        /// <summary>
        /// 특정 스탯의 모든 버프 제거
        /// </summary>
        public void ClearModifiers(eStats stat)
        {
            eStats realStat = GetProperStatAttribute(stat);
            if (_modifiers.ContainsKey(realStat))
            {
                _modifiers[realStat].Clear();
                RecalculateStat(realStat);
            }
        }
        
        #endregion

        #region Derived Stat System
        
        /// <summary> 파생 스탯 계산 규칙 베이스 </summary>
        public abstract class DerivedStatRule
        {
            /// <summary> 초기값(접두사 없음)으로부터 파생값 계산 </summary>
            public abstract long CalculateFromBase(CharStat owner, eStats baseStat);
            
            /// <summary> 이 파생 스탯이 의존하는 초기 스탯 </summary>
            public abstract eStats GetBaseStat();
        }
        
        // base * multiplier + offset
        private class LinearDerivedRule : DerivedStatRule
        {
            private eStats _baseStat;
            private int _multiplier;
            private int _offset;
            
            public LinearDerivedRule(eStats baseStat, int multiplier, int offset = 0)
            {
                _baseStat = baseStat;
                _multiplier = multiplier;
                _offset = offset;
            }
            
            public override long CalculateFromBase(CharStat owner, eStats baseStat)
            {
                return owner._charStat[(int)_baseStat] * _multiplier + _offset;
            }
            
            public override eStats GetBaseStat() => _baseStat;
        }
        
        // 복잡한 파생 규칙용 (커스텀 계산)
        private class CustomDerivedRule : DerivedStatRule
        {
            private eStats _baseStat;
            private Func<CharStat, long> _calculator;
            
            public CustomDerivedRule(eStats baseStat, Func<CharStat, long> calculator)
            {
                _baseStat = baseStat;
                _calculator = calculator;
            }
            
            public override long CalculateFromBase(CharStat owner, eStats baseStat)
            {
                return _calculator(owner);
            }
            
            public override eStats GetBaseStat() => _baseStat;
        }
        
        /// <summary>
        /// 파생 스탯 규칙 초기화
        /// </summary>
        private void InitializeDerivedRules()
        {
            // Red 
            _derivedRules[eStats.NPHYSICAL_ATTACK] = new LinearDerivedRule(eStats.RED, 4, 2);
            _derivedRules[eStats.NCRIT_RATE] = new LinearDerivedRule(eStats.RED, 2);
            
            // Blue
            _derivedRules[eStats.NDEFENSE] = new LinearDerivedRule(eStats.BLUE, 3, 5);
            _derivedRules[eStats.NMAGIC_RESIST] = new LinearDerivedRule(eStats.BLUE, 3, 5);
            _derivedRules[eStats.NAILMENT_RESISTANCE] = new LinearDerivedRule(eStats.BLUE, 3);
            
            // Yellow
            _derivedRules[eStats.NMHP] = new LinearDerivedRule(eStats.YELLOW, 5, 12);
            _derivedRules[eStats.NSPEED] = new LinearDerivedRule(eStats.YELLOW, 2, 10);
            _derivedRules[eStats.NEVATION] = new LinearDerivedRule(eStats.YELLOW, 2, 5);
            
            // Black
            _derivedRules[eStats.NMAGIC_ATTACK] = new LinearDerivedRule(eStats.BLACK, 4, 2);
            _derivedRules[eStats.NMOVEMENT] = new LinearDerivedRule(eStats.BLACK, 4, 8);
            
            // White
            _derivedRules[eStats.NAILMENT_INFLICT] = new LinearDerivedRule(eStats.WHITE, 3);
            _derivedRules[eStats.NACCURACY] = new LinearDerivedRule(eStats.WHITE, 3);
            
            // 2차 파생: MOVEMENT → ACTION_POINT (정수 나눗셈이므로 재계산 필요)
            _derivedRules[eStats.NMACTION_POINT] = new CustomDerivedRule(eStats.NMOVEMENT, 
                owner => owner._charStat[(int)eStats.NMOVEMENT] / 4);
        }
        
        #endregion

        #region Constructors
        
        public CharStat(CharStatData charStat)
        {
            InitializeDerivedRules();
            
            _charStat[(int)eStats.HP] = charStat.HealthPoint;
            _charStat[(int)eStats.DEFENSE] = charStat.Defense;
            _charStat[(int)eStats.MAGIC_RESIST] = charStat.MagicResist;
            _charStat[(int)eStats.PHYSICAL_ATTACK] = charStat.PhysicalAttack;
            _charStat[(int)eStats.MAGIC_ATTACK] = charStat.MagicAttack;
            _charStat[(int)eStats.CRIT_RATE] = charStat.CriticalRate;
            _charStat[(int)eStats.SPEED] = charStat.Speed;
            _charStat[(int)eStats.MOVEMENT] = charStat.Movement;
            _charStat[(int)eStats.ACTION_POINT] = charStat.Movement / 4;
            _charStat[(int)eStats.EVATION] = charStat.Evasion;
            
            _charStat[(int)eStats.NHP] = charStat.HealthPoint;
            _charStat[(int)eStats.NDEFENSE] = charStat.Defense;
            _charStat[(int)eStats.NMAGIC_RESIST] = charStat.MagicResist;
            _charStat[(int)eStats.NPHYSICAL_ATTACK] = charStat.PhysicalAttack;
            _charStat[(int)eStats.NMAGIC_ATTACK] = charStat.MagicAttack;
            _charStat[(int)eStats.NCRIT_RATE] = charStat.CriticalRate;
            _charStat[(int)eStats.NSPEED] = charStat.Speed;
            _charStat[(int)eStats.NMOVEMENT] = charStat.Movement;
            _charStat[(int)eStats.NACTION_POINT] = charStat.Movement / 4;
            _charStat[(int)eStats.NEVATION] = charStat.Evasion;
            
            // 최댓값 초기화 (NM 접두사)
            _charStat[(int)eStats.NMHP] = charStat.HealthPoint;
            _charStat[(int)eStats.NMACTION_POINT] = charStat.Movement / 4;
            
            // 추가 스탯 (버프 전용)
            _charStat[(int)eStats.RANGE_INCREASE] = 0;
            _charStat[(int)eStats.DAMAGE_INCREASE] = 0;
            _charStat[(int)eStats.ACCURACY_INCREASE] = 0;
        }
        
        public CharStat(DokkaebiData dokkaebiData)
        {
            InitializeDerivedRules();
            
            int r = dokkaebiData.ObangRed;
            int b = dokkaebiData.ObangBlue;
            int y = dokkaebiData.ObangYellow;
            int bl = dokkaebiData.ObangBlack;
            int w = dokkaebiData.ObangWhite;
            
            // 오방색 초기값 설정 (접두사 없음)
            _charStat[(int)eStats.RED] = r;
            _charStat[(int)eStats.BLUE] = b;
            _charStat[(int)eStats.YELLOW] = y;
            _charStat[(int)eStats.BLACK] = bl;
            _charStat[(int)eStats.WHITE] = w;
            
            // 오방색 현재값 설정 (N 접두사)
            _charStat[(int)eStats.N_RED] = r;
            _charStat[(int)eStats.N_BLUE] = b;
            _charStat[(int)eStats.N_YELLOW] = y;
            _charStat[(int)eStats.N_BLACK] = bl;
            _charStat[(int)eStats.N_WHITE] = w;
            
            // 모든 파생 스탯 계산
            RecalculateAllDerivedStats();
            
            // 현재 HP/AP를 최댓값으로 초기화
            _charStat[(int)eStats.NHP] = _charStat[(int)eStats.NMHP];
            _charStat[(int)eStats.NACTION_POINT] = _charStat[(int)eStats.NMACTION_POINT];
        }
        
        #endregion

        #region Stat Calculation Core
        
        /// <summary>
        /// 특정 스탯의 현재값(N) 재계산
        /// 초기값 → 파생 계산 → 버프 적용 순서로 진행
        /// </summary>
        private void RecalculateStat(eStats stat)
        {
            // 1단계: 초기값 확인
            eStats baseStat = GetBaseStatFromCurrent(stat);
            long baseValue = _charStat[(int)baseStat];
            
            // 2단계: 파생 계산 (규칙이 있는 경우)
            long derivedValue = baseValue;
            if (_derivedRules.ContainsKey(stat))
            {
                derivedValue = _derivedRules[stat].CalculateFromBase(this, baseStat);
            }
            
            // 3단계: 버프/디버프 적용
            long finalValue = ApplyModifiers(stat, derivedValue);
            
            // 4단계: 범위 제한 및 최종값 저장
            _charStat[(int)stat] = ClampStat(stat, finalValue);
            
            // 5단계: 이 스탯에 의존하는 다른 스탯 재계산
            PropagateStatChange(stat);
            
            // 6단계: 특수 케이스 처리
            HandleSpecialCases(stat);
        }
        
        /// <summary>
        /// 버프/디버프를 파생값에 적용
        /// </summary>
        private long ApplyModifiers(eStats stat, long baseValue)
        {
            if (!_modifiers.ContainsKey(stat))
                return baseValue;
            
            float flatAdd = 0;
            float percentAdd = 0;
            float percentMult = 1;
            
            foreach (var mod in _modifiers[stat])
            {
                switch (mod.Type)
                {
                    case StatModifier.ModifierType.FlatAdd:
                        flatAdd += mod.Value;
                        break;
                    case StatModifier.ModifierType.PercentAdd:
                        percentAdd += mod.Value;
                        break;
                    case StatModifier.ModifierType.PercentMult:
                        percentMult *= (1 + mod.Value / 100f);
                        break;
                }
            }
            
            // 계산 순서: (Base * (1 + %Add) + Flat) * %Mult
            float result = (baseValue * (1 + percentAdd / 100f) + flatAdd) * percentMult;
            return (long)result;
        }
        
        /// <summary>
        /// 스탯 변경 전파 (의존성이 있는 다른 스탯 갱신)
        /// </summary>
        private void PropagateStatChange(eStats changedStat)
        {
            // 모든 파생 규칙을 검사하여 의존성이 있는 스탯 재계산
            foreach (var kvp in _derivedRules)
            {
                eStats dependentStat = kvp.Key;
                DerivedStatRule rule = kvp.Value;
                
                // 변경된 스탯이 이 파생 스탯의 베이스인 경우
                if (rule.GetBaseStat() == changedStat)
                {
                    RecalculateStat(dependentStat);
                }
            }
        }
        
        /// <summary>
        /// 모든 파생 스탯 재계산 (초기화 시 사용)
        /// </summary>
        private void RecalculateAllDerivedStats()
        {
            // 의존성 순서대로 계산
            // 1차 파생: 오방색 → 기본 스탯
            foreach (var stat in _derivedRules.Keys)
            {
                // NMACTION_POINT는 2차 파생이므로 나중에
                if (stat != eStats.NMACTION_POINT)
                {
                    RecalculateStat(stat);
                }
            }
            
            // 2차 파생: NMOVEMENT → NMACTION_POINT
            if (_derivedRules.ContainsKey(eStats.NMACTION_POINT))
            {
                RecalculateStat(eStats.NMACTION_POINT);
            }
        }
        
        /// <summary>
        /// 특수 케이스 처리 (최댓값 변경 시 현재값 클램프 등)
        /// </summary>
        private void HandleSpecialCases(eStats stat)
        {
            switch (stat)
            {
                case eStats.NMHP:
                    // 최대 HP 변경 시 현재 HP가 넘치면 클램프
                    long currentHp = _charStat[(int)eStats.NHP];
                    long maxHp = _charStat[(int)eStats.NMHP];
                    if (currentHp > maxHp)
                    {
                        long delta = maxHp - currentHp;
                        _charStat[(int)eStats.NHP] = maxHp;
                        OnStatChanged?.Invoke(eStats.NHP, delta, maxHp);
                    }
                    break;
                    
                case eStats.NMACTION_POINT:
                    // 최대 AP 변경 시 현재 AP가 넘치면 클램프
                    long currentAp = _charStat[(int)eStats.NACTION_POINT];
                    long maxAp = _charStat[(int)eStats.NMACTION_POINT];
                    if (currentAp > maxAp)
                    {
                        long delta = maxAp - currentAp;
                        _charStat[(int)eStats.NACTION_POINT] = maxAp;
                        OnStatChanged?.Invoke(eStats.NACTION_POINT, delta, maxAp);
                    }
                    break;
            }
        }
        
        #endregion

        #region Stat Helper
        
        /// <summary>
        /// 스탯 enum을 적절한 형태로 변환 (접두사 추가)
        /// </summary>
        private eStats GetProperStatAttribute(eStats stat)
        {
            switch (stat)
            {
                case eStats.BLUE:           return eStats.N_BLUE;
                case eStats.RED:            return eStats.N_RED;
                case eStats.YELLOW:         return eStats.N_YELLOW;
                case eStats.WHITE:          return eStats.N_WHITE;
                case eStats.BLACK:          return eStats.N_BLACK;
                case eStats.HP:             return eStats.NHP;
                case eStats.DEFENSE:        return eStats.NDEFENSE;
                case eStats.MAGIC_RESIST:   return eStats.NMAGIC_RESIST;
                case eStats.PHYSICAL_ATTACK: return eStats.NPHYSICAL_ATTACK;
                case eStats.MAGIC_ATTACK:   return eStats.NMAGIC_ATTACK;
                case eStats.CRIT_RATE:      return eStats.NCRIT_RATE;
                case eStats.SPEED:          return eStats.NSPEED;
                case eStats.ACTION_POINT:   return eStats.NACTION_POINT;
                case eStats.EVATION:        return eStats.NEVATION;
                case eStats.MOVEMENT:       return eStats.NMOVEMENT;
            }
            return stat;
        }
        
        /// <summary>
        /// 현재값(N) 스탯으로부터 초기값 스탯을 추출
        /// </summary>
        private eStats GetBaseStatFromCurrent(eStats currentStat)
        {
            switch (currentStat)
            {
                case eStats.N_BLUE:         return eStats.BLUE;
                case eStats.N_RED:          return eStats.RED;
                case eStats.N_YELLOW:       return eStats.YELLOW;
                case eStats.N_WHITE:        return eStats.WHITE;
                case eStats.N_BLACK:        return eStats.BLACK;
                case eStats.NHP:            return eStats.HP;
                case eStats.NDEFENSE:       return eStats.DEFENSE;
                case eStats.NMAGIC_RESIST:  return eStats.MAGIC_RESIST;
                case eStats.NPHYSICAL_ATTACK: return eStats.PHYSICAL_ATTACK;
                case eStats.NMAGIC_ATTACK:  return eStats.MAGIC_ATTACK;
                case eStats.NCRIT_RATE:     return eStats.CRIT_RATE;
                case eStats.NSPEED:         return eStats.SPEED;
                case eStats.NACTION_POINT:  return eStats.ACTION_POINT;
                case eStats.NEVATION:       return eStats.EVATION;
                case eStats.NMOVEMENT:      return eStats.MOVEMENT;
            }
            return currentStat;
        }

        private long GetMaxOf(eStats stat)
        {
            return stat switch
            {
                eStats.NHP => _charStat[(int)eStats.NMHP],
                eStats.NACTION_POINT => _charStat[(int)eStats.NMACTION_POINT],
                _ => long.MaxValue
            };
        }

        private static long ClampLong(long v, long min, long max) => v < min ? min : (v > max ? max : v);
        
        private long ClampStat(eStats stat, long target)
        {
            switch (stat)
            {
                case eStats.NHP:
                case eStats.NACTION_POINT:
                    return ClampLong(target, 0, GetMaxOf(stat));
                
                case eStats.NCRIT_RATE:
                case eStats.NEVATION:
                case eStats.NACCURACY:
                case eStats.NAILMENT_INFLICT:
                case eStats.NAILMENT_RESISTANCE:
                    return ClampLong(target, 0, 100);
                
                case eStats.N_RED:
                case eStats.N_BLUE:
                case eStats.N_YELLOW:
                case eStats.N_WHITE:
                case eStats.N_BLACK:
                case eStats.NPHYSICAL_ATTACK:
                case eStats.NMAGIC_ATTACK:
                case eStats.NDEFENSE:
                case eStats.NMAGIC_RESIST:
                case eStats.NSPEED:
                case eStats.NMOVEMENT:
                case eStats.NMHP:
                case eStats.NMACTION_POINT:
                    return Math.Max(0, target);
                
                default:
                    return target;
            }    
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// 현재 스탯값 조회 (N 접두사)
        /// </summary>
        public long GetStat(eStats stat)
        {
            return _charStat[(int)GetProperStatAttribute(stat)];
        }

        /// <summary>
        /// 스탯 변화 이벤트 (변경된 스탯, 변화량, 결과값)
        /// </summary>
        public event Action<eStats, long, long> OnStatChanged;
        
        public void ClearChangeEvent() => OnStatChanged = null;
        
        /// <summary>
        /// 초기값 스탯 변경 (오방색 등 Base 스탯)
        /// 파생 스탯들이 자동으로 재계산됨
        /// </summary>
        public void ChangeBaseStat(eStats stat, long valueDelta)
        {
            eStats baseStat = GetBaseStatFromCurrent(GetProperStatAttribute(stat));
            eStats currentStat = GetProperStatAttribute(stat);
            
            long before = _charStat[(int)baseStat];
            long after = Math.Max(0, before + valueDelta);
            _charStat[(int)baseStat] = after;
            
            // 현재값(N) 스탯도 변경 (버프 없으면 동일)
            if (!_derivedRules.ContainsKey(currentStat))
            {
                _charStat[(int)currentStat] = after;
            }
            
            // 파생 스탯 재계산 트리거
            RecalculateStat(currentStat);
            
            long finalValue = _charStat[(int)currentStat];
            OnStatChanged?.Invoke(currentStat, valueDelta, finalValue);
        }
        
        /// <summary>
        /// 현재값 직접 변경 (HP, AP 등 소모성 리소스)
        /// 파생 계산을 수행하지 않음
        /// </summary>
        public void ChangeCurrentValue(eStats stat, long valueDelta)
        {
            eStats realStat = GetProperStatAttribute(stat);
            
            // HP, AP만 직접 변경 가능
            if (realStat == eStats.NHP || realStat == eStats.NACTION_POINT)
            {
                long before = _charStat[(int)realStat];
                long after = ClampStat(realStat, before + valueDelta);
                long actualDelta = after - before;
                
                _charStat[(int)realStat] = after;
                OnStatChanged?.Invoke(realStat, actualDelta, after);
            }
            else
            {
                Debug.LogWarning($"ChangeCurrentValue는 HP/AP에만 사용 가능: {stat}");
            }
        }
        
        /// <summary>
        /// 기존 호환성을 위한 통합 메서드
        /// </summary>
        public void ChangeStat(eStats stat, long valueDelta)
        {
            eStats realStat = GetProperStatAttribute(stat);
            
            // 오방색 Base 스탯
            if (realStat == eStats.N_RED || realStat == eStats.N_BLUE || 
                realStat == eStats.N_YELLOW || realStat == eStats.N_BLACK || 
                realStat == eStats.N_WHITE)
            {
                ChangeBaseStat(stat, valueDelta);
            }
            // 소모성 리소스
            else if (realStat == eStats.NHP || realStat == eStats.NACTION_POINT)
            {
                ChangeCurrentValue(stat, valueDelta);
            }
            else
            {
                Debug.LogWarning($"직접 변경 불가능한 파생 스탯: {stat}. 버프를 사용하세요.");
            }
        }
        
        #endregion
        
        #region API - UI Model
        
        public event Action<HPModel> OnFocusedCharHpChanged;
        public event Action<ApModel> OnFocusedCharApChanged;
        
        [Obsolete("Use ChangeCurrentValue(eStats.HP, delta) instead")]
        public void ChangeHP(int delta)
        {
            ChangeCurrentValue(eStats.HP, delta);
            OnFocusedCharHpChanged?.Invoke(new HPModel(delta));
        }
        
        [Obsolete("Use ChangeCurrentValue(eStats.ACTION_POINT, delta) instead")]
        public void ChangeAP(int delta)
        {
            ChangeCurrentValue(eStats.ACTION_POINT, delta);
            OnFocusedCharApChanged?.Invoke(new ApModel(delta));
        }
        
        #endregion
        
        #region Damage Part
        
        public void ReceiveDamage(float damage)
        {
            ChangeCurrentValue(eStats.HP, -(long)damage);
        }

        public void ReceiveHPPercentDamage(float percent)
        {
            var hp = GetStat(eStats.NMHP);
            int delta = Mathf.RoundToInt(hp * percent / 100f); 
            ReceiveDamage(delta);
        }
        
        public long GetAttackStat(eSkillType skillType)
        {
            switch (skillType)
            {
                case eSkillType.PhysicalAttack: return GetStat(eStats.NPHYSICAL_ATTACK);
                case eSkillType.MagicAttack: return GetStat(eStats.NMAGIC_ATTACK);
                default: return 0;
            }
        }
        
        public long GetDefenseStat(eSkillType skillType)
        {
            switch (skillType)
            {
                case eSkillType.PhysicalAttack: return GetStat(eStats.NDEFENSE);
                case eSkillType.MagicAttack: return GetStat(eStats.NMAGIC_RESIST);
                default: return 0;
            }
        }
        
        #endregion
        
        #region Action Point Part

        public bool UseActionPoint(float value = 1f)
        {
            if (GetStat(eStats.NACTION_POINT) >= value)
            {
                ChangeCurrentValue(eStats.ACTION_POINT, -(long)value);
                return true;
            }
            else
            {
                Debug.Log($"Action Point 부족함. {GetStat(eStats.NACTION_POINT)}");
                return false;
            }
        }
        
        #endregion
    }
}