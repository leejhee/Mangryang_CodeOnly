using UnityEngine;

public class CharAnim
{
    protected Animator _Animator;       // 애니메이터

    public Animator Animator => _Animator;


    /// <summary>
    /// 초기화
    /// </summary>
    public void Initialized(Animator animator)
    {
        _Animator = animator;
    }
    //public void PlayAnimation(PlayerState state)
    //{
    //    _Animator.CrossFade($"{state}", 1f, -1, 0);
    //}
}
