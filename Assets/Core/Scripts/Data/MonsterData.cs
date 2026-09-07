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
    public partial class MonsterData : SheetData
    {
public long index; // 캐릭터 ID
		public string charName; // 캐릭터 이름(추후 stringcode로 교체할 것)
		public string charPrefabName; // 캐릭터 프리팹 루트
		public string charImage; // 캐릭터 아이콘 루트
		public long charStatID; // 캐릭터 스탯 ID
		public long defaultPassiveListID; // 초기 패시브 리스트
		
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

                    MonsterData data = new MonsterData();

                    
					if(values[0] == "")
					    data.index = default;
					else
					    data.index = Convert.ToInt64(values[0]);
					
					if(values[2] == "")
					    data.charName = default;
					else
					    data.charName = Convert.ToString(values[2]);
					
					if(values[3] == "")
					    data.charPrefabName = default;
					else
					    data.charPrefabName = Convert.ToString(values[3]);
					
					if(values[4] == "")
					    data.charImage = default;
					else
					    data.charImage = Convert.ToString(values[4]);
					
					if(values[5] == "")
					    data.charStatID = default;
					else
					    data.charStatID = Convert.ToInt64(values[5]);
					
					if(values[6] == "")
					    data.defaultPassiveListID = default;
					else
					    data.defaultPassiveListID = Convert.ToInt64(values[6]);
					

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