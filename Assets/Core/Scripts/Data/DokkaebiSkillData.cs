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
    public partial class DokkaebiSkillData : SheetData
    {
public long index; // 도깨비 스킬 ID
		public string skillName; // 스킬 이름
		
		public SystemEnum.eObang ObangTag; // 오방색
		public float Weight; // 가중치
		
		public SystemEnum.eSkillType skillType; // 스킬 종류
		public long skillDamage; // 스킬 데미지
		public long skillRange; // 스킬 범위
		public int skillCritical; // 치명타 배율
		public int skillAccuracy; // 명중율
		public string skillIconImage; // 스킬 아이콘명
		public string skillTimeLine; // 스킬 타임라인명
		public string SkillToolTip; // 스킬 툴팁
		
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

                    DokkaebiSkillData data = new DokkaebiSkillData();

                    
					if(values[0] == "")
					    data.index = default;
					else
					    data.index = Convert.ToInt64(values[0]);
					
					if(values[1] == "")
					    data.skillName = default;
					else
					    data.skillName = Convert.ToString(values[1]);
					
					if(values[3] == "")
					    data.ObangTag = default;
					else
					    data.ObangTag = (SystemEnum.eObang)Enum.Parse(typeof(SystemEnum.eObang), values[3]);
					
					if(values[4] == "")
					    data.Weight = default;
					else
					    data.Weight = Convert.ToSingle(values[4]);
					
					if(values[5] == "")
					    data.skillType = default;
					else
					    data.skillType = (SystemEnum.eSkillType)Enum.Parse(typeof(SystemEnum.eSkillType), values[5]);
					
					if(values[6] == "")
					    data.skillDamage = default;
					else
					    data.skillDamage = Convert.ToInt64(values[6]);
					
					if(values[7] == "")
					    data.skillRange = default;
					else
					    data.skillRange = Convert.ToInt64(values[7]);
					
					if(values[8] == "")
					    data.skillCritical = default;
					else
					    data.skillCritical = Convert.ToInt32(values[8]);
					
					if(values[9] == "")
					    data.skillAccuracy = default;
					else
					    data.skillAccuracy = Convert.ToInt32(values[9]);
					
					if(values[10] == "")
					    data.skillIconImage = default;
					else
					    data.skillIconImage = Convert.ToString(values[10]);
					
					if(values[11] == "")
					    data.skillTimeLine = default;
					else
					    data.skillTimeLine = Convert.ToString(values[11]);
					
					if(values[12] == "")
					    data.SkillToolTip = default;
					else
					    data.SkillToolTip = Convert.ToString(values[12]);
					

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