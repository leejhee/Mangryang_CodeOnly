using UnityEngine;
#if HAS_URP
using UnityEngine.Rendering.Universal;
#endif

namespace Core.Scripts.Foundation.Utils
{
    public static class CameraUtil
    {
#if HAS_URP
        public static bool SetRenderType(Camera cam, CameraRenderType type)
        {
            if (!TryGetURPData(cam, out var data)) return false;
            data.renderType = type;

            if (type == CameraRenderType.Overlay && data.cameraStack != null && data.cameraStack.Count > 0)
                data.cameraStack.Clear();

            return true;
        }

        public static bool SetRenderer(Camera cam, int rendererIndex)
        {
            if (!TryGetURPData(cam, out var data)) return false;
            data.SetRenderer(rendererIndex);
            return true;
        }

        public static bool TryStackOverlay(Camera baseCam, Camera overlayCam)
        {
            if (!TryGetURPData(baseCam, out UniversalAdditionalCameraData baseData)) return false;
            if (!TryGetURPData(overlayCam, out UniversalAdditionalCameraData overlayData)) return false;
            if (baseCam == overlayCam) return false;

            // 타입 고정
            baseData.renderType   = CameraRenderType.Base;
            overlayData.renderType = CameraRenderType.Overlay;

            // Overlay는 클리어 무의미하므로 혼동 방지용
            overlayCam.clearFlags = CameraClearFlags.Nothing;

            var stack = baseData.cameraStack;
            if (stack == null)
            {
                Debug.LogError("[CameraUtil] cameraStack == null (URP 초기화 상태 확인 필요)");
                return false;
            }

            if (!stack.Contains(overlayCam))
                stack.Add(overlayCam);

            return true;
        }

        public static bool UnstackOverlay(Camera baseCam, Camera overlayCam)
        {
            if (!TryGetURPData(baseCam, out var baseData)) return false;
            var stack = baseData.cameraStack;
            return stack != null && stack.Remove(overlayCam);
        }

        public static bool ClearStack(Camera baseCam)
        {
            if (!TryGetURPData(baseCam, out var baseData)) return false;
            var stack = baseData.cameraStack;
            if (stack == null) return false;
            stack.Clear();
            return true;
        }

        private static bool TryGetURPData(Camera cam, out UniversalAdditionalCameraData data)
        {
            data = null;
            if (!cam) return false;
            if (!cam.TryGetComponent(out data))
                data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            return data;
        }

        public static void ReturnMainCamera()
        {
            Camera mainCamera = Camera.main;
            if (!mainCamera) return;

            SetRenderType(mainCamera, CameraRenderType.Base);
            ClearStack(mainCamera);
            mainCamera.clearFlags = CameraClearFlags.Skybox;
        }
        
#else
        public static bool SetRenderType(Camera cam, object type) { return false; }
        public static bool SetRenderer(Camera cam, int rendererIndex) { return false; }
        public static bool TryStackOverlay(Camera baseCam, Camera overlayCam) { return false; }
        public static bool UnstackOverlay(Camera baseCam, Camera overlayCam) { return false; }
        public static bool ClearStack(Camera baseCam) { return false; }
#endif
    }
}
