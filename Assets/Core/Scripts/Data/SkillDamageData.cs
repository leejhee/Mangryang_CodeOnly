using Core.Scripts.Foundation.Define;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Data;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Core.Scripts.Data
{
    public partial class SkillDamageData : SheetData
    {
public long index; // 스킬 데미지ID
		
		public SystemEnum.eSkillType SkillType; // 공격 방식
		public float DamageCoefficient; // 대미지 계수
		public float RandMin; // 난수 MIM 데미지
		public float RandMax; // 난수 MAX 데미지
		
		public SystemEnum.eRound RoundNormal; // 일반 데미지 처리
		
		public SystemEnum.eRound RoundFinal; // 최종 데미지 처리
		public float CritMultiplier; // 치명타계수
		public string ConditionalDamageFormula; // 조건부 데미지식
		
		public SystemEnum.eStats input1; // 0번 스탯
		
		public SystemEnum.eStats input2; // 1번 스탯
		
		public SystemEnum.eStats input3; // 2번 스탯
		
		public SystemEnum.eStats input4; // 3번 스탯
		
        /// <summary>Addressable(RM)로 CSV를 비동기 로드해 파싱함</summary>
        public override UniTask<Dictionary<long, SheetData>> ParseAsync(string csv, CancellationToken ct = default)
        {
            var dataList = new Dictionary<long, SheetData>();
            string ListStr = null;
            int line = 0;

            try
            { 
                string[] lines = csv.Split('\n');

                for (int i = 3; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                        continue;

                    string[] values = CSVParser.Parse(lines[i].Trim());
                    line = i;

                    SkillDamageData data = new SkillDamageData();

                    
					if(values[0] == "")
					    data.index = default;
					else
					    data.index = Convert.ToInt64(values[0]);
					
					if(values[3] == "")
					    data.SkillType = default;
					else
					    data.SkillType = (SystemEnum.eSkillType)Enum.Parse(typeof(SystemEnum.eSkillType), values[3]);
					
					if(values[4] == "")
					    data.DamageCoefficient = default;
					else
					    data.DamageCoefficient = Convert.ToSingle(values[4]);
					
					if(values[5] == "")
					    data.RandMin = default;
					else
					    data.RandMin = Convert.ToSingle(values[5]);
					
					if(values[6] == "")
					    data.RandMax = default;
					else
					    data.RandMax = Convert.ToSingle(values[6]);
					
					if(values[7] == "")
					    data.RoundNormal = default;
					else
					    data.RoundNormal = (SystemEnum.eRound)Enum.Parse(typeof(SystemEnum.eRound), values[7]);
					
					if(values[8] == "")
					    data.RoundFinal = default;
					else
					    data.RoundFinal = (SystemEnum.eRound)Enum.Parse(typeof(SystemEnum.eRound), values[8]);
					
					if(values[9] == "")
					    data.CritMultiplier = default;
					else
					    data.CritMultiplier = Convert.ToSingle(values[9]);
					
					if(values[10] == "")
					    data.ConditionalDamageFormula = default;
					else
					    data.ConditionalDamageFormula = Convert.ToString(values[10]);
					
					if(values[11] == "")
					    data.input1 = default;
					else
					    data.input1 = (SystemEnum.eStats)Enum.Parse(typeof(SystemEnum.eStats), values[11]);
					
					if(values[12] == "")
					    data.input2 = default;
					else
					    data.input2 = (SystemEnum.eStats)Enum.Parse(typeof(SystemEnum.eStats), values[12]);
					
					if(values[13] == "")
					    data.input3 = default;
					else
					    data.input3 = (SystemEnum.eStats)Enum.Parse(typeof(SystemEnum.eStats), values[13]);
					
					if(values[14] == "")
					    data.input4 = default;
					else
					    data.input4 = (SystemEnum.eStats)Enum.Parse(typeof(SystemEnum.eStats), values[14]);
					

                    dataList[data.index] = data;
                }

                return UniTask.FromResult(dataList);
            }
            catch (Exception e)
            {
                Debug.LogError($"{this.GetType().Name}의 {line} 전후로 데이터 문제 발생: {e}");
                return UniTask.FromResult(new Dictionary<long, SheetData>());
            }
        }       
       
    }
}