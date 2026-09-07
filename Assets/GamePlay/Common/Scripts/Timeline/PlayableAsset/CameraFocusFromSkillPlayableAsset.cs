// CameraFocusFromSkillPlayableAsset.cs
using System;
using UnityEngine;
using UnityEngine.Playables;
using GamePlay.Common.Scripts.Timeline.PlayableAsset;
using GamePlay.Common.Scripts.Timeline.PlayableBehaviour;

public enum SkillCameraTargetRole { Caster, Target }
public enum SkillCameraEffectType { FocusZoom /*, Shake, Group ...*/ }

[Serializable]
public sealed class CameraFocusFromSkillPlayableAsset : SkillTimeLinePlayableAsset
{
    [Header("Target")]
    public SkillCameraTargetRole role = SkillCameraTargetRole.Caster;

    [Header("Effect")]
    public SkillCameraEffectType effect = SkillCameraEffectType.FocusZoom;
    public bool setFollow = true;
    public bool disableInputWhilePlaying = true;
    public bool restoreZoomOnEnd = false;

    [Header("Zoom (FocusZoom)")]
    public bool animateZoom = true;
    public float targetOrthoSize = 10f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);
    
    [Header("Zoom – Auto Direction & Curves")]
    public bool useDirectionalEase = true; // true면 방향별 커브 사용
    public AnimationCurve easeWhenZoomIn  = AnimationCurve.EaseInOut(0,0,1,1);
    public AnimationCurve easeWhenZoomOut = AnimationCurve.EaseInOut(0,0,1,1);
        
    [Header("Zoom – Relative Mode (optional)")]
    public bool useRelativeZoom = false;   // true면 절대값 대신 배율 사용
    public float zoomInFactor  = 0.90f;    // 시작크기 * 0.90 → 줌-인
    public float zoomOutFactor = 1.10f;    // 시작크기 * 1.10 → 줌-아웃
    public float minZoomDelta  = 0.05f;    // 변화량이 이 값보다 작으면 줌 생략
    
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = CreatePlayableWithContext<CameraFocusFromSkillPlayableBehaviour>(graph, owner);
        var bhv = playable.GetBehaviour();

        // 컨텍스트가 없으면 no-op
        if (!bhv.HasContext) return playable;

        // Behaviour에 파라미터 전달
        bhv.role = role;
        bhv.effect = effect;
        bhv.setFollow = setFollow;
        bhv.disableInputWhilePlaying = disableInputWhilePlaying;
        bhv.restoreZoomOnEnd = restoreZoomOnEnd;

        bhv.animateZoom = animateZoom;
        bhv.targetOrthoSize = targetOrthoSize;
        bhv.ease = ease;
        
        bhv.useDirectionalEase = useDirectionalEase;
        bhv.easeWhenZoomIn  = easeWhenZoomIn;
        bhv.easeWhenZoomOut = easeWhenZoomOut;
        bhv.useRelativeZoom = useRelativeZoom;
        bhv.zoomInFactor  = zoomInFactor;
        bhv.zoomOutFactor = zoomOutFactor;
        bhv.minZoomDelta  = minZoomDelta;
        
        return playable;
    }
}