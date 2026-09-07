using Core.Scripts.Data;
using Core.Scripts.Foundation.Define;
using Core.Scripts.Managers;
using GamePlay.Common.Scripts.Entities.Character.Components;
using GamePlay.Common.Scripts.Entities.Skills;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GamePlay.Common.Scripts.Entities.Character
{
    /// <summary> 파티에는 이 정보가 저장됩니다. </summary>
    [Serializable]
    public class CharacterModel
    {
        [SerializeField] private long index;
        #region Fields
        private string _characterName;
        private CharStat _baseStat;
        private SystemEnum.eCharType _type;
        
        // 해금 목록이 따로 드러나야 한다는 전제로 3단으로 구성
        
        /// <summary>
        /// 이 캐릭터가 가질 수 있는 모든 스킬의 리스트
        /// </summary>
        private List<SkillModel> _allSkillModels = new();
        
        /// <summary>
        /// 현재 활성화되어있는 스킬들
        /// TODO : 스킬 연결 앞으로 여기에 할 것.
        /// </summary>
        private List<SkillModel> _activeSkillModels = new();
        
        /// <summary>
        /// 현재 활성화가 되어 있는 스킬들 중 사용하는 스킬
        /// </summary>
        private List<SkillModel> _usingSkillModels = new();
        
        #region Asset Key Route
        private string _prefabRoot;
        private string _iconSpriteRoot;
        private string _ldRoot;
        #endregion
        #endregion
        
        #region Properties
        public long Index => index;
        public CharStat BaseStat => _baseStat;
        public string Name => _characterName;
        public IReadOnlyList<SkillModel> AllSkills => _allSkillModels.AsReadOnly();
        public IReadOnlyList<SkillModel> ActiveSkills => _activeSkillModels.AsReadOnly();
        public IReadOnlyList<SkillModel> UsingSkills => _usingSkillModels.AsReadOnly();
        public SystemEnum.eCharType CharacterType => _type;

        public string PrefabRoot => _prefabRoot;
        public string IconSpriteRoot => _iconSpriteRoot;
        public string LdRoot => _ldRoot;
        #endregion
        
        #region CTOR
        // 탐사 중 영입 시 등록
        public CharacterModel(CompanionData companion, bool includeSkills = true)
        {
            index = companion.index;
            _type = SystemEnum.eCharType.Player;
            _characterName = companion.charName;
            _prefabRoot = companion.charPrefabName;
            _iconSpriteRoot = companion.charImage;
            _ldRoot = companion.charLDRoute;

            long statIndex = companion.charStatID;
            CharStatData statData = DataManager.Instance.GetData<CharStatData>(statIndex);
            if (statData == null)
            {
                Debug.LogError($"[CharacterModel] : Invalid Stat Data - Check your Index : {statIndex}");
                return;
            }
            _baseStat = new CharStat(statData);

            if (!includeSkills) return;

            List<SkillData> sl = DataManager.Instance.CharacterSkillMap[index];
            foreach(SkillData skill in sl)
                _allSkillModels.Add(new SkillModel(skill));
            _activeSkillModels = _allSkillModels.FindAll(x => x.Unlock == SystemEnum.eSkillUnlock.Default);

            foreach (SkillModel activeSkill in _activeSkillModels)
            {
                if (_usingSkillModels.Count >= 4) break;
                _usingSkillModels.Add(activeSkill);
            }
        }
        
        public CharacterModel(MonsterData monster, bool includeSkills = true)
        {
            index = monster.index;
            _type = SystemEnum.eCharType.Enemy;
            _characterName = monster.charName;
            _prefabRoot = monster.charPrefabName;
            _iconSpriteRoot = monster.charImage;
            _ldRoot = null;

            long statIndex = monster.charStatID;
            CharStatData statData = DataManager.Instance.GetData<CharStatData>(statIndex);
            if (statData == null)
            {
                Debug.LogError($"[CharacterModel] : Invalid Stat Data - Check your Index : {statIndex}");
                return;
            }
            _baseStat = new CharStat(statData);

            if (!includeSkills) return;
            
            // 몬스터는 데이터 상의 스킬 사용에 제한이 없다고 가정. 또한 그 수에도 제한을 두지 않는다 가정
            var skills = DataManager.Instance.CharacterSkillMap[index];
            foreach(SkillData sk in skills)
                _allSkillModels.Add(new SkillModel(sk));
            _activeSkillModels = new List<SkillModel>(_allSkillModels);
            _usingSkillModels =  new List<SkillModel>(_activeSkillModels);
        }
        
        /// <summary>
        /// 파티 초기화 시 필요.
        /// </summary>
        public CharacterModel(DokkaebiData dok)
        {
            index = dok.index;
            _type = SystemEnum.eCharType.Player;
            _characterName = dok.DokkaebiName;
            _ldRoot = dok.SpriteLDRoute;
            _iconSpriteRoot = dok.SpriteIconRoute;
            _prefabRoot = dok.PrefabRoot;
            
            _baseStat = new CharStat(dok);
            
            //도깨비는 처음부터 스킬을 얻지 않는다는 전제
            ////////////// SKILL TEST SECTION ////////////////////


            // for (int i = 0; i < 5; i++)
            // {
            //     var skillData = DataManager.Instance.GetData<DokkaebiSkillData>(10101001 + i);
            //     var skillModel = new SkillModel(skillData);
            //     _allSkillModels.Add(skillModel);
            // }
            //
            // //
            //  _activeSkillModels = new List<SkillModel>(_allSkillModels);
            
            //_usingSkillModels = new List<SkillModel>(skillModels);
            ////////////// SKILL TEST SECTION //////////////////// 
            
            
        }
        
        // 기존의 데이터로 등록
        public CharacterModel(CharacterModel model)
        {
            index = model.Index;
            _type = model.CharacterType;
            _characterName = model.Name;
            _ldRoot = model.LdRoot;
            _iconSpriteRoot = model.IconSpriteRoot;
            _allSkillModels = model._allSkillModels;
            _baseStat = model.BaseStat;
        }
        
        #endregion
        
        /// <summary>
        /// 도깨비의 스킬 선택에는 이 메서드를 사용할 것
        /// </summary>
        /// <param name="skillModel"></param>
        public void AddSkill(SkillModel skillModel)
        {
            if (_activeSkillModels.Contains(skillModel) || _usingSkillModels.Contains(skillModel))
            {
                Debug.LogWarning("이미 있는 스킬 데이터입니다.");
                return;
            }
            
            _activeSkillModels.Add(skillModel);
            if (_usingSkillModels.Count < 4)
            {
                _usingSkillModels.Add(skillModel);
            }
        }

        public bool UseSkill(SkillModel skillModel)
        {
            bool useSKillSuccess = false;

            // 사용중인 스킬이 4개 이상이면 사용 불가
            if (_usingSkillModels.Count >= 4)
            {
                Debug.Log("스킬 4개 선택 완료");
            }
            else
            {
                // 이미 사용중인 스킬이면 선택x
                if (_usingSkillModels.Contains(skillModel))
                {
                    Debug.Log("이미 사용중인 스킬입니다.");
                }
                else
                {
                    // 스킬이 4개 미만으로 선택되어 있고 사용중인 스킬이 아니라면 선택
                    _usingSkillModels.Add(skillModel);
                    useSKillSuccess = true;
                }
            }


            return useSKillSuccess;
        }

        public void NotUseSkill(SkillModel skillModel)
        {
            if (_usingSkillModels.Contains(skillModel))
            {
                _usingSkillModels.Remove(skillModel);
            }
            else
            {
                Debug.Log("이미 사용 해제한 스킬");
            }
        }
        
        /// <summary>
        /// 동료의 스킬 해금에는 이 메서드를 사용할 것
        /// </summary>
        /// <param name="unlock"></param>
        public void UnlockSkill(SystemEnum.eSkillUnlock unlock)
        {
            List<SkillModel> skillModels = _allSkillModels.FindAll(x => x.Unlock == unlock);
            foreach (SkillModel skillModel in skillModels)
            {
                if (_activeSkillModels.Contains(skillModel))
                {
                    Debug.LogWarning($"{skillModel}은 이미 존재하는 스킬입니다. 여기 오면 안되는데?");
                    continue;
                }
                _activeSkillModels.Add(skillModel);
                if (_usingSkillModels.Count < 4) _usingSkillModels.Add(skillModel);
                //skillModel.locked = false; 필요한가?
            }
        }
        
    }
    
}
