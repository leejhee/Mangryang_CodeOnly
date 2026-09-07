using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.Unit.Components
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class CharAnimDriver : MonoBehaviour
    {
        // 지속 상태는 Bool, 일회성 상태는 Trigger를 사용합니다.
        // 캐릭터별 Controller를 순차 이전하는 동안 기존 Bool도 함께 지원합니다.
        //   Float: Speed - BlendTree 쓰는 경우
        private static readonly int P_IsMoving = CharacterAnimatorContract.MovingHash;
        private static readonly int P_Hit = CharacterAnimatorContract.HitHash;
        private static readonly int P_Push = CharacterAnimatorContract.PushHash;
        private static readonly int P_Evade = CharacterAnimatorContract.EvadeHash;
        private static readonly int P_JumpOut = CharacterAnimatorContract.JumpOutHash;
        private static readonly int P_JumpIn = CharacterAnimatorContract.JumpInHash;
        private static readonly int P_ParryReady = CharacterAnimatorContract.ParryReadyHash;
        //private static readonly int P_Speed = Animator.StringToHash("Speed");

        [SerializeField] private Animator animator; // 비워두면 Awake에서 GetComponent
        public Animator Animator => animator;

        // 타임라인 임대/반납 (Director가 잡는 동안은 파라미터 구동 금지)
        private int _timelineLease;
        private bool _parryReady;
        private bool _parryReadyAssigned;
        public bool IsTimelineActive => _timelineLease > 0;

        private sealed class Lease : IDisposable
        {
            private readonly CharAnimDriver _d;
            private bool _disposed;

            public Lease(CharAnimDriver d) { _d = d; }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                if (_d._timelineLease > 0) _d._timelineLease--;
                if (_d._timelineLease == 0 && _d.animator)
                {
                    _d.animator.Rebind();
                    _d.animator.Update(0f);
                    _d.ReapplyPersistentStates();
                }
            }
        }

        public IDisposable AcquireTimelineLease()
        {
            _timelineLease++;
            return new Lease(this);
        }

        // 파라미터 타입 캐시(경고 1회만)
        private Dictionary<int, AnimatorControllerParameterType> _availableParams;
        private readonly HashSet<int> _warnedInvalidParams = new();

        // 내부 토큰 (상태 유지 중 중단 시 최신 것만 유효)
        private CancellationTokenSource _cts;

        private CancellationToken NewToken(CancellationToken external = default)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(external);
            return _cts.Token;
        }

        void Awake()
        {
            if (!animator) animator = GetComponent<Animator>();
            BuildParamCache();
        }

        void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        void BuildParamCache()
        {
            _availableParams = new Dictionary<int, AnimatorControllerParameterType>();
            if (!animator) return;
            foreach (var p in animator.parameters)
                _availableParams[p.nameHash] = p.type;
        }

        bool CanDrive => !IsTimelineActive && animator && animator.isActiveAndEnabled;

        void SetBoolParam(int id, bool value)
        {
            if (!CanDrive) return;
            if (!TryGetParamType(id, out AnimatorControllerParameterType type))
                return;
            if (type != AnimatorControllerParameterType.Bool)
            {
                if (_warnedInvalidParams.Add(id))
                    Debug.LogWarning(
                        $"[AnimDriver] Animator '{animator.name}' param hash {id} is {type}, expected Bool.");
                return;
            }

            animator.SetBool(id, value);
        }

        bool TryGetParamType(int id, out AnimatorControllerParameterType type)
        {
            if (_availableParams != null && _availableParams.TryGetValue(id, out type))
                return true;

            type = default;
            if (_warnedInvalidParams.Add(id))
                Debug.LogWarning(
                    $"[AnimDriver] Animator '{animator.name}' missing param hash {id}. Check controller params.");
            return false;
        }

        void ResetParam(int id)
        {
            if (!CanDrive) return;
            if (!TryGetParamType(id, out AnimatorControllerParameterType type))
                return;

            switch (type)
            {
                case AnimatorControllerParameterType.Bool:
                    animator.SetBool(id, false);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    animator.ResetTrigger(id);
                    break;
                default:
                    if (_warnedInvalidParams.Add(id))
                        Debug.LogWarning(
                            $"[AnimDriver] Animator '{animator.name}' param hash {id} cannot be reset as an animation state.");
                    break;
            }
        }

        void SetFloatParam(int id, float value)
        {
            if (!CanDrive) return;
            if (!TryGetParamType(id, out AnimatorControllerParameterType type))
                return;
            if (type != AnimatorControllerParameterType.Float)
            {
                if (_warnedInvalidParams.Add(id))
                    Debug.LogWarning(
                        $"[AnimDriver] Animator '{animator.name}' param hash {id} is {type}, expected Float.");
                return;
            }

            animator.SetFloat(id, value);
        }

        // ===== 공개 API (편의 메서드) =====
        public void SetMoving(bool v) => SetBoolParam(P_IsMoving, v);
        public void SetPush(bool v) => SetBoolParam(P_Push, v);
        public void SetJumpOut(bool v) => SetBoolParam(P_JumpOut, v);
        public void SetJumpIn(bool v) => SetBoolParam(P_JumpIn, v);
        public void SetParryReady(bool value)
        {
            _parryReady = value;
            _parryReadyAssigned = true;
            SetBoolParam(P_ParryReady, value);
        }
        //public void SetSpeed(float v) => SetFloatParam(P_Speed, v);

        /// body 실행 동안 특정 Bool을 true로 유지 → 종료 시 false
        public async UniTask WithFlag(int paramId, Func<CancellationToken, UniTask> body, CancellationToken ct = default)
        {
            ct = NewToken(ct);
            SetBoolParam(paramId, true);
            try { await body(ct); }
            finally { SetBoolParam(paramId, false); }
        }

        /// <summary>
        /// Trigger Controller에서는 애니메이션을 한 번 시작하고 게임 로직과 분리합니다.
        /// 아직 이전하지 않은 Bool Controller에서는 기존 스코프 동작을 유지합니다.
        /// </summary>
        private async UniTask WithOneShot(
            int paramId,
            Func<CancellationToken, UniTask> body,
            CancellationToken ct = default)
        {
            ct = NewToken(ct);

            if (!CanDrive || !TryGetParamType(paramId, out AnimatorControllerParameterType type))
            {
                await body(ct);
                return;
            }

            if (type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(paramId);
                animator.SetTrigger(paramId);
                await body(ct);
                return;
            }

            if (type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(paramId, true);
                try { await body(ct); }
                finally { SetBoolParam(paramId, false); }
                return;
            }

            if (_warnedInvalidParams.Add(paramId))
                Debug.LogWarning(
                    $"[AnimDriver] Animator '{animator.name}' param hash {paramId} must be Bool or Trigger.");
            await body(ct);
        }

        // 전용 스코프 편의 함수들
        public UniTask WithMoving(Func<CancellationToken, UniTask> body, CancellationToken ct = default)
            => WithFlag(P_IsMoving, body, ct);

        public UniTask WithHit(Func<CancellationToken, UniTask> body, CancellationToken ct = default)
            => WithOneShot(P_Hit, body, ct);

        public UniTask WithPush(Func<CancellationToken, UniTask> body, CancellationToken ct = default)
            => WithFlag(P_Push, body, ct);

        public UniTask WithEvade(Func<CancellationToken, UniTask> body, CancellationToken ct = default)
            => WithOneShot(P_Evade, body, ct);

        public UniTask WithJumpOut(Func<CancellationToken, UniTask> body, CancellationToken ct = default)
            => WithFlag(P_JumpOut, body, ct);

        public UniTask WithJumpIn(Func<CancellationToken, UniTask> body, CancellationToken ct = default)
            => WithFlag(P_JumpIn, body, ct);

        /// 모든 플래그/스피드 초기화
        public void ResetAllFlags()
        {
            SetMoving(false);
            ResetParam(P_Hit);
            SetPush(false);
            ResetParam(P_Evade);
            SetJumpOut(false);
            SetJumpIn(false);
            //SetSpeed(0f);
        }

        private void ReapplyPersistentStates()
        {
            if (_parryReadyAssigned)
                SetBoolParam(P_ParryReady, _parryReady);
        }
    }
}
