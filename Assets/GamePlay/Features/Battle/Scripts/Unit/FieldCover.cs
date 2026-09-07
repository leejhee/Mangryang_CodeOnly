using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Contracts;
using GamePlay.Features.Battle.Scripts.BattleMap;
using GamePlay.Features.Battle.Scripts.Tutorial;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace GamePlay.Features.Battle.Scripts.Unit
{
    public class FieldCover : MonoBehaviour, IDamageable, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, Range(0,100)] private float damageProbability = 50f;

        public long NMHP;
        public long NHP;

        private BattleStageGrid _grid;
        private Vector2Int _cell;
        public event Action<FieldCover> Broken;
        
        private void Start()
        {
            NHP = NMHP;
        }

        public void BindGrid(BattleStageGrid grid, Vector2Int cell)
        {
            _grid = grid;
            _cell = cell;
        }

        public async UniTask DamageAsync(long _, CancellationToken ct)
        {
            bool hit = Random.value < (damageProbability * 0.01f) ||
                       BattleTutorialRules.HitRule == TutorialHitRule.AlwaysHit;
            if (hit)
            {
                //대미지 폰트
                var before = NHP;
                NHP --;
                Debug.Log($"{before} -> {NHP}");
                
                GameObject damageText =
                    await BattleFXManager.Instance.PlayFX(FX.DamageFX, transform, new Vector2(1, 1), ct);
                IngameDamageObject txt = damageText.GetComponent<IngameDamageObject>();
                txt.Init(1);
                
                if (NHP <= 0)
                {
                    Broken?.Invoke(this);
                    Destroy(gameObject);
                }
            }
            else
            {
                Debug.Log("가만히있는 반반확률도 못맞추는 허접❤️");
                //감나빗 텍스트 효과
            }
        }

        private void OnDestroy()
        {
            _grid.UnregisterCover(this);
        }

        /// <summary>
        /// 올렸을 때 장애물 타수 정보가 나와야 함.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 텍스트 박스 + 몇대 남았는지
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 텍스트 박스 치워야지
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out CharBase character)) return;
            character.RegisterCoverage(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent(out CharBase character)) return;
            character.ExitCoverage();
        }
    }
}