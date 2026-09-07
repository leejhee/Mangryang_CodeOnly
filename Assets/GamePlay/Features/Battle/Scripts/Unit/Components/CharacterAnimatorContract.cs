using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.Unit.Components
{
    /// <summary>
    /// 전투 캐릭터 Animator가 공통으로 제공해야 하는 파라미터 계약입니다.
    /// 캐릭터별 Controller를 통합하기 전까지 기존 파라미터 이름을 유지합니다.
    /// </summary>
    public static class CharacterAnimatorContract
    {
        public const string MovingParameter = "IsMoving";

        // 기존 Controller의 이름은 OnAttack이지만 실제 용도는 피격(Hit)입니다.
        public const string HitParameter = "OnAttack";
        public const string PushParameter = "Push";
        public const string EvadeParameter = "Evade";
        public const string JumpOutParameter = "JumpOut";
        public const string JumpInParameter = "JumpIn";
        public const string ParryReadyParameter = "ParryReady";

        public static readonly int MovingHash = Animator.StringToHash(MovingParameter);
        public static readonly int HitHash = Animator.StringToHash(HitParameter);
        public static readonly int PushHash = Animator.StringToHash(PushParameter);
        public static readonly int EvadeHash = Animator.StringToHash(EvadeParameter);
        public static readonly int JumpOutHash = Animator.StringToHash(JumpOutParameter);
        public static readonly int JumpInHash = Animator.StringToHash(JumpInParameter);
        public static readonly int ParryReadyHash = Animator.StringToHash(ParryReadyParameter);
    }
}
