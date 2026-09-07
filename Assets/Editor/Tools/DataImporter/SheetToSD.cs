using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace AngelBeat.Tools.DataImporter
{
    public class SheetToSD : EditorWindow
    {
        [MenuItem("Tools/Import Sheet Data")]
        public static void ShowWindow()
        {
            GetWindow<SheetToSD>("Import Sheet Data from Google Sheets");
        }

        private const string ADDRESS = "https://docs.google.com/spreadsheets/d/";
        private string spreadsheetId;
        private string gid;

        private Dictionary<string, string> typeBringer;
        private int idx = 0;
        private string[] dataTypes;

        private bool initialized = false;

        private void OnEnable()
        {
            #region Init Dictionary of SheetData Type with GID Pair
            typeBringer = new Dictionary<string, string>();
            string filePath = Application.dataPath + "/GamePlay/Common/CommonResources/CSV/MEMCSV/ImportDescription.txt";
            if (File.Exists(filePath))
            {
                string description = File.ReadAllText(filePath);
                var pair = description.Split(' ');
                spreadsheetId = pair[0]; gid = pair[1];
                EditorCoroutineUtility.StartCoroutine(ParseSheet(true), this);
            }
            #endregion
        }

        private void OnGUI()
        {
            if (initialized)
            {
                EditorGUILayout.BeginVertical();
                #region Get Input
                GUILayout.Label("Google Sheets Importer(Only for SheetData!)", EditorStyles.boldLabel);
                idx = EditorGUILayout.Popup("Select Type", idx, dataTypes);
                #endregion
                #region Buttons below
                if (GUILayout.Button("Import"))
                {
                    gid = typeBringer[dataTypes[idx]];
                    EditorCoroutineUtility.StartCoroutine(ParseSheet(), this);
                }

                #endregion
                EditorGUILayout.EndVertical();
            }
        }

        private IEnumerator ParseSheet(bool init = false)
        {
            string url = ADDRESS + spreadsheetId + $"/export?format=csv&gid={gid}";
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Error: " + www.error);
                }
                else
                {
                    string importedCSV = www.downloadHandler.text;
                    Debug.Log("Data retrieved: \n" + importedCSV);
                    if (!init)
                    {
                        string selectedType = dataTypes[idx];
                        TextAssetToClass.ParseCSV(importedCSV, selectedType, true);
                    }
                    else // 가져올 타입 목록 초기화
                    {
                        InitSheetAddresses(importedCSV);
                    }
                }
            }
        }

        /// <summary> 드롭다운 리스트업 위한 시트 id 초기화 </summary>
        private void InitSheetAddresses(string idTable)
        {
            var rows = idTable.Split("\r\n");
            foreach (var row in rows)
            {
                var elements = row.Split(',');
                typeBringer.Add(elements[0], elements[1]);
            }
            dataTypes = typeBringer.Keys.ToArray();
            initialized = true;
        }
    }
}
