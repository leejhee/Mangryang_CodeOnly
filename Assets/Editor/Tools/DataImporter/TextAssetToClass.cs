using System.Text;
using System.Text.RegularExpressions;
using System;
using System.IO;
using UnityEngine;

namespace AngelBeat.Tools.DataImporter
{
    public static class TextAssetToClass
    {
        /// <summary> CSV 저장 및 데이터 클래스 스크립트 작성. 타입 이름은 시트에서 가져옴 </summary>
        public static void ParseCSV(string csv, string dataType, bool isSheetData)
        {
            // CSV 테이블화. #에 쓰는 시트 설명에 대한 예외사항 반영
            var rows = Regex.Split(csv, @"\r\n");
            var fieldComments = rows[0].Split(",");
            var fieldNames = rows[1].Split(",");
            var fieldTypes = rows[2].Split(",");

            #region Write Script
            StringBuilder fieldDeclaration = new();
            StringBuilder fieldParsing = new();

            for (int col = 0; col < fieldNames.Length; col++)
            {
                var comment = fieldComments[col];
                var name = fieldNames[col];
                var type = fieldTypes[col];

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type) 
                    || comment[0] == '#' || name[0] == '#' || type[0] == '#') continue;

                var toMemberType = ToMemberType(type.Replace("[]", ""));
                if (toMemberType != string.Empty)
                {
                    if (type.EndsWith("[]"))
                    {
                        // 리스트 타입 처리
                        string elementType = type.Replace("[]", "");
                        fieldDeclaration.Append(string.Format(DataClassFormat.dataRegisterListFormat, elementType, name, comment) + Environment.NewLine);
                        fieldParsing.Append(string.Format(DataClassFormat.dataParseListFormat, col.ToString(), name, ToMemberType(elementType)) + Environment.NewLine);
                    }
                    else
                    {
                        // 기본 자료형
                        fieldDeclaration.Append(string.Format(DataClassFormat.dataRegisterFormat, type, name, comment) + Environment.NewLine);
                        fieldParsing.Append(string.Format(DataClassFormat.dataParseFomat, col.ToString(), name, toMemberType) + Environment.NewLine);
                    }
                }
                else if (type.Contains("Sprite"))
                {
                    // 스프라이트 처리(로드까지 다 함)
                    fieldDeclaration.Append(string.Format(DataClassFormat.dataSpriteRegisterFormat, type, name, comment) + Environment.NewLine);
                    fieldParsing.Append(string.Format(DataClassFormat.dataSpriteParseFormat, col.ToString(), name, dataType) + Environment.NewLine);
                }
                else
                {
                    // Enum 전용
                    fieldDeclaration.Append(string.Format(DataClassFormat.dataEnumRegisterFormat, type, name, comment) + Environment.NewLine);
                    fieldParsing.Append(string.Format(DataClassFormat.dataEnumParseFomat, col.ToString(), name, type) + Environment.NewLine);
                }
            }

            fieldDeclaration = fieldDeclaration.Replace("\n", "\n\t");
            fieldParsing = fieldParsing.Replace("\n", "\n\t\t\t\t");
            //try
            {
                if(isSheetData)
                {
                    var dataScript = string.Format(DataClassFormat.classDataFormat, dataType, fieldDeclaration.ToString(), fieldParsing.ToString());
                    File.WriteAllText($"{Application.dataPath}/Core/Scripts/Data/{dataType}.cs", dataScript);
                    Debug.Log($"코드 작성 완료. {dataType}.cs");

                    File.WriteAllText($"{Application.dataPath}/Gameplay/Common/CommonResources/CSV/MEMCSV/{dataType}.csv", csv);
                    Debug.Log($"CSV 저장 완료. {dataType}.csv");
                }
                else
                {
                    var dataScript = string.Format(DataClassFormat.SODataFormat, dataType, fieldDeclaration.ToString());
                    File.WriteAllText($"{Application.dataPath}/Scripts/ScriptableObj/{dataType}.cs", dataScript);
                    var dataContainerScript = string.Format(DataClassFormat.SOContainerFormat, dataType, fieldDeclaration.ToString(), fieldParsing.ToString());
                    File.WriteAllText($"{Application.dataPath}/Scripts/ScriptableObj/{dataType}List.cs", dataContainerScript);

                    Debug.Log($"코드 작성 완료. {dataType}.cs");
                    File.WriteAllText($"{Application.dataPath}/Resources/CSV/SOCSV/{dataType}.csv", csv);
                    Debug.Log($"CSV 저장 완료. {dataType}.csv");
                }
                
            }
            //catch (FormatException e)
            //{
            //    Debug.Log(e.Message);
            //}


