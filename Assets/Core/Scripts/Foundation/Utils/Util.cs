using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Core.Scripts.Foundation.Utils
{
    public class Util
    {
        public static T GetOrAddComponent<T>(GameObject go) where T : UnityEngine.Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
            }
            return component;
        }
        public static T FindChild<T>(GameObject go, string name = null, bool recursive = false) where T : UnityEngine.Object
        {
            if (go == null)
                return null;

            if (recursive == false)
            {
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    Transform transform = go.transform.GetChild(i);
                    if (string.IsNullOrEmpty(name) || transform.name == name)
                    {
                        T component = transform.GetComponent<T>();
                        if (component != null)
                            return component;
                    }
                }
            }
            else
            {
                foreach (T component in go.GetComponentsInChildren<T>())
                {
                    if (string.IsNullOrEmpty(name) || component.name == name)
                        return component;
                }
            }

            return null;
        }
        public static GameObject FindChild(GameObject go, string name = null, bool recursive = false)
        {
            Transform transform = FindChild<Transform>(go, name, recursive);
            if (transform == null)
                return null;

            return transform.gameObject;
        }

        public static void SaveJson<DataClass>(DataClass dataClass, string fileName = null) where DataClass : class
        {
            string jsonText = null;

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = typeof(DataClass).Name;

                int index = fileName.IndexOf("DataClass");
                if(index != -1)
                {
                    fileName = string.Concat(fileName.Substring(0, index), 's');
                }
            }

            string savePath;
            string appender = "/userdata";
            string nameString = $"/{fileName}.json";

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE
            savePath = Application.dataPath;
#elif UNITY_ANDROID
        savePath = Application.persistentDataPath;
#endif

            StringBuilder stringBuilder = new StringBuilder(savePath);
            stringBuilder.Append(appender);
            if (!Directory.Exists(stringBuilder.ToString()))
            {
                Debug.Log("No such directory");
                Directory.CreateDirectory(stringBuilder.ToString());
            }
            stringBuilder.Append(nameString);

            jsonText = JsonUtility.ToJson(dataClass, true);
            Debug.Log(jsonText);

            using(FileStream fileStream = new FileStream(stringBuilder.ToString(), FileMode.Create))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(jsonText);
                fileStream.Write(bytes, 0, bytes.Length);
                fileStream.Close();
            }
        }
        
        [Obsolete("영속 데이터 관리용 비동기 메소드(SlotIO)를 사용하세요.")]
        public static DataClass LoadSaveData<DataClass>(string fileName = null) where DataClass : class
        {
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = typeof(DataClass).Name;

                int index = fileName.IndexOf("DataClass");
                if (index != -1)
                {
                    fileName = string.Concat(fileName.Substring(0, index), 's');
                }
            }

            DataClass gameData;
            string loadPath;
            string appender = "/userdata";
            string nameString = $"/{fileName}.json";
#if UNITY_ANDROID
        loadPath = Application.persistentDataPath;
#endif        
            loadPath = Application.dataPath;

            StringBuilder stringBuilder = new StringBuilder(loadPath);
            stringBuilder.Append(appender);
            if (!Directory.Exists(stringBuilder.ToString()))
            {
                Debug.Log("No such directory. Returns nothing");
                Directory.CreateDirectory(stringBuilder.ToString());
                return default(DataClass);
            }
            stringBuilder.Append(nameString);

            if (File.Exists(stringBuilder.ToString()))
            {
                using(FileStream filestream = new FileStream(stringBuilder.ToString(), FileMode.Open))
                {
                    byte[] bytes = new byte[filestream.Length];
                    filestream.Read(bytes, 0, bytes.Length);
                    filestream.Close();
                    string jsonData = Encoding.UTF8.GetString(bytes);
                    gameData = JsonUtility.FromJson<DataClass>(jsonData);
                }
            }
            else
            {
                gameData = default(DataClass);
            }
            return gameData;
        }
        //[Obsolete("영속 데이터 관리용 비동기 메소드(SlotIO)를 사용하세요.")]
        public static void SaveJsonNewtonsoft<TSave>(TSave dataClass, string fileName = null) where TSave : class
        {
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = typeof(TSave).Name;
                int index = fileName.IndexOf("DataClass", StringComparison.Ordinal);
                if (index != -1)
                {
                    fileName = string.Concat(fileName.Substring(0, index), 's');
                }
            }

            string savePath = GetSavePath();
            string fullPath = Path.Combine(savePath, "userdata", $"{fileName}.json");

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? string.Empty);

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                TypeNameHandling = TypeNameHandling.Auto
            };

            string jsonText = JsonConvert.SerializeObject(dataClass, settings);
            File.WriteAllText(fullPath, jsonText, Encoding.UTF8);
        
            Debug.Log($"Saved: {fullPath}");
        }
        
        //[Obsolete("영속 데이터 관리용 비동기 메소드(SlotIO)를 사용하세요.")]
        public static TSave LoadSaveDataNewtonsoft<TSave>(string fileName = null) where TSave : class
        {
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = typeof(TSave).Name;
                int index = fileName.IndexOf("DataClass", StringComparison.Ordinal);
                if (index != -1)
                {
                    fileName = string.Concat(fileName.Substring(0, index), 's');
                }
            }

            string loadPath = GetSavePath();
            string fullPath = Path.Combine(loadPath, "userdata", $"{fileName}.json");

            if (!File.Exists(fullPath))
            {
                Debug.Log($"No save file found: {fullPath}");
                return null;
            }

            try
            {
                string jsonData = File.ReadAllText(fullPath, Encoding.UTF8);
            
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                };
            
                return JsonConvert.DeserializeObject<TSave>(jsonData, settings);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load save data: {e.Message}");
                return null;
            }
        }

        private static string GetSavePath()
        {
           return Application.persistentDataPath;
        }

    }
}
