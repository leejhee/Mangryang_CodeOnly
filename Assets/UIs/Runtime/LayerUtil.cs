using UnityEngine;
using UnityEngine.UI;

namespace UIs.Runtime
{
    /// <summary>
    /// 임의의 씬에서 UI를 배치하기 위한 레이어 탐색 헬퍼 클래스
    /// </summary>
    public static class LayerUtil
    {
        private static RectTransform root, screen, modal, toast;

        private const string RootName   = "UIRoot";
        private const string ScreenName = "ScreenLayer";
        private const string ModalName  = "ModalLayer";
        private const string ToastName  = "ToastLayer";

        public static RectTransform Root 
        {
            get {
                if (root) return root;

                var go = GameObject.Find(RootName) ?? new GameObject(RootName, typeof(RectTransform));
                if (!go.TryGetComponent(out Canvas canvas)) {
                    canvas = go.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    go.AddComponent<CanvasScaler>();
                    go.AddComponent<GraphicRaycaster>();
                }

                root = go.GetComponent<RectTransform>();
                Stretch(root);
                Object.DontDestroyOnLoad(go);             
                go.layer = LayerMask.NameToLayer("UI");

                return root;
            }
        }

        public static RectTransform ScreenLayer => EnsureLayer(ref screen, ScreenName, 0);
        public static RectTransform ModalLayer  => EnsureLayer(ref modal,  ModalName,  1000);
        public static RectTransform ToastLayer  => EnsureLayer(ref toast,  ToastName,  2000);

        private static RectTransform EnsureLayer(ref RectTransform cache, string name, int sortingOrder)
        {
            if (cache) return cache;

            var t = Root.Find(name) as RectTransform;
            if (!t) {
                var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
                t = go.GetComponent<RectTransform>();
                t.SetParent(Root, false);
                Stretch(t);

                var sub = go.GetComponent<Canvas>();
                sub.overrideSorting = true;
                sub.sortingOrder    = sortingOrder;
                go.layer = Root.gameObject.layer;
            }

            cache = t;
            return cache;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot     = new Vector2(0.5f, 0.5f);
        }
    }
}