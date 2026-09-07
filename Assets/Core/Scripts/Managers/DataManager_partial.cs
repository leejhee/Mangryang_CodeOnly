using Core.Scripts.Data;
using Core.Scripts.Foundation.Define;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Core.Scripts.Foundation.Define.SystemEnum;

namespace Core.Scripts.Managers
{
    public partial class DataManager
    {
        private Dictionary<eKeyword, KeywordData> _keywordMap = new();
        public Dictionary<eKeyword, KeywordData> KeywordMap => _keywordMap;

        private readonly Dictionary<(eSourceType sourceType, long sourceRefID), List<KeywordGrantData>>
            _keywordGrantMap = new();
        private readonly Dictionary<long, List<KeywordEffectData>> _keywordEffectMap = new();
        
        
        /// <summary>
        /// 캐릭터별 스킬 데이터 맵
        /// </summary>
        private Dictionary<long, List<SkillData>> characterSkillMap = new();
        public Dictionary<long, List<SkillData>> CharacterSkillMap => characterSkillMap;
        
        
        private void ClearJoinedMaps()
        {
            //=======Clear Data - Poco Maps==========//
            
            _keywordMap.Clear();
            _keywordGrantMap.Clear();
            _keywordEffectMap.Clear();
            characterSkillMap.Clear();
            
            //=========================================//
            
        }


        private void SetKeywordDataMap()
        {
            string key = nameof(KeywordData);
            if (_cache.ContainsKey(key) == false)
                return;
            Dictionary<long, SheetData> keywordDict = _cache[key];
            if (keywordDict == null)
            {
                Debug.LogError($"Map not included in parsing : {key}");
                return;
            }

            foreach (var _keyword in keywordDict.Values)
            {
                if (_keyword is not KeywordData keyword) continue;
                if(!_keywordMap.ContainsKey(keyword.keywordType))
                    _keywordMap.Add(keyword.keywordType, keyword);
            }
        }

        private void SetKeywordGrantDataMap()
        {
            const string key = nameof(KeywordGrantData);
            if (!_cache.TryGetValue(key, out Dictionary<long, SheetData> grantDict) || grantDict == null)
                return;

            _keywordGrantMap.Clear();
            foreach (SheetData sheetData in grantDict.Values)
            {
                if (sheetData is not KeywordGrantData grant)
                    continue;

                var mapKey = (grant.SourceType, grant.SouceRefID);
                if (!_keywordGrantMap.TryGetValue(mapKey, out List<KeywordGrantData> grants))
                {
                    grants = new List<KeywordGrantData>();
                    _keywordGrantMap.Add(mapKey, grants);
                }

                grants.Add(grant);
            }

            foreach (List<KeywordGrantData> grants in _keywordGrantMap.Values)
                grants.Sort((lhs, rhs) => lhs.index.CompareTo(rhs.index));
        }

        private void SetKeywordEffectDataMap()
        {
            const string key = nameof(KeywordEffectData);
            if (!_cache.TryGetValue(key, out Dictionary<long, SheetData> effectDict) || effectDict == null)
                return;

            _keywordEffectMap.Clear();
            foreach (SheetData sheetData in effectDict.Values)
            {
                if (sheetData is not KeywordEffectData effect)
                    continue;

                if (!_keywordEffectMap.TryGetValue(effect.KeywordID, out List<KeywordEffectData> effects))
                {
                    effects = new List<KeywordEffectData>();
                    _keywordEffectMap.Add(effect.KeywordID, effects);
                }

                effects.Add(effect);
            }

            foreach (List<KeywordEffectData> effects in _keywordEffectMap.Values)
                effects.Sort((lhs, rhs) => lhs.index.CompareTo(rhs.index));
        }

        public IReadOnlyList<KeywordGrantData> GetKeywordGrants(eSourceType sourceType, long sourceRefID)
        {
            return _keywordGrantMap.TryGetValue((sourceType, sourceRefID), out List<KeywordGrantData> grants)
                ? grants
                : Array.Empty<KeywordGrantData>();
        }

        public IReadOnlyList<KeywordEffectData> GetKeywordEffects(long keywordID)
        {
            return _keywordEffectMap.TryGetValue(keywordID, out List<KeywordEffectData> effects)
                ? effects
                : Array.Empty<KeywordEffectData>();
        }

        private void ValidateKeywordDataRelations()
        {
            if (!_cache.TryGetValue(nameof(KeywordData), out Dictionary<long, SheetData> keywordDict))
                return;

            foreach (List<KeywordGrantData> grants in _keywordGrantMap.Values)
            {
                foreach (KeywordGrantData grant in grants)
                {
                    if (!keywordDict.ContainsKey(grant.KeywordID))
                        Debug.LogWarning($"[DataManager] KeywordGrantData {grant.index}의 KeywordID {grant.KeywordID}가 정의되어 있지 않습니다.");
                }
            }

            foreach (List<KeywordEffectData> effects in _keywordEffectMap.Values)
            {
                foreach (KeywordEffectData effect in effects)
                {
                    if (!keywordDict.ContainsKey(effect.KeywordID))
                        Debug.LogWarning($"[DataManager] KeywordEffectData {effect.index}의 KeywordID {effect.KeywordID}가 정의되어 있지 않습니다.");
                }
            }
        }

        
        private void SetCharacterSkillMap()
        {
            const string key = nameof(SkillData);
            if (!_cache.TryGetValue(key, out Dictionary<long, SheetData> skillDict))
                return;

            if (skillDict == null)
            {
                Debug.LogError($"Map not included in parsing : {key}");
                return;
            }
            
            foreach (SheetData skillData in skillDict.Values)
            {
                if (skillData is not SkillData skill) continue;
                if (!characterSkillMap.ContainsKey(skill.characterID))
                {
                    CharacterSkillMap.Add(skill.characterID, new List<SkillData>{ skill });
                }
                else
                {
                    CharacterSkillMap[skill.characterID].Add(skill);
                }
            }
        }
        
        public List<SkillData> GetDefaultSkillSet(long characterID)
        {
            if (characterID <= 0)
            {
                Debug.LogError($"[DataManager] Invalid CharacterID {characterID} : Must be > 0");
                return new List<SkillData>();
            }

            if (!characterSkillMap.TryGetValue(characterID, out List<SkillData> skills))
            {
                Debug.LogError($"[DataManager] Invalid CharacterID : No {characterID} in Data List");
                return new List<SkillData>();
            }

            List<SkillData> defaultSkills = skills.FindAll(x => x.unlockCondition == eSkillUnlock.Default);
            return defaultSkills;
        }
        
    }

}
