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
    public partial class DokkaebiData : SheetData
    {
public long index; // 도깨비 ID
		public string DokkaebiName; // 캐릭터 이름
		public int ObangBlue; // 청
		public int ObangRed; // 적
		public int ObangYellow; // 황
		public int ObangWhite; // 백
		public int ObangBlack; // 흑
		public long PassiveD1; // 첫 번째 패시브
		public long PassiveD2; // 두 번째 패시브
		public string SpriteLDRoute; // Sprite_LD
		public string SpriteIconRoute; // Sprite_TurnIcon
		public string PrefabRoot; // PrefabRoot
		
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

                    DokkaebiData data = new DokkaebiData();

                    
					if(values[0] == "")
					    data.index = default;
					else
					    data.index = Convert.ToInt64(values[0]);
					
					if(values[1] == "")
					    data.DokkaebiName = default;
					else
					    data.DokkaebiName = Convert.ToString(values[1]);
					
					if(values[2] == "")
					    data.ObangBlue = default;
					else
					    data.ObangBlue = Convert.ToInt32(values[2]);
					
					if(values[3] == "")
					    data.ObangRed = default;
					else
					    data.ObangRed = Convert.ToInt32(values[3]);
					
					if(values[4] == "")
					    data.ObangYellow = default;
					else
					    data.ObangYellow = Convert.ToInt32(values[4]);
					
					if(values[5] == "")
					    data.ObangWhite = default;
					else
					    data.ObangWhite = Convert.ToInt32(values[5]);
					
					if(values[6] == "")
					    data.ObangBlack = default;
					else
					    data.ObangBlack = Convert.ToInt32(values[6]);
					
					if(values[7] == "")
					    data.PassiveD1 = default;
					else
					    data.PassiveD1 = Convert.ToInt64(values[7]);
					
					if(values[8] == "")
					    data.PassiveD2 = default;
					else
					    data.PassiveD2 = Convert.ToInt64(values[8]);
					
					if(values[9] == "")
					    data.SpriteLDRoute = default;
					else
					    data.SpriteLDRoute = Convert.ToString(values[9]);
					
					if(values[10] == "")
					    data.SpriteIconRoute = default;
					else
					    data.SpriteIconRoute = Convert.ToString(values[10]);
					
					if(values[11] == "")
					    data.PrefabRoot = default;
					else
					    data.PrefabRoot = Convert.ToString(values[11]);
					

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