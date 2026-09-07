using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine.Networking;
using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;

namespace AngelBeat.Tools.DataImporter
{
    /// <summary>
    /// Creating ScriptableObject of MapNodeData from Sheet 
    /// </summary>
    public class SheetToSO : EditorWindow
    {
        [MenuItem("Tools/Import Scriptable Object Data")]
        public static void ShowWindow()
        {
            GetWindow<SheetToSO>("Import SO data from Google Sheets");
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
            #region Init Dictionary of SO Type with GID Pair
            typeBringer = new Dictionary<string, string>();
            string filePath = Application.dataPath + "/Resources/CSV/SOCSV/ImportDescription.txt";
            if (File.Exists(filePath)) 
            {
                string description = File.ReadAllText(filePath);
                var pair = description.Split(' ');
                spreadsheetId = pair[0]; gid = pair[1];
                EditorCoroutineUtility.StartCoroutine(ParseSheet(true), this);
            }
            #endregion
        }

        /// <summary>
        /// GUI 설정하는 부분
        /// </summary>
        private void OnGUI()
        {
            if(initialized)
            {
                EditorGUILayout.BeginVertical();
                #region Get Input
                GUILayout.Label("Google Sheets Importer(Only for Scriptable Objects!)", EditorStyles.boldLabel);
                idx = EditorGUILayout.Popup("Select Type", idx, dataTypes);
                #endregion
                #region Buttons below
                if (GUILayout.Button("Import Data for SO"))
                {               
                    gid = typeBringer[dataTypes[idx]];
                    EditorCoroutineUtility.StartCoroutine(ParseSheet(), this);
                }

                if (GUILayout.Button("Create Empty SO to Asset"))
                {
                    CreateEmptySO(dataTypes[idx]);
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
                        TextAssetToClass.ParseCSV(importedCSV, selectedType, false);
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

        private void CreateEmptySO(string dataType)
        {
            Type type = Type.GetType($"{dataType}, Assembly-CSharp");
            Type containerType = Type.GetType($"{dataType}List, Assembly-CSharp");
            if (type == null || containerType == null)
            {
                Debug.LogError("먼저 들여오고 누르셨나요??");
                return;
            }
          
            var objContainer = CreateInstance(containerType.Name);
            AssetDatabase.CreateAsset(objContainer, 
                $"Assets/Resources/ScriptableObjects/{dataType}/{dataType}List.asset");
            EditorUtility.SetDirty(objContainer);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }



}

