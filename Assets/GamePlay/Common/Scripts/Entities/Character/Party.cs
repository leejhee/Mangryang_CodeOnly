using Core.Scripts.Data;
using Core.Scripts.Foundation.Define;
using Core.Scripts.Managers;
using GamePlay.Common.Scripts.Entities.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GamePlay.Common.Scripts.Entities.Character
{
    [Serializable]
    public class Party
    {
        [Header("어느 타입의 파티인가요?")]
        public SystemEnum.eCharType partyType;

        [Header("파티 멤버들을 기록합니다.")]
        public List<CharacterModel> partyMembers;

        [Header("해당 파티 전원에 적용되는 효과를 기록합니다.")]
        public List<long> FunctionsPerParty;
        
        [Header("파티가 보유한 돈")]
        public int money;

        [Header("파티가 보유한 열쇠 부적")] 
        public int talisman;

        [Header("파티가 보유한 부활 부적")]
        public int revivalTalisman;
        
        
        /// <summary>
        /// 슬롯 생성 후 처음 파티 생성 시 호출
        /// </summary>
        public Party()
        {
            partyType = SystemEnum.eCharType.Player;
            partyMembers = new List<CharacterModel>();
            FunctionsPerParty = new List<long>();
        }
        
        public Party(
            List<CharacterModel> partyMembers,
            List<long> FunctionsPerParty=null,
            SystemEnum.eCharType partyType = SystemEnum.eCharType.Player
            )
        {
            this.partyMembers = partyMembers;
            this.FunctionsPerParty = FunctionsPerParty;
            this.partyType = partyType;
        }
        
        #region Party Members Management
        /// <summary>
        /// 파티가 생성되는 최초 시점에만 적용
        /// </summary>
        public void InitParty()
        {
            DokkaebiData dok = DataManager.Instance.GetData<DokkaebiData>(SystemConst.DokkaebiID);
            CharacterModel dokModel = new(dok);
            partyMembers.Add(dokModel);
        }
        
        public void AddMember(CharacterModel member)
        {
            partyMembers.Add(member);
        }
        
        //업데이트에 안쓰니까 마음놓고 쓰도록 하자.
        public CharacterModel SearchCharacter(string charName)
        {
            return partyMembers.Find(x => x.Name == charName);
        }

        public CharacterModel SearchCharacter(long charIndex)
        {
            return partyMembers.Find(x => x.Index == charIndex);
        }

        public CharacterModel YeonModel => SearchCharacter("연");

        
        // 캐릭터 모델에 스킬을 추가 
        // TODO : 보상 테이블 시 스킬 / 재화 / 아이템 등으로 type 지정
        // TODO : 캐릭터 풀에 맞는 스킬을 데이터 풀에서 놓고 제공하도록 보장할 것.
        public bool AddSkillInCharacter(long skillIndex)
        {
            // 데이터 찾아 삼만리
            SheetData data = DataManager.Instance.GetData<DokkaebiSkillData>(skillIndex);
            SkillModel model;
            CharacterModel receiver;
            if (data == null)
            {
                // 그냥 스킬이었다.
                data = DataManager.Instance.GetData<SkillData>(skillIndex);
                if (data == null)
                {
                    Debug.LogError("Invalid Skill ");
                    return false;
                }
                // 데이터로 모델 만들고 수령자 지정
                SkillData skillData = data as SkillData;
                model = new SkillModel(skillData);
                receiver = SearchCharacter(model.SkillOwnerID);
            }
            else
            {
                // 데이터로 모델 만들고 수령자 지정
                DokkaebiSkillData skillData = data as DokkaebiSkillData;
                model = new SkillModel(skillData);
                receiver = YeonModel;
            }
            receiver.AddSkill(model);
            return true;
        }
        
        
        #endregion
        
        #region Debug
        public override string ToString()
        {
            string func = FunctionsPerParty == null || FunctionsPerParty.Count == 0
                ? "없음" : $"{FunctionsPerParty.Count}개 버프 있음";
            return new StringBuilder($"{partyType} : {partyMembers.Count}명 | 버프 : ").Append(func).ToString();
        }
        
        #endregion
        
        
    }
}