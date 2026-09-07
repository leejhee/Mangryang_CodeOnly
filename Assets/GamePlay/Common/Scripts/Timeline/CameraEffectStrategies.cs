using GamePlay.Features.Battle.Scripts;
using UnityEngine;

namespace GamePlay.Common.Scripts.Timeline
{
    public struct CameraCtx
    {
        public BattleCameraDriver driver;
        public BattleCameraInput input;
        public Transform target;
        public float startOrtho;
        public float duration;
    }

    public interface ICamEffectStrategy
    {
        void OnPlay(CameraCtx ctx);
        void Process(CameraCtx ctx, float t01); // 0..1
        void OnPause(CameraCtx ctx);
    }

    // 포커스-줌 (OrthoSize 보간)
    public sealed class FocusZoomStrategy : ICamEffectStrategy
    {
        readonly float _targetOrtho;
        readonly AnimationCurve _ease;

        public FocusZoomStrategy(float targetOrthoSize, AnimationCurve ease)
        {
            _targetOrtho = Mathf.Max(0.01f, targetOrthoSize);
            _ease = ease ?? AnimationCurve.Linear(0,0,1,1);
        }

        public void OnPlay(CameraCtx ctx) { /* startOrtho는 ctx에 담겨옴 */ }

        public void Process(CameraCtx ctx, float t01)
        {
            if (!ctx.driver || !ctx.driver.IsOrtho) return;
            float w = Mathf.Clamp01(_ease.Evaluate(Mathf.Clamp01(t01)));
            float v = Mathf.Lerp(ctx.startOrtho, _targetOrtho, w);
            ctx.driver.SetZoom(v); // 드라이버 내부 min/max 클램프
        }

        public void OnPause(CameraCtx ctx) { }
    }
}