using Core.Scripts.Foundation;
using Core.Scripts.Foundation.Singleton;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Scripts.Managers
{
    public partial class ResourceManager
    {
        public static Sprite LoadImageFromResources(string basePath)
        {
            string[] extensions = { "", ".png", ".jpg", ".jpeg" };
            Texture2D texture = null;

            foreach(string ext in extensions)
            {
                string testPath = basePath + ext;
                texture = Resources.Load<Texture2D>(testPath);

                if (texture != null)
                {
                    Debug.Log("이미지 로드 성공" + testPath);
                    break;
                }
            }

            if (texture == null)
            {
                Debug.LogError($"이미지를 찾을수 없음. {basePath}");
                return null;
            }

            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        public static List<Sprite> LoadAtlasSprites(string basePath)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(basePath);

            if (sprites != null && sprites.Length > 0)
            {
                Debug.Log($"{sprites.Length}개 스프라이트 로드 성공 : {basePath}");
                return new List<Sprite>(sprites);
            }

            Debug.LogError($"아틀라스 스프라이트 찾을수 없음 : {basePath}");
            return new();
        }
        public static T LoadAsset<T>(string path) where T : Object
        {
            T asset = Resources.Load<T>(path);
            if (asset == null)
            {
                Debug.LogWarning($"{path} 경로에서 {typeof(T).Name} 타입의 에셋을 찾을 수 없음");
            }
            return asset;
        }

        public static T[] LoadAllAssets<T>(string path) where T : Object
        {
            T[] assets = Resources.LoadAll<T>(path);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError($"{path} 경로에서 {typeof(T).Name} 타입 에셋 발견 실패");
            }
            return assets;
        }
    }
}