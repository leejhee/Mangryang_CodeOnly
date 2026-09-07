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
    public partial class KeywordEffectData : SheetData
    {
public long index; // 키워드 이펙트ID
		public long KeywordID; // 키워드 ID
		
		public SystemEnum.eEffectType EffectType; // 효과 종류
		
		public SystemEnum.eTriggerCondition TriggerCondition; // 발동 조건
		public long ActionID; // 실행할 스킬
		
		public SystemEnum.eStats TargetStat; // 목표 스탯
		
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

                    KeywordEffectData data = new KeywordEffectData();

                    
					if(values[0] == "")
					    data.index = default;
					else
					    data.index = Convert.ToInt64(values[0]);
					
					if(values[1] == "")
					    data.KeywordID = default;
					else
					    data.KeywordID = Convert.ToInt64(values[1]);
					
					if(values[3] == "")
					    data.EffectType = default;
					else
					    data.EffectType = (SystemEnum.eEffectType)Enum.Parse(typeof(SystemEnum.eEffectType), values[3]);
					
					if(values[4] == "")
					    data.TriggerCondition = default;
					else
					    data.TriggerCondition = (SystemEnum.eTriggerCondition)Enum.Parse(typeof(SystemEnum.eTriggerCondition), values[4]);
					
					if(values[5] == "")
					    data.ActionID = default;
					else
					    data.ActionID = Convert.ToInt64(values[5]);
					
					if(values[6] == "")
					    data.TargetStat = default;
					else
					    data.TargetStat = (SystemEnum.eStats)Enum.Parse(typeof(SystemEnum.eStats), values[6]);
					

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