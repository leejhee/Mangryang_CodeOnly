using Core.Scripts.Foundation.Define;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Core.Scripts.Foundation.SceneUtil
{
    public class LoadingScene : MonoBehaviour
    {
        [SerializeField] private Image progressBar;
        [SerializeField] private float fakeLoadingTime;
        [SerializeField] private float loadingBoundary = 0.5f;
        
        private CancellationTokenSource _cts;
        private async void Start()
        {
            var ct = this.GetCancellationTokenOnDestroy();
            
            #region Validating Destination
            string destination = SceneLoader.DestinationScene.ToString();
            if (string.IsNullOrEmpty(destination) || destination == nameof(SystemEnum.eScene.None))
            {
                Debug.LogError("Invalid Destination");
            }
            
            #endregion
            
            #region Scene Loading
            if(progressBar) progressBar.fillAmount = 0; 
            AsyncOperation op = SceneManager.LoadSceneAsync(destination, LoadSceneMode.Additive);

            while (!op.isDone)
            {
                if(progressBar)
                    progressBar.fillAmount = Mathf.Clamp01(op.progress / loadingBoundary) * loadingBoundary;
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            #endregion
            
            
            var destScene = SceneManager.GetSceneByName(destination);
            if (!destScene.IsValid() || !destScene.isLoaded)
            {
                Debug.LogError($"[LoadingScene] : failed to load scene {destination}");
                return;
            }
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(destination));
            
            #region Loading Pipeline - After Scene Loading
            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _cts = linked;
            bool initializationSucceeded = false;
            try
            {
                if (SceneLoader.InitCallbackAsync != null)
                {
                    var progress = new Progress<float>(p => {
                        if(progressBar)
                            progressBar.fillAmount = loadingBoundary + ((1 - loadingBoundary) * Mathf.Clamp01(p));
                    });
                
                    await SceneLoader.InitCallbackAsync(_cts.Token, progress);
                }
                else
                {
                    if (progressBar) progressBar.fillAmount = 1f;
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }

                initializationSucceeded = true;
            }
            catch (OperationCanceledException){}
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
            finally
            {
                SceneLoader.Clear(); 
            }
            
            #endregion

            if (ct.IsCancellationRequested) return;

            if (!initializationSucceeded)
            {
                Debug.LogError($"[LoadingScene] Initialization failed. Unloading destination scene: {destination}");
                SceneManager.SetActiveScene(gameObject.scene);
                AsyncOperation failedSceneUnload = SceneManager.UnloadSceneAsync(destScene);
                if (failedSceneUnload != null)
                {
                    while (!failedSceneUnload.isDone)
                        await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
                return;
            }

            _ = SceneManager.UnloadSceneAsync(gameObject.scene);
            
        }

        private void OnDestroy()
        {
            _cts?.Dispose();
            _cts = null;
        }
    }
}
