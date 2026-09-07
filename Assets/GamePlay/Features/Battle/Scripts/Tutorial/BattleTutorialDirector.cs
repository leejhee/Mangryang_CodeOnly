using Core.Scripts.Data;
using Core.Scripts.Foundation.Define;
using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Entities.Character;
using GamePlay.Common.Scripts.Novel;
using GamePlay.Features.Battle.Scripts.BattleAction;
using GamePlay.Features.Battle.Scripts.BattleMap;
using GamePlay.Features.Battle.Scripts.BattleTurn;
using GamePlay.Features.Battle.Scripts.UI;
using GamePlay.Features.Battle.Scripts.Unit;
using GamePlay.Features.Battle.Scripts.Unit.Components.AI;
using GamePlay.Features.Explore.Scripts;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.Tutorial
{
    public class BattleTutorialDirector : MonoBehaviour
    {
        #region Singleton
        public static BattleTutorialDirector Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        #endregion

        [Header("전투 튜토리얼에 대한 config 삽입")] 
        [SerializeField] private List<BattleTutorialConfig> configs;
        [SerializeField, ReadOnly] private BattleTutorialConfig currentConfig;
        [SerializeField] private BattleInputGate inputGate;
        
        [SerializeField, ReadOnly] private int currentConfigIndex;  // 몇 번째 config?
        [SerializeField, ReadOnly] private int stepCursor;          // 현재 config 안에서 얼마나 진행?
        
        private readonly HashSet<string> _completedSteps = new();
        private TurnEventContext _lastTurnContext;
        private TurnController _turnController;
        private BattleController _battleController;
        
        public void Init(TurnController turnController, BattleController battleController)
        {
            if (configs == null || configs.Count == 0)
            {
                Debug.LogError("[BattleTutorialDirector] No config assigned.");
                return;
            }
            
            if (!BattleSession.Instance.NeedTutorial)
            {
                Destroy(gameObject);
                return;
            }
            
            currentConfigIndex = BattleSession.Instance.CurrentTutorialIndex;
            currentConfig = configs[currentConfigIndex];
            stepCursor = 0;
            
            _turnController = turnController;
            _battleController = battleController;
            
            _battleController.OnBattleStartAsync += OnBattleStartAsync;
            _battleController.OnBattleEndAsync += OnBattleEndAsync;
            _battleController.OnBattleActionStarted += OnBattleActionStarted;
            _battleController.ActionCompleted += OnActionCompleted;
            _battleController.OnActionPreviewStarted += OnActionPreviewStarted;
            _battleController.OnFocusedAsync += OnFocusedAsync;
            
            _turnController.OnRoundProceedAsync += OnRoundProceedAsync;
            _turnController.OnTurnBeganAsync += OnTurnBeganAsync;
            _turnController.OnTurnEndedAsync += OnTurnEndedAsync;
            
        }

        

        #region Events

        private async UniTask OnBattleStartAsync()
        {
            if (currentConfig == null) return;
            BattleTutorialStep step = GetCurrentStep();
            if (step == null) return;
            if (step.triggerType != TutorialTriggerEventType.BattleStart) return;
            if (!IsStepAvailable(step)) return;

            await ExecuteStep(step);
            stepCursor++;
            
        }

        private async UniTask OnBattleEndAsync(SystemEnum.eCharType winnerType)
        {
            BattleTutorialStep step = GetCurrentStep();
            if (step == null) return;
            if (step.triggerType != TutorialTriggerEventType.BattleEnd) return;
            if (!IsStepAvailable(step)) return;
            
            if (step.winnerType != SystemEnum.eCharType.None &&
                step.winnerType != winnerType)
                return;

            // 파티 합류 처리
            if (step.partyAddIndex != 0)
            {
                CompanionData data = DataManager.Instance.GetData<CompanionData>(step.partyAddIndex);
                if (data != null)
                {
                    CharacterModel model = new(data);
                    BattleController.Instance.PlayerParty.AddMember(model);
                }
            }

            await ExecuteStep(step);
            stepCursor++;

            await HandleTutorialEndBattleAsync(winnerType);
            if (winnerType == SystemEnum.eCharType.Player && BattleSession.Instance != null)
            {
                BattleSession.Instance.MarkTutorialBattleCompleted();
            }
        }

        private async UniTask HandleTutorialEndBattleAsync(SystemEnum.eCharType winnerType)
        {
            var battle = BattleSession.Instance;
            var explore = ExploreSession.Instance;

            if (!battle.IsEndExploreBattle)
                return;
            if (winnerType != SystemEnum.eCharType.Player)
                return;
            if (currentConfigIndex != configs.Count - 1) // 마지막이어야지
                return;
            
            const SystemEnum.Dungeon nextDungeon = SystemEnum.Dungeon.MOUNTAIN_BACK;
            const int nextFloor = 1;
            Party nextParty = battle.PlayerParty; 
            explore.SetNewExplore(nextDungeon, nextFloor, nextParty);
        }
        
        private async UniTask OnRoundProceedAsync(RoundEventContext ctx)
        {
            BattleTutorialStep step = GetCurrentStep();
            if (!step) return;
            if (step.triggerType != TutorialTriggerEventType.RoundStart) return;
            if (!IsStepAvailable(step)) return;

            // 라운드 조건
            if (step.requiredRound > 0 && step.requiredRound != ctx.Round)
                return;

            await ExecuteStep(step);
            stepCursor++;
        }

        private async UniTask OnTurnBeganAsync(TurnEventContext ctx)
        {
            _lastTurnContext = ctx;
            await TryExecuteCurrentStepForTurnEvent(TutorialTriggerEventType.TurnStart, ctx);
        }

        private async UniTask OnFocusedAsync(TurnEventContext ctx)
        {
            _lastTurnContext = ctx;
            await TryExecuteCurrentStepForTurnEvent(TutorialTriggerEventType.TurnFocused, ctx);
        }
        
        private async UniTask OnTurnEndedAsync(TurnEventContext ctx)
        {
            await TryExecuteCurrentStepForTurnEvent(TutorialTriggerEventType.TurnEnd, ctx);
            ClearInputLock();
        }
        
        private void OnActionCompleted(BattleActionBase action, BattleActionResult result)
        {
            HandleActionCompletedAsync(action, result).Forget();
        }
        
        private async UniTask HandleActionCompletedAsync(BattleActionBase action, BattleActionResult result)
        {
            ClearInputLock();
            
            if (!result.ActionSuccess)
                return;
            
            BattleTutorialStep step = GetCurrentStep();
            if (step == null) return;
            if (step.triggerType != TutorialTriggerEventType.ActionCompleted) return;
            if (!IsStepAvailable(step)) return;

            if (!MatchTurnCondition(step, _lastTurnContext)) return;
            if (!MatchActionCondition(step, action, result)) return;
            
            
            await ExecuteStep(step);
            stepCursor++;
            await AutoEndTurnIfNeeded(step);
        }
        
        private void OnActionPreviewStarted(BattleActionContext ctx, BattleActionPreviewData data)
        {
            HandleActionPreviewStartedAsync(ctx, data).Forget();
        }

        private async UniTask HandleActionPreviewStartedAsync(
            BattleActionContext ctx, 
            BattleActionPreviewData data)
        {
            ClearInputLock();
            BattleTutorialStep step = GetCurrentStep();
            if (!step) return;
            if (step.triggerType != TutorialTriggerEventType.ActionPreviewStart) return;
            if (!IsStepAvailable(step)) return;

            // Actor 조건은 TurnContext를 써야 하니까 lastTurnContext 사용
            if (!MatchTurnCondition(step, _lastTurnContext)) return;

            if (step.filterActionType &&
                step.guidingActionType != ctx.battleActionType)
                return;

            await ExecuteStep(step);
            stepCursor++;
        }
        
        private void OnBattleActionStarted()
        {
            BattleTutorialGuideUI.Instance?.Hide();
            BattleTutorialFocusMask.Instance?.Hide();
        }
        
        #endregion
        
        #region Core Methods
        
        private BattleTutorialStep GetCurrentStep()
        {
            if (currentConfig == null || currentConfig.steps == null)
                return null;
            if (stepCursor < 0 || stepCursor >= currentConfig.steps.Length)
                return null;
            return currentConfig.steps[stepCursor];
        }
        
        private async UniTask TryExecuteCurrentStepForTurnEvent(
            TutorialTriggerEventType trigger,
            TurnEventContext ctx)
        {
            BattleTutorialStep step = GetCurrentStep();
            if (step == null) return;
            if (step.triggerType != trigger) return;
            if (!IsStepAvailable(step)) return;
            if (!MatchTurnCondition(step, ctx)) return;

            // 적 AI 강제는 TurnStart에서만
            if (trigger == TutorialTriggerEventType.TurnStart)
                ApplyEnemyScriptForStep(step, ctx);

            await ExecuteStep(step);
            stepCursor++;
        }
        
        
        private bool IsStepAvailable(BattleTutorialStep step)
        {
            if (!step.triggerOnce) return true;
            if (string.IsNullOrEmpty(step.stepID)) return true;

            return !_completedSteps.Contains(step.stepID);
        }

        private void MarkStepCompleted(BattleTutorialStep step)
        {
            if (!step.triggerOnce) return;
            if (string.IsNullOrEmpty(step.stepID)) return;

            _completedSteps.Add(step.stepID);
        }
        
        private void ApplyInputLockForStep(BattleTutorialStep step)
        {
            if (!inputGate || !step.lockInputDuringStep)
                return;
            
            // TODO : 지금 의미 있는지? domain input manager로 승격시킬 생각 할 것.
            inputGate.ApplyTutorialLock(step, _lastTurnContext);
            
            //===================== Masking Input ====================//
            BattleTutorialFocusMask mask = BattleTutorialFocusMask.Instance;
            if (!mask) return;

            CharBase actor = _lastTurnContext?.Actor;
            BattleStageGrid grid = _battleController?.StageGrid;
            
            switch (step.requiredClickTarget)
            {
                case TutorialGuideTarget.None:
                    mask.Hide();
                    break;

                case TutorialGuideTarget.ActorWorld:
                    if (actor && grid)
                    {
                        Vector2Int actorCell = grid.WorldToCell(actor.CharTransform.position);
                        Vector2    worldPos  = grid.CellToWorldCenter(actorCell);
                        Vector2    worldSize = grid.CellSize;

                        mask.ShowHoleAroundWorldPosition(worldPos, worldSize, padding: 0f);
                    }
                    break;

                case TutorialGuideTarget.CellWorld:
                    if (actor && grid)
                    {
                        Vector2Int actorCell = grid.WorldToCell(actor.CharTransform.position);
                        Vector2Int maskCell  = actorCell + step.requiredCellOffset;

                        Vector2 worldPos  = grid.CellToWorldCenter(maskCell);
                        Vector2 worldSize = grid.CellSize;

                        mask.ShowHoleAroundWorldPosition(worldPos, worldSize, padding: 0f);
                    }
                    break;

                // hud를 싱글톤으로 하지 않을 거기 때문에 그냥 find로 해결한다.
                // TODO : viewID로 view를 찾아주는 헬퍼를 UIManager에 추가할 것
                case TutorialGuideTarget.BattleUIButton:
                    GameObject go = GameObject.Find("BattleSceneView");
                    if (!go) return;
                    if (!go.TryGetComponent(out BattleHUDView view)) return;
                    RectTransform rect = view.GetTutorialTarget(step.uiTargetKey);
                    if (rect)
                        mask.ShowHoleForRectTransform(rect, padding: 12f);
                    break;
            }
        }
        
        private void ClearInputLock()
        {
            inputGate?.ClearTutorialLock();
            BattleTutorialGuideUI.Instance?.Hide();
            BattleTutorialFocusMask.Instance?.Hide();
            
        }
        
        private async UniTask ExecuteStep(BattleTutorialStep step)
        {
            if (!step) return;
            InputManager.Instance.DisableBattleInput();
            MarkStepCompleted(step);
            ApplyInputLockForStep(step);
            
            switch (step.viewType)
            {
                case BattleTutorialViewType.Novel:
                    await PlayNovelStepAsync(step);
                    break;
        
                case BattleTutorialViewType.Guide:
                    PlayGuideStepAsync(step).Forget();
                    break;
            }
            InputManager.Instance.EnableBattleInput();
        }
        
        private async UniTask PlayNovelStepAsync(BattleTutorialStep step)
        {
            if (string.IsNullOrEmpty(step.novelScriptId))
                return;

            await NovelDomainPlayer.PlayNovelScript(step.novelScriptId);
        }
        
        private async UniTask PlayGuideStepAsync(BattleTutorialStep step)
        {
            if (step.guidePages == null || step.guidePages.Length == 0)
                return;
    
            BattleTutorialGuideUI ui = BattleTutorialGuideUI.Instance;
            if (!ui)
            {
                Debug.LogWarning("BattleTutorialGuideUI 인스턴스가 없습니다.");
                return;
            }

            CharBase actor = _lastTurnContext?.Actor;

            foreach (BattleTutorialGuidePage page in step.guidePages)
            {
                ui.ShowFixedLeft(page.text);

                await ui.WaitForNextAsync();
            }

            ui.Hide();

        }
        
        private bool MatchTurnCondition(BattleTutorialStep step, TurnEventContext ctx)
        {
            if (step.requiredRound > 0 && step.requiredRound != ctx.Round)
                return false;

            CharBase actor = ctx.Actor;
            if (!actor)
                return false;

            if (step.onlyPlayer && actor.GetCharType() != SystemEnum.eCharType.Player)
                return false;
            if (step.onlyEnemy && actor.GetCharType() == SystemEnum.eCharType.Player)
                return false;

            if (step.requiredCharId != 0 && actor.GetID() != step.requiredCharId)
                return false;

            //if (step.requiredActorTurnCount > 0 && step.requiredActorTurnCount != ctx.ActorTurnCount)
            //    return false;

            return true;
        }

        private bool MatchActionCondition(BattleTutorialStep step, BattleActionBase action, BattleActionResult result)
        {
            if (step.filterActionType && action.ActionType != step.requiredCompletedActionType)
                return false;

            // 필요하면 스킬 ID 같은 조건도 여기서 추가
            return true;
        }
        
        private void ApplyEnemyScriptForStep(BattleTutorialStep step, TurnEventContext ctx)
        {
            if (!step.forceEnemyScript)
                return;

            // 현재 턴 액터
            CharMonster monster = ctx.Actor as CharMonster;
            if (!monster)
                return;

            if (step.enemyCommands == null || step.enemyCommands.Length == 0)
                return;

            List<EnemyScriptCommand> commands = new(step.enemyCommands);

            // 튜토리얼 AI 생성 후 주입
            CharTutorialAI tutorialAI = new(monster, commands);
            monster.OverrideAI(tutorialAI);

            Debug.Log($"[Tutorial] {monster.name} 에 튜토리얼 AI 스크립트 주입 (stepID: {step.stepID})");
        }
        
        private async UniTask AutoEndTurnIfNeeded(BattleTutorialStep step)
        {
            // step 설정상 자동 종료가 아니면 아무 것도 안 함
            if (!step.autoEndTurn)
                return;

            if (_turnController == null)
                return;

            // 플레이어 턴일 때만 강제 턴 종료
            CharBase actor = _lastTurnContext?.Actor;
            if (!actor)
                return;
            if (actor.GetCharType() != SystemEnum.eCharType.Player)
                return;

            await _turnController.ChangeTurn();
        }
        
        #endregion
        
        private void OnDestroy()
        {
            if (_turnController != null)
            {
                _turnController.OnRoundProceedAsync -= OnRoundProceedAsync;
                _turnController.OnTurnBeganAsync -= OnTurnBeganAsync;
                _turnController.OnTurnEndedAsync -= OnTurnEndedAsync;
            }

            if (_battleController != null)
            {
                _battleController.ActionCompleted -= OnActionCompleted;
                _battleController.OnBattleActionStarted -= OnBattleActionStarted;
                _battleController.OnBattleStartAsync -= OnBattleStartAsync;
                _battleController.OnBattleEndAsync -= OnBattleEndAsync;
                _battleController.OnActionPreviewStarted -= OnActionPreviewStarted;
                _battleController.OnFocusedAsync -= OnFocusedAsync;
            }
            
            if (Instance == this)
                Instance = null;
        }
        
        
    }
}