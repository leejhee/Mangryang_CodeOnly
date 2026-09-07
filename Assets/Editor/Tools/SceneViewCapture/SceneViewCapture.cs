using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SceneViewCapture
{
    private const string DefaultRelativeFolder = "Assets/SceneViewShots";
    private const string PrefKeyFolder = "SceneViewCapture.Folder";

    [MenuItem("Tools/Capture/Scene View (Camera Only) %#xc")]
    public static void CaptureCameraOnly()
    {
        Capture(includeOverlays: false);
    }

    [MenuItem("Tools/Capture/Scene View (Include Grid & Wire)")]
    public static void CaptureWithOverlays()
    {
        Capture(includeOverlays: true);
    }

    [MenuItem("Tools/Capture/Set Save Folder...")]
    public static void SetSaveFolder()
    {
        string current = GetFolderAbsolute();
        string chosen = EditorUtility.OpenFolderPanel("Choose Save Folder (inside project if you want auto-import)", current, "");
        if (!string.IsNullOrEmpty(chosen))
        {
            // 저장 경로 기억
            EditorPrefs.SetString(PrefKeyFolder, chosen);
            Debug.Log($"[SceneViewCapture] Save folder set to:\n{chosen}");
        }
    }

    private static void Capture(bool includeOverlays)
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null && SceneView.sceneViews != null && SceneView.sceneViews.Count > 0)
            sv = SceneView.sceneViews[0] as SceneView;

        if (sv == null || sv.camera == null)
        {
            Debug.LogError("[SceneViewCapture] No Scene View found. Open a Scene view first.");
            return;
        }

        var cam = sv.camera;
        int width  = Mathf.Max(1, cam.pixelWidth);
        int height = Mathf.Max(1, cam.pixelHeight);

        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        var prevCamRT = cam.targetTexture;
        var prevActive = RenderTexture.active;

        try
        {
            if (includeOverlays)
            {
                // SceneView 느낌에 가깝게 캡쳐 (그리드/와이어 등)
                Graphics.SetRenderTarget(rt);
#if UNITY_2020_1_OR_NEWER
                Handles.DrawCamera(new Rect(0, 0, width, height), cam, DrawCameraMode.TexturedWire);
#else
                Handles.DrawCamera(new Rect(0, 0, width, height), cam);
#endif
                RenderTexture.active = rt;
            }
            else
            {
                // 카메라가 보는 화면만 (빠르고 정확)
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
            }

            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            var bytes = tex.EncodeToPNG();

            // 경로 준비
            string folderAbs = GetFolderAbsolute();
            if (!Directory.Exists(folderAbs))
                Directory.CreateDirectory(folderAbs);

            string filename = $"SceneView_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string savePathAbs = Path.Combine(folderAbs, filename);
            File.WriteAllBytes(savePathAbs, bytes);

            // 프로젝트 내부 경로면 임포트
            string projectRoot = Directory.GetCurrentDirectory().Replace('\\', '/');
            string normalized = savePathAbs.Replace('\\', '/');
            if (normalized.StartsWith(projectRoot))
            {
                string relative = normalized.Substring(projectRoot.Length + 1); // drop trailing '/'
                AssetDatabase.Refresh();
                Debug.Log($"[SceneViewCapture] Saved: {relative}");
            }
            else
            {
                Debug.Log($"[SceneViewCapture] Saved (outside project): {savePathAbs}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SceneViewCapture] Failed: {ex}");
        }
        finally
        {
            cam.targetTexture = prevCamRT;
            RenderTexture.active = prevActive;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }

    private static string GetFolderAbsolute()
    {
        string pref = EditorPrefs.GetString(PrefKeyFolder, string.Empty);
        if (!string.IsNullOrEmpty(pref)) return pref;

        string projectRoot = Directory.GetCurrentDirectory(); // 프로젝트 루트
        return Path.Combine(projectRoot, DefaultRelativeFolder);
    }
}
