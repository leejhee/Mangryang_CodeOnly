using Core.Scripts.Foundation.Define;
using GamePlay.Features.Battle.Scripts.BattleAction;
using GamePlay.Features.Battle.Scripts.Unit.Components.AI;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.Tutorial
{
    [CreateAssetMenu(
        menuName = "GamePlay/Battle/Tutorial Step",
        fileName = "BattleTutorialStep")]
    public class BattleTutorialStep : ScriptableObject
    {
        [Header("ID 및 중복 실행 옵션")] 
        public string stepID;
        public bool triggerOnce = true;
        
        [Header("트리거 타입")]
        public TutorialTriggerEventType triggerType;
        
        [Header("연출 타입")]
        public BattleTutorialViewType viewType = BattleTutorialViewType.Novel;
        
        [Header("라운드 조건 - 0이면 무시")]
        public int requiredRound;
        
        [Header("캐릭터 조건")]
        public bool onlyPlayer;        // 플레이어만
        public bool onlyEnemy;         // 적만
        public int requiredCharId;         
        public int requiredActorTurnCount;
        
        [Header("행동 조건 - ActionCompleted에서만 사용")]
        public bool filterActionType;
        public ActionType requiredCompletedActionType; // 어떤 행동이 끝나고 invoke되어야 하는가?
        
        public bool filterSkillId;
        public int requiredSkillId;

        public bool autoEndTurn;
        
        [Header("Novel 연출이라면 Script의 ID를 작성할 것")]
        public string novelScriptId;   
        
        [Header("Guide 연출이라면 Page의 배열을 작성할 것")]
        public BattleTutorialGuidePage[] guidePages;
        
        [Header("Guide 연출이라면 입력 유도 / 강제 옵션")]
        public bool lockInputDuringStep;  // 이 스텝이 활성화된 동안 입력을 제한할지
        public TutorialGuideTarget requiredClickTarget = TutorialGuideTarget.None;  // 무엇을 클릭하게 할지
        public ActionType guidingActionType; // 입력을 강제하게 할 행동 타입
        
        public string uiTargetKey;
        public Vector2Int requiredCellOffset; // 액터 기준 오프셋
        
        [Header("선택: 이 스텝에서 적 AI를 스크립트로 강제할지")]
        public bool forceEnemyScript = false;
        public EnemyScriptCommand[] enemyCommands;

        [Header("전투 끝나고")] 
        public SystemEnum.eCharType winnerType;
        public bool isRestartBattle;
        public long partyAddIndex;
    }
}