            #endregion
        }

        public static void ParseTSV(string tsv, string dataType,  bool isSheetData)
        {
            var rows = Regex.Split(tsv, @"\r\n");
            var fieldComments = rows[0].Split("\t");
            var fieldNames = rows[1].Split("\t");
            var fieldTypes = rows[2].Split("\t");

            #region Write Script
            StringBuilder fieldDeclaration = new();
            StringBuilder fieldParsing = new();

            for (int col = 0; col < fieldNames.Length; col++)
            {
                var comment = fieldComments[col];
                var name = fieldNames[col];
                var type = fieldTypes[col];

                if (comment[0] == '#' || name[0] == '#' || type[0] == '#') continue;

                var toMemberType = ToMemberType(type.Replace("[]", ""));
                if (toMemberType != string.Empty)
                {
                    if (type.EndsWith("[]"))
                    {
                        // 리스트 타입 처리
                        string elementType = type.Replace("[]", "");
                        fieldDeclaration.Append(string.Format(DataClassFormat.dataRegisterListFormat, elementType, name, comment) + Environment.NewLine);
                        fieldParsing.Append(string.Format(DataClassFormat.dataParseListFormat, col.ToString(), name, ToMemberType(elementType)) + Environment.NewLine);
                    }
                    else
                    {
                        // 기본 자료형
                        fieldDeclaration.Append(string.Format(DataClassFormat.dataRegisterFormat, type, name, comment) + Environment.NewLine);
                        fieldParsing.Append(string.Format(DataClassFormat.dataParseFomat, col.ToString(), name, toMemberType) + Environment.NewLine);
                    }
                }
                else if (type.Contains("Sprite"))
                {
                    // 스프라이트 처리(로드까지 다 함)
                    fieldDeclaration.Append(string.Format(DataClassFormat.dataSpriteRegisterFormat, type, name, comment) + Environment.NewLine);
                    fieldParsing.Append(string.Format(DataClassFormat.dataSpriteParseFormat, col.ToString(), name, dataType) + Environment.NewLine);
                }
                else
                {
                    // Enum 전용
                    fieldDeclaration.Append(string.Format(DataClassFormat.dataEnumRegisterFormat, type, name, comment) + Environment.NewLine);
                    fieldParsing.Append(string.Format(DataClassFormat.dataEnumParseFomat, col.ToString(), name, type) + Environment.NewLine);
                }
            }

            fieldDeclaration = fieldDeclaration.Replace("\n", "\n\t");
            fieldParsing = fieldParsing.Replace("\n", "\n\t\t\t\t");

            if (isSheetData)
            {
                var dataScript = string.Format(DataClassFormat.stringDataFormat, dataType, fieldDeclaration.ToString(), fieldParsing.ToString());
                File.WriteAllText($"{Application.dataPath}/Scripts/SheetData/{dataType}.cs", dataScript);
                Debug.Log($"코드 작성 완료. {dataType}.cs");

                File.WriteAllText($"{Application.dataPath}/Resources/CSV/MEMTSV/{dataType}.csv", tsv);
                Debug.Log($"TSV 저장 완료. {dataType}.csv");
            }

            #endregion

        }



        /// <summary> 원활한 타입 변환 위한 string 변환기 </summary>
        private static string ToMemberType(string memberType)
        {
            switch (memberType)
            {
                case "bool":
                case "Bool":
                    return "ToBoolean";
                // case "byte": // 따로 정의되어있지는 않음
                case "short":
                case "Short":
                    return "ToInt16";
                case "ushort":
                case "Ushort":
                    return "ToUInt16";
                case "int":
                case "Int":
                    return "ToInt32";
                case "uint":
                case "Uint":
                    return "ToUInt32";
                case "long":
                case "Long":
                    return "ToInt64";
                case "ulong":
                case "Ulong":
                    return "ToUInt64";
                case "float":
                case "Float":
                    return "ToSingle";
                case "double":
                case "Double":
                    return "ToDouble";
                case "string":
                case "String":
                    return "ToString";
                default:
                    return string.Empty;
            }
        }
    }
}
