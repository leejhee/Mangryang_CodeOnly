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
    public partial class KeywordData : SheetData
    {
public long index; // 키워드 ID
		public string KeywordName; // 키워드 이름
		
		public SystemEnum.eInfluenceType InfluenceType; // 긍정/부정/중립
		public bool Stackable; // 중첩 가능
		public int MaxStack; // 최대 중첩
		
		public SystemEnum.eRefreshPolicy RefreshPolicy; // Refresh / Stack / Extend
		
		public SystemEnum.eRemovePolicy RemovePolicy; // ByDuration / ByCondition / Permanent
		public long EvolvedKeywordID; // 진화 후 키워드 ID
		public bool iconIsVisible; // 아이콘이 보이는지
		
		public SystemEnum.eKeyword keywordType; // 키워드 enum
		public string keywordIcon; // 키워드 아이콘
		
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

                    KeywordData data = new KeywordData();

                    
					if(values[0] == "")
					    data.index = default;
					else
					    data.index = Convert.ToInt64(values[0]);
					
					if(values[1] == "")
					    data.KeywordName = default;
					else
					    data.KeywordName = Convert.ToString(values[1]);
					
					if(values[3] == "")
					    data.InfluenceType = default;
					else
					    data.InfluenceType = (SystemEnum.eInfluenceType)Enum.Parse(typeof(SystemEnum.eInfluenceType), values[3]);
					
					if(values[4] == "")
					    data.Stackable = default;
					else
					    data.Stackable = Convert.ToBoolean(values[4].ToLowerInvariant());
					
					if(values[5] == "")
					    data.MaxStack = default;
					else
					    data.MaxStack = Convert.ToInt32(values[5]);
					
					if(values[6] == "")
					    data.RefreshPolicy = default;
					else
					    data.RefreshPolicy = (SystemEnum.eRefreshPolicy)Enum.Parse(typeof(SystemEnum.eRefreshPolicy), values[6]);
					
					if(values[7] == "")
					    data.RemovePolicy = default;
					else
					    data.RemovePolicy = (SystemEnum.eRemovePolicy)Enum.Parse(typeof(SystemEnum.eRemovePolicy), values[7]);
					
					if(values[8] == "")
					    data.EvolvedKeywordID = default;
					else
					    data.EvolvedKeywordID = Convert.ToInt64(values[8]);
					
					if(values[9] == "")
					    data.iconIsVisible = default;
					else
					    data.iconIsVisible = Convert.ToBoolean(values[9].ToLowerInvariant());
					
					if(values[10] == "")
					    data.keywordType = default;
					else
					    data.keywordType = (SystemEnum.eKeyword)Enum.Parse(typeof(SystemEnum.eKeyword), values[10]);
					
					if(values[11] == "")
					    data.keywordIcon = default;
					else
					    data.keywordIcon = Convert.ToString(values[11]);
					

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