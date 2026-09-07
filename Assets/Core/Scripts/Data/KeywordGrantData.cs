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
    public partial class KeywordGrantData : SheetData
    {
public long index; // 키워드 부여 ID
		
		public SystemEnum.eSourceType SourceType; // 부여 주체
		public long SouceRefID; // 참조 ID
		public long KeywordID; // 부여 키워드
		
		public SystemEnum.eApplyTarget ApplyTarget; // 부여 대상
		public int Duration; // 턴 수
		public int InitStack; // 시작 스택
		public int Probability; // 확률
		
		public SystemEnum.eStackBehavior StackBehavior; // 합산/갱신/무
		
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

                    KeywordGrantData data = new KeywordGrantData();

                    
					if(values[0] == "")
					    data.index = default;
					else
					    data.index = Convert.ToInt64(values[0]);
					
					if(values[2] == "")
					    data.SourceType = default;
					else
					    data.SourceType = (SystemEnum.eSourceType)Enum.Parse(typeof(SystemEnum.eSourceType), values[2]);
					
					if(values[3] == "")
					    data.SouceRefID = default;
					else
					    data.SouceRefID = Convert.ToInt64(values[3]);
					
					if(values[4] == "")
					    data.KeywordID = default;
					else
					    data.KeywordID = Convert.ToInt64(values[4]);
					
					if(values[5] == "")
					    data.ApplyTarget = default;
					else
					    data.ApplyTarget = (SystemEnum.eApplyTarget)Enum.Parse(typeof(SystemEnum.eApplyTarget), values[5]);
					
					if(values[6] == "")
					    data.Duration = default;
					else
					    data.Duration = Convert.ToInt32(values[6]);
					
					if(values[7] == "")
					    data.InitStack = default;
					else
					    data.InitStack = Convert.ToInt32(values[7]);
					
					if(values[8] == "")
					    data.Probability = default;
					else
					    data.Probability = Convert.ToInt32(values[8]);
					
					if(values[9] == "")
					    data.StackBehavior = default;
					else
					    data.StackBehavior = (SystemEnum.eStackBehavior)Enum.Parse(typeof(SystemEnum.eStackBehavior), values[9]);
					

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