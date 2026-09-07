using GamePlay.Features.Battle.Scripts.BattleAction;
using GamePlay.Features.Battle.Scripts.BattleMap;
using GamePlay.Features.Battle.Scripts.Unit;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts
{
    /// <summary>
    /// 현재는 튜토리얼에서의 입력 통제 전용.
    /// 입력 게이트 역할을 하는 객체.
    /// </summary>
    public class BattleInputGate : MonoBehaviour
    {
        #region Singleton
        public static BattleInputGate Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        #endregion
        
        private bool _inputLocked;
        private Tutorial.TutorialGuideTarget _requiredClickTarget = Tutorial.TutorialGuideTarget.None;
        private Vector2Int _requiredCell;
        private long _requiredActorId;

        private bool _filterActionType;
        private ActionType _requiredActionType;
        
        public bool InputLocked
        {
            get => _inputLocked;
            set
            {
                _inputLocked = value;
                Debug.Log("InputLocked: " + _inputLocked);
            }
        }
        
        #region Public API - Rule
        
        public void ApplyTutorialLock(
            Tutorial.BattleTutorialStep step,
            BattleTurn.TurnEventContext turnCtx)
        {
            if (!step || !step.lockInputDuringStep)
                return;

            _inputLocked = true;
            _requiredClickTarget = step.requiredClickTarget;
            _requiredCell = default;
            _requiredActorId = 0;

            _filterActionType = step.filterActionType;
            _requiredActionType = step.guidingActionType;
            
            if (_requiredClickTarget == Tutorial.TutorialGuideTarget.ActorWorld
                && turnCtx?.Actor)
            {
                // 특정 턴 캐릭터만
                _requiredActorId = turnCtx.Actor.GetID();
            }
            else if (_requiredClickTarget == Tutorial.TutorialGuideTarget.CellWorld
                && turnCtx?.Actor
                && BattleController.Instance)
            {
                // 현재 턴 캐릭터 기준 offset 셀만 클릭 가능
                BattleStageGrid grid = BattleController.Instance.StageGrid;
                if (grid)
                {
                    Vector2Int actorCell = grid.WorldToCell(turnCtx.Actor.transform.position);
                    _requiredCell = actorCell + step.requiredCellOffset;
                }
            }

            Debug.Log($"[BattleInputGate] Tutorial lock applied: target={_requiredClickTarget}, cell={_requiredCell}, actorId={_requiredActorId}");
        }

        public void ClearTutorialLock()
        {
            _inputLocked = false;
            _requiredClickTarget = Tutorial.TutorialGuideTarget.None;
            _requiredCell = default;
            _requiredActorId = 0;

            _filterActionType = false;
            _requiredActionType = default;

            Debug.Log("[BattleInputGate] Tutorial lock cleared.");
        }

        #endregion

        #region Public API - Query

        public bool CanStartAction(ActionType type)
        {
            if (!_inputLocked) return true;
            if (!_filterActionType) return true;

            return type == _requiredActionType;
        }

        public bool CanClickCell(Vector2Int cell)
        {
            if (!_inputLocked) return true;
            if (_requiredClickTarget != Tutorial.TutorialGuideTarget.CellWorld)
                return true;

            return cell == _requiredCell;
        }

        public bool CanClickActor(CharBase actor)
        {
            if (!_inputLocked) return true;
            if (_requiredClickTarget != Tutorial.TutorialGuideTarget.ActorWorld)
                return true;

            if (_requiredActorId == 0 || actor == null)
                return true; // 안전장치

            return actor.GetID() == _requiredActorId;
        }

        #endregion
    }
}