using UnityEngine;

#if UNITY_EDITOR
using Core.Scripts.Managers;
using UnityEditor;

namespace AngelBeat
{
    public class DataTool
    {
        [MenuItem("Data/데이터검증")]
        public static async void DataVerification()
        {
            await DataManager.Instance.InitAsync();
            DataManager.Instance.ClearCache();
            Debug.Log("데이터 검증 끝.");
        }

    }
#endif
}
