using Core.Scripts.Foundation.Define;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Entities.Character;
using GamePlay.Features.Battle.Scripts.BattleTurn;
using GamePlay.Features.Battle.Scripts.Unit.Components.AI;
using UnityEngine;


namespace GamePlay.Features.Battle.Scripts.Unit
{
    public class CharMonster : CharBase
    {
        private CharSimpleAI _defaultAI;
        private CharacterAI _currentAI; // 현재 사용 중인 AI

        protected override SystemEnum.eCharType CharType => SystemEnum.eCharType.Enemy;
        
        public override async UniTask CharInit(CharacterModel charModel)
        {
            await base.CharInit(charModel);
            BattleCharManager.Instance.SetChar(this);

            _defaultAI = new CharSimpleAI(this);
            _currentAI = _defaultAI;
        }

        /// <summary>
        /// 튜토리얼 쪽에서 AI를 덮어쓸 때 사용
        /// </summary>
        public void OverrideAI(CharacterAI newAI)
        {
            _currentAI = newAI ?? _defaultAI;
        }

        public async UniTask ExecuteAITurn(Turn turn)
        {
            if (_currentAI == null)
            {
                Debug.LogWarning($"{name} AI 미초기화");
                return;
            }

            await _currentAI.ExecuteTurn(turn);
        }
    }
}
