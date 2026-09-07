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
    public partial class CharStatData : SheetData
    {
public long index; // 캐릭터 스탯 ID
		public int Defense; // 방어력
		public int MagicResist; // 항마력
		public int AilmentResist; // 상태이상저항
		public int PhysicalAttack; // 물리공격력
		public int CriticalRate; // 치명타율
		public int MagicAttack; // 마법공격력
		public int Movement; // 이동력
		public int Accuracy; // 명중률
		public int AilmentInflict; // 상태이상가산치
		public int Evasion; // 회피율
		public int Speed; // 행동속도
		public int HealthPoint; // 체력
		public int MoveResist; // 변위 저항
		
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

                    CharStatData data = new CharStatData();

                    
					if(values[0] == "")
					    data.index = default;
					else
					    data.index = Convert.ToInt64(values[0]);
					
					if(values[1] == "")
					    data.Defense = default;
					else
					    data.Defense = Convert.ToInt32(values[1]);
					
					if(values[2] == "")
					    data.MagicResist = default;
					else
					    data.MagicResist = Convert.ToInt32(values[2]);
					
					if(values[3] == "")
					    data.AilmentResist = default;
					else
					    data.AilmentResist = Convert.ToInt32(values[3]);
					
					if(values[4] == "")
					    data.PhysicalAttack = default;
					else
					    data.PhysicalAttack = Convert.ToInt32(values[4]);
					
					if(values[5] == "")
					    data.CriticalRate = default;
					else
					    data.CriticalRate = Convert.ToInt32(values[5]);
					
					if(values[6] == "")
					    data.MagicAttack = default;
					else
					    data.MagicAttack = Convert.ToInt32(values[6]);
					
					if(values[7] == "")
					    data.Movement = default;
					else
					    data.Movement = Convert.ToInt32(values[7]);
					
					if(values[8] == "")
					    data.Accuracy = default;
					else
					    data.Accuracy = Convert.ToInt32(values[8]);
					
					if(values[9] == "")
					    data.AilmentInflict = default;
					else
					    data.AilmentInflict = Convert.ToInt32(values[9]);
					
					if(values[10] == "")
					    data.Evasion = default;
					else
					    data.Evasion = Convert.ToInt32(values[10]);
					
					if(values[11] == "")
					    data.Speed = default;
					else
					    data.Speed = Convert.ToInt32(values[11]);
					
					if(values[12] == "")
					    data.HealthPoint = default;
					else
					    data.HealthPoint = Convert.ToInt32(values[12]);
					
					if(values[13] == "")
					    data.MoveResist = default;
					else
					    data.MoveResist = Convert.ToInt32(values[13]);
					

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