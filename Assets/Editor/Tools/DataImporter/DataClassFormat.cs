namespace AngelBeat.Tools.DataImporter
{
    public static class DataClassFormat
    {
        // {0} 자료형
        // {1} 변수명
        // {2} 설명
        public static string dataRegisterFormat =
            @"public {0} {1}; // {2}";

        public static string dataRegisterListFormat =
            @"public List<{0}> {1}; // {2}";

        // {0} : row index
        // {1} : 자료형 이름
        // {2} : 자료형 변환
        public static string dataParseFomat =
            @"
if(values[{0}] == """")
    data.{1} = default;
else
    data.{1} = Convert.{2}(values[{0}]);";


        public static string dataParseListFormat =
            @"
ListStr = values[{0}].Replace('[',' ');
ListStr = ListStr.Replace(']', ' ');
var {1}Data = ListStr.ToString().Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).Select(x => Convert.{2}(x)).ToList();
data.{1} = {1}Data;";

        //추후 enum 묶는 클래스 부여 시 활용 예정
        public static string dataEnumRegisterFormat =
            @"public SystemEnum.{0} {1}; // {2}";

        // {2} : Enum 자료형
        public static string dataEnumParseFomat =
            @"
if(values[{0}] == """")
    data.{1} = default;
else
    data.{1} = (SystemEnum.{2})Enum.Parse(typeof(SystemEnum.{2}), values[{0}]);";

        public static string dataSpriteRegisterFormat =
            @"public {0} {1}; // {2}";

        public static string dataSpriteParseFormat =
            @"
if(values[{0}] == """")
    data.{1} = null;
else
    data.{1} = Resources.Load<Sprite>($""Sprites/{2}/{{values[{0}]}}"");";


        // {0} : 클래스 이름
        // {1} : 자료형들
        // {2} : 파싱
        public static string SODataFormat =
            @"using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public partial class {0}
{{
    {1}   
}}";

        public static string SOContainerFormat =
            @"using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Data;
using System.Linq;
using System.IO;

[Serializable]
public class {0}List : ScriptableObject, ITableSO
{{
    [SerializeField]
    private List<{0}> objects = new(); // 필드로 정의
    public List<{0}> Objects => objects; // 타입별 리스트

    public void DataInitialize()
    {{
        objects.Clear();
        TextAsset csvFile = Resources.Load<TextAsset>($""CSV/SOCSV/{{typeof({0})}}"");
        int line = 0;
        string ListStr = null;
        try
        {{
            string csv = csvFile.text;
            string[] rows = csv.Split('\n');
            for (int i = 3; i < rows.Length; i++)
            {{
                if (string.IsNullOrWhiteSpace(rows[i]))
                    continue;

                string[] values = rows[i].Trim().Split(',');
                line = i;

                {0} data = new();
                
                {2}

                Objects.Add(data);
            }}
        }}
        catch (Exception)
        {{
            Debug.LogError($""{{GetType().Name}}의 {{line}}전후로 데이터 문제 발생"");
        }}
    }}
}}";

        public static string classDataFormat =
            @"using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

[Serializable]
public partial class {0} : SheetData
{{
    {1}

    public override Dictionary<long, SheetData> LoadData()
    {{
        var dataList = new Dictionary<long, SheetData>();

        string ListStr = null;
		int line = 0;
        TextAsset csvFile = Resources.Load<TextAsset>($""CSV/MEMCSV/{{this.GetType().Name}}"");
        try
		{{            
            string csvContent = csvFile.text;
            string[] lines = Regex.Split(csvContent, @""\r?\n"");

            for (int i = 3; i < lines.Length; i++)
            {{
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                string[] values = CSVParser.Parse(lines[i].Trim());


                line = i;

                {0} data = new {0}();

                {2}

                dataList[data.index] = data;
            }}

            return dataList;
        }}
		catch (Exception e)
		{{
			Debug.LogError($""{{this.GetType().Name}}의 {{line}}전후로 데이터 문제 발생"");
			return new Dictionary<long, SheetData>();
		}}
    }}
}}";

        public static string stringDataFormat =
            @"using Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace Client
{{
    [Serializable]
    public partial class {0} : SheetData
    {{
{1}

        public override Dictionary<long, SheetData> LoadData()
        {{
            var dataList = new Dictionary<long, SheetData>();

            string ListStr = null;
			int line = 0;
            TextAsset csvFile = Resources.Load<TextAsset>($""CSV/MEMTSV/{{this.GetType().Name}}"");
            try
			{{            
                string csvContent = csvFile.text;
                var lines = Regex.Split(csvContent, @""(?<!""""[^""""]*)\r?\n"");
                for (int i = 3; i < lines.Length; i++)
                {{
                    if (string.IsNullOrWhiteSpace(lines[i]))
                        continue;

                    string[] values = lines[i].Trim().Split('\t');
                    line = i;

                    {0} data = new {0}();

                    {2}

                    dataList[data.index] = data;
                }}

                return dataList;
            }}
			catch (Exception e)
			{{
				Debug.LogError($""{{this.GetType().Name}}의 {{line}}전후로 데이터 문제 발생"");
				return new Dictionary<long, SheetData>();
			}}
        }}
    }}
}}"; 
    }


}
 