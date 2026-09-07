using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Contracts;
using GamePlay.Features.Battle.Scripts.BattleMap;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GamePlay.Features.Battle.Scripts.Unit
{
    public class FieldObstacle : MonoBehaviour, IDamageable, IPointerEnterHandler, IPointerExitHandler
    {
        // 타수 처리.
        public long NMHP;
        public long NHP;
        
        private BattleStageGrid _grid;
        private Vector2Int _cell;
        public event System.Action<FieldObstacle> Broken;
        
        private void Start()
        {
            NHP = NMHP;
            // hp바도 초기화해줘야한다.
        }

        public void BindGrid(BattleStageGrid grid, Vector2Int cell)
        {
            _grid = grid;
            _cell = cell;
        }
        
        /// <summary>
        /// 대미지 받음
        /// </summary> 
        public async UniTask DamageAsync(long _, CancellationToken ct)
        {
            NHP--;
            // 대미지 글자 연출
            if (NHP <= 0)
            {
                // 등록 해제
                Broken?.Invoke(this);
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            _grid.UnregisterObstacle(this);
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
    }
}