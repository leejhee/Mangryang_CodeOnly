using Core.Scripts.Foundation.Define;
using Core.Scripts.Foundation.SceneUtil;
using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.Scripts.Boot
{
    /// <summary>
    /// 앱 시작 시 최초로 부트해주는 역할.
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private SystemEnum.eScene nextScene = SystemEnum.eScene.LobbyScene;
        
        [SerializeField] private GameObject uiManager;
        [SerializeField] private GameObject inputManager;
        [SerializeField] private GameObject transitionManager;
        [SerializeField] private GameObject mainCamera;
        [SerializeField] private GameObject eventSystem;
        
        protected async void Start()
        {
            try
            {
                GameManager.Instance.GameState = SystemEnum.GameState.Loading;
                await GameReady.InitializeOnceAsync(); // 전체 매니저 초기화
                
                GameObject ui = Instantiate(uiManager);
                GameObject input = Instantiate(inputManager);
                GameObject transition = Instantiate(transitionManager);
                GameObject cam = Instantiate(mainCamera);
                GameObject es = Instantiate(eventSystem);
                
                DontDestroyOnLoad(ui);
                DontDestroyOnLoad(input);
                DontDestroyOnLoad(transition);
                DontDestroyOnLoad(cam);
                DontDestroyOnLoad(es);
                
                SceneLoader.LoadSceneWithLoading(nextScene); // nextScene 로드
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogException(e);
            } 
        }
        
        
    }
}