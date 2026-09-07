using Cysharp.Threading.Tasks;
using GamePlay.Features.Battle.Scripts.BattleMap;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace GamePlay.Features.Battle.Scripts
{
    public sealed class BattleCameraDriver : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera followCam;
        [SerializeField] private float defaultBlendSeconds = 0.5f;
        [SerializeField] private float orthoSizeClampMax;
        
        private CinemachineBrain _brain;
        
        [Header("Focus Settings")]
        [SerializeField] private bool useFocusZoom = true;
        [SerializeField] private float focusOrthoSize = 10f;
        [SerializeField] private float focusZoomSeconds = 0.35f;

        [Header("Jump Focus Settings")]
        [SerializeField, Min(0f)] private float jumpFocusBlendSeconds = 0.2f;
        [SerializeField, Min(0f)] private float jumpFocusZoomSeconds = 0.2f;
        
        [Header("Free Mode Settings")]
        [SerializeField] float minOrthoSize = 5f;
        [SerializeField] float maxOrthoSize = 20f;
        
        public enum FreeEdgeSlackMode { None, FixedWorld, ScreenHalf }

        [SerializeField] FreeEdgeSlackMode freeSlackMode = FreeEdgeSlackMode.ScreenHalf; // 기본값: 화면 반만큼
        [SerializeField] float fixedWorldSlack = 0f;  // FreeEdgeSlackMode.FixedWorld일 때만
        [SerializeField] float freeInnerPaddingWorld = 0f;  // 경계 안쪽 여유
        
        private Transform _freeAnchor;                 // 자유 팬용 앵커
        private Transform _actionAnchor;               // 행동 목적지 연출용 앵커
        private bool _lockedToTarget = true;           // true=Follow 모드, false=Free 모드

        private Vector2 _stageOriginWorld;
        private Vector2 _stageSizeWorld;
        public bool IsLockedToTarget => _lockedToTarget;
        public bool IsOrtho => followCam && followCam.Lens.Orthographic;
        public float CurrentOrthoSize => IsOrtho ? followCam.Lens.OrthographicSize : 0f;
        public float FocusOrthoSize => IsOrtho ? focusOrthoSize : 0f;
        private void Awake()
        {
            _brain = Camera.main ? Camera.main.GetComponent<CinemachineBrain>() : null;
            if (!_brain) Debug.LogWarning("[BattleCameraDriver] CinemachineBrain(MainCamera) 미발견");
        }
        
        private void StoreStageBounds(Vector2 originWorld, Vector2 sizeWorld)
        {
            _stageOriginWorld = originWorld;
            _stageSizeWorld   = sizeWorld;
        }
        
        #region Intro Showing Part
        public async UniTask ShowStageIntro(StageField stage, float paddingWorld = 1f, float fadeSeconds = 0.8f)
        {
            if (!stage || !followCam) return;

            var grid = stage.Grid;
            var lossy = stage.transform.lossyScale;
            Vector2 cellWorld = new(grid.cellSize.x * lossy.x, grid.cellSize.y * lossy.y);

            Vector2Int gs = stage.GridSize;
            Vector2 sizeWorld = new(gs.x * cellWorld.x, gs.y * cellWorld.y);

            Vector2Int half = gs / 2;
            var zeroCenter = (Vector2)grid.GetCellCenterWorld(Vector3Int.zero);
            Vector2 originWorld = zeroCenter - new Vector2(half.x + 0.5f, half.y + 0.5f) * cellWorld;

            Vector2 centerWorld = originWorld + 0.5f * sizeWorld;
            
            StoreStageBounds(originWorld, sizeWorld);
            
            var lens = followCam.Lens;
            if (lens.Orthographic)
            {
                float need = ComputeOrthoSizeToFit(sizeWorld, paddingWorld);
                if (orthoSizeClampMax > 0f) need = Mathf.Min(need, orthoSizeClampMax);
                lens.OrthographicSize = need;
                followCam.Lens = lens;
            }
            else
            {
                Debug.LogWarning("[BattleCameraDriver] 현재 카메라가 Perspective입니다. " +
                                 "URP 스택상 Base/Overlay 투영 일치 권장. 런타임 강제 전환은 금지합니다.");
            }

            var anchor = EnsureStageAnchor(stage.transform, centerWorld);
            followCam.Follow = anchor;
            followCam.LookAt = anchor;
            followCam.Priority = 100;

            await FadeFromBlack(fadeSeconds);
        }

        private float ComputeOrthoSizeToFit(Vector2 sizeWorld, float pad)
        {
            var cam = _brain && _brain.OutputCamera ? _brain.OutputCamera : Camera.main;
            float aspect = cam ? cam.aspect : 16f / 9f;
            float halfH = sizeWorld.y * 0.5f;
            float halfW = sizeWorld.x * 0.5f;
            return Mathf.Max(halfH, halfW / Mathf.Max(0.0001f, aspect)) + pad;
        }

        private Transform EnsureStageAnchor(Transform stageRoot, Vector2 centerWorld)
        {
            const string Name = "__StageCameraAnchor";
            var t = stageRoot.Find(Name);
            if (!t)
            {
                var go = new GameObject(Name);
                go.transform.SetParent(stageRoot, worldPositionStays: false);
                t = go.transform;
            }
            t.position = centerWorld;
            t.rotation = Quaternion.identity;
            t.localScale = Vector3.one;
            return t;
        }

        private async UniTask FadeFromBlack(float dur)
        {
            if (dur <= 0f) return;
            var go = new GameObject("__BattleIntroFade");
            var canvas = go.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10000;
            var cg = go.AddComponent<CanvasGroup>(); var img = go.AddComponent<Image>(); img.color = Color.black; cg.alpha = 1f;
            float t = 0f;
            while (t < dur) { t += Time.deltaTime; cg.alpha = Mathf.Lerp(1f, 0f, t / dur); await UniTask.Yield(); }
            Destroy(go);
        }
        #endregion
        
        #region Mode Implementation
        /// <summary>Follow 모드 진입</summary>
        public async UniTask EnterFollowAsync(
            Transform target, 
            float? blendSeconds = null,
            float? targetOrthoSize = null, 
            float? zoomSeconds = null)
        {
            _lockedToTarget = true;
            await Focus(target, blendSeconds, targetOrthoSize, zoomSeconds);
        }
        
        public async UniTask FollowDuringAsync(Transform target,
            UniTask task,
            float? blendSeconds = 0.3f,
            float? targetOrthoSize = null,
            float? zoomSeconds = null)
        {
            await EnterFollowAsync(target, blendSeconds, targetOrthoSize ?? focusOrthoSize, zoomSeconds);
            await task; // 액션 실행 완료까지 대기
            // 여기서 Free로 자동 복귀시키고 싶다면:
            // EnterFree(stageFieldRef);
        }

        /// <summary>
        /// 제네릭 반환형 버전. 작업의 결과를 그대로 반환합니다.
        /// </summary>
        public async UniTask<T> FollowDuringAsync<T>(Transform target,
            UniTask<T> task,
            float? blendSeconds = 0.3f,
            float? targetOrthoSize = null,
            float? zoomSeconds = null)
        {
            await EnterFollowAsync(target, blendSeconds, targetOrthoSize ?? focusOrthoSize, zoomSeconds);
            var result = await task;
            return result;
        }

        /// <summary>
        /// 점프 중에는 아직 이동하지 않은 캐릭터 대신 목적지를 먼저 추적합니다.
        /// 점프가 끝나면 실제 캐릭터 Transform으로 추적 대상을 되돌립니다.
        /// </summary>
        public async UniTask FollowJumpDuringAsync(
            Vector3 destination,
            Transform characterTarget,
            UniTask jumpTask)
        {
            Transform destinationAnchor = EnsureActionAnchor(destination);
            UniTask focusTask = EnterFollowAsync(
                destinationAnchor,
                jumpFocusBlendSeconds,
                focusOrthoSize,
                jumpFocusZoomSeconds);

            try
            {
                await UniTask.WhenAll(focusTask, jumpTask);
            }
            finally
            {
                if (characterTarget)
                    FocusImmediate(characterTarget);
            }
        }
        
        /// <summary>Free 모드 진입. 현재 화면 중심을 기준으로 앵커 생성, 재배치</summary>
        public void EnterFree(StageField stage)
        {
            if (!followCam) return;

            if (_freeAnchor == null)
            {
                var go = new GameObject("__CamFreeAnchor");
                go.transform.SetParent(stage.transform, worldPositionStays: false);
                _freeAnchor = go.transform;
            }

            // 현재 화면 중심을 월드로 환산
            var outCam = _brain && _brain.OutputCamera ? _brain.OutputCamera : Camera.main;
            Vector3 camPos = outCam ? outCam.transform.position : followCam.transform.position;
            _freeAnchor.position = new Vector3(camPos.x, camPos.y, 0f);

            followCam.Follow = _freeAnchor;
            followCam.LookAt = _freeAnchor;
            followCam.Priority = 100;
            _lockedToTarget = false;

            ClampFreeAnchorToStage(); // 시작 시점 클램프
        }

        private Transform EnsureActionAnchor(Vector3 worldPosition)
        {
            if (!_actionAnchor)
            {
                GameObject anchorObject = new("__CamActionAnchor");
                _actionAnchor = anchorObject.transform;
                _actionAnchor.SetParent(transform, worldPositionStays: true);
            }

            _actionAnchor.position = worldPosition;
            _actionAnchor.rotation = Quaternion.identity;
            _actionAnchor.localScale = Vector3.one;
            return _actionAnchor;
        }

        /// <summary>Free 모드에서 월드 델타만큼 팬</summary>
        public void PanFree(Vector2 worldDelta)
        {
            if (_lockedToTarget || !_freeAnchor) return;
            _freeAnchor.position += (Vector3)worldDelta;
            ClampFreeAnchorToStage();
        }

        /// <summary>줌(OrthoSize) 즉시 설정(클램프 포함)</summary>
        public void SetZoom(float orthoSize)
        {
            if (!followCam) return;
            var lens = followCam.Lens;
            if (!lens.Orthographic) return;

            lens.OrthographicSize = Mathf.Clamp(orthoSize, minOrthoSize, maxOrthoSize);
            followCam.Lens = lens;
            ClampFreeAnchorToStage(); // 줌 변하면 화면 반경도 변하므로 재클램프
        }

        /// <summary>현재 줌에 대해 Free 앵커를 전장 경계 내로 클램프</summary>
        void ClampFreeAnchorToStage()
        {
            if (!_freeAnchor) return;
            LensSettings lens = followCam.Lens;
            if (!lens.Orthographic) return;

            var cam = _brain && _brain.OutputCamera ? _brain.OutputCamera : Camera.main;
            float aspect = cam ? cam.aspect : 16f/9f;

            float halfH = lens.OrthographicSize;
            float halfW = lens.OrthographicSize * aspect;

            // ▶ 슬랙 계산
            float slackX = 0f, slackY = 0f;
            switch (freeSlackMode)
            {
                case FreeEdgeSlackMode.None:
                    slackX = 0f; slackY = 0f;
                    break;
                case FreeEdgeSlackMode.FixedWorld:
                    slackX = fixedWorldSlack; slackY = fixedWorldSlack;
                    break;
                case FreeEdgeSlackMode.ScreenHalf:
                    // 핵심: 화면 반만큼 바깥으로 허용
                    slackX = halfW; slackY = halfH;
                    break;
            }

            float minX = _stageOriginWorld.x + halfW - slackX + freeInnerPaddingWorld;
            float maxX = _stageOriginWorld.x + _stageSizeWorld.x - halfW + slackX - freeInnerPaddingWorld;
            float minY = _stageOriginWorld.y + halfH - slackY + freeInnerPaddingWorld;
            float maxY = _stageOriginWorld.y + _stageSizeWorld.y - halfH + slackY - freeInnerPaddingWorld;
            
            // edge case
            if (minX > maxX) { float c = (_stageOriginWorld.x + _stageSizeWorld.x) * 0.5f; minX = maxX = c; }
            if (minY > maxY) { float c = (_stageOriginWorld.y + _stageSizeWorld.y) * 0.5f; minY = maxY = c; }

            var p = _freeAnchor.position;
            p.x = Mathf.Clamp(p.x, minX, maxX);
            p.y = Mathf.Clamp(p.y, minY, maxY);
            _freeAnchor.position = p;
        }
        
        #endregion
        
        #region Core
        public async UniTask Focus(Transform target,
                               float? blendSeconds = null,
                               float? targetOrthoSize = null,
                               float? zoomSeconds = null)
        {
            if (!target || !followCam || !_brain) return;

            followCam.Follow = target;
            followCam.LookAt = target;
            followCam.Priority = 100;

            var def = _brain.DefaultBlend;
            float prevBlend = def.Time;
            def.Time = blendSeconds ?? prevBlend;
            _brain.DefaultBlend = def;

            UniTask zoomTask = UniTask.CompletedTask;
            var lens = followCam.Lens;
            bool doZoom = (useFocusZoom || targetOrthoSize.HasValue) && lens.Orthographic;

            if (doZoom)
            {
                float aim = targetOrthoSize ?? focusOrthoSize;
                float dur = zoomSeconds ?? blendSeconds ?? focusZoomSeconds;
                zoomTask = ZoomTo(aim, dur);
            }

            var blendWait = UniTask.WaitUntil(() => !_brain.IsBlending);
            await UniTask.WhenAll(blendWait, zoomTask);

            await WaitForCameraSettled();
            
            def = _brain.DefaultBlend;
            def.Time = prevBlend;
            _brain.DefaultBlend = def;

            await UniTask.Delay(500); // 안정적인 카메라 고정을 위한 임시 wait
        }

        /// <summary>
        /// OrthoSize(또는 FOV)를 duration 동안 부드럽게 보간
        /// </summary>
        public async UniTask ZoomTo(float targetValue, float duration)
        {
            if (!followCam) return;

            var lens = followCam.Lens;
            bool isOrtho = lens.Orthographic;
            float start = isOrtho ? lens.OrthographicSize : lens.FieldOfView;
            if (Mathf.Approximately(start, targetValue) || duration <= 0f)
            {
                if (isOrtho) { lens.OrthographicSize = targetValue; }
                else         { lens.FieldOfView      = targetValue; }
                followCam.Lens = lens;
                return;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float w = Mathf.Clamp01(t / duration);
                // Smooth Step이라고 하네요
                float k = w * w * (3f - 2f * w);
                float v = Mathf.Lerp(start, targetValue, k);

                lens = followCam.Lens;
                if (isOrtho) { lens.OrthographicSize = v; }
                else         { lens.FieldOfView      = v; }
                followCam.Lens = lens;

                await UniTask.Yield();
            }
        }
            
        public void FocusImmediate(Transform target)
        {
            if (!target || !followCam) return;
            followCam.Follow = target;
            followCam.LookAt = target;
            followCam.Priority = 100;
        }
        
        private async UniTask WaitForCameraSettled(
            float positionEpsilon = 0.01f, // 이 이하 움직이면 멈췄다고 본다
            int stableFrames = 2           // 몇 프레임 연속으로 안 움직여야 하는지
        )
        {
            if (_brain == null || _brain.OutputCamera == null)
                return;

            var camTr = _brain.OutputCamera.transform;
            Vector3 lastPos = camTr.position;
            int stableCount = 0;

            while (stableCount < stableFrames)
            {
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

                if (camTr == null) break;

                Vector3 now = camTr.position;
                if ((now - lastPos).sqrMagnitude < positionEpsilon * positionEpsilon)
                {
                    stableCount++;
                }
                else
                {
                    stableCount = 0;
                }

                lastPos = now;
            }
        }
        
        #endregion
    }
}
