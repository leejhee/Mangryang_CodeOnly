using Cysharp.Threading.Tasks;
using GamePlay.Features.Battle.Scripts.Unit;
using System;
using UnityEngine;


namespace GamePlay.Features.Battle.Scripts.UI.BattleHovering
{
    [ExecuteAlways]
    public class CharacterHpBar : MonoBehaviour
    {
        [SerializeField] private CharBase charBase;
        [SerializeField] private SpriteRenderer hpBar;
        private MaterialPropertyBlock _mp; //= new();

        [Range(0f, 1f)]
        public float fillAmount = 1f;
        
        private void Awake()
        {
            // Awake에서 한 번 초기화
            if (_mp == null)
                _mp = new MaterialPropertyBlock();
        }
        

        public void SetFillAmount(float target, float max)
        {
            SetFillAmount(target / max);
        }
        
        public async void SetFillAmount(float targetPercent)
        {
            if (_mp == null)
                _mp = new MaterialPropertyBlock();

            if (hpBar == null)
                return;
            
            hpBar.GetPropertyBlock(_mp);
            
            float startValue = _mp.GetFloat("_Fill"); // 현재 fill 값 가져오기
            float duration = 0.1f;
            float curDuration = 0f;
            
            while (curDuration < duration)
            {
                curDuration += Time.deltaTime;
                float t = curDuration / duration;
                float newValue = Mathf.Lerp(startValue, targetPercent, t); // 선형 보간
                fillAmount = newValue;
                _mp.SetFloat("_Fill", Mathf.Clamp01(newValue));
                hpBar.SetPropertyBlock(_mp);

                await UniTask.Yield(); // 다음 프레임까지 대기
            }
            
            _mp.SetFloat("_Fill", Mathf.Clamp01(targetPercent));
            hpBar.SetPropertyBlock(_mp);
        }
        private void Update()
        {
            //SetFillAmount(fillAmount);
            // #if UNITY_EDITOR
            // // 에디터에서만 자동 갱신
            // if (!Application.isPlaying)
            //     SetFillAmount(fillAmount);
            // #endif
        }
    }
}
