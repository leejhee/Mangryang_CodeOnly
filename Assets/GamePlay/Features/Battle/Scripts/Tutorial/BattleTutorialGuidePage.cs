using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.Tutorial
{
    public enum GuideAnchor
    {
        ScreenTop,
        Actor,
        ScreenPosition,
    }
    
    [System.Serializable]
    public class BattleTutorialGuidePage
    {
        [TextArea]
        public string text;

        public GuideAnchor anchor;

        public Vector2 screenNormalizedPos = new(0.5f, 0.5f);
    }
}