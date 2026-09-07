using System;
using UnityEngine;

namespace GamePlay.Features.Explore.Scripts.Symbol.Encounter
{
    public abstract class EncounterSymbol : MonoBehaviour
    {
        [SerializeField] protected int cellIndex;

        protected virtual bool MarkClearedBeforeEncounter => true;
        
        public void InitializeCellIndex(int index)
        {
            cellIndex = index;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 플레이어만 처리
            var player = other.GetComponentInParent<ExploreController>();
            if (player == null)
                return;

            var session = ExploreSession.Instance;
            if (session == null)
            {
                Debug.LogWarning("[EncounterSymbol] ExploreSession is null");
                return;
            }

            // 이미 처리한 심볼이면 그냥 무시
            if (session.IsSymbolCleared(cellIndex))
                return;

            if (MarkClearedBeforeEncounter)
                session.AddClearedSymbol(cellIndex);

            // 실제 상호작용은 자식이 구현
            OnEncounter(player);
        }

        // 자식이 구현해야 하는 부분
        protected abstract void OnEncounter(ExploreController player);
    }
}
