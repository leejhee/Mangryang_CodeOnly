using Core.Scripts.Foundation.Define;
using Core.Scripts.Foundation.SceneUtil;
using Core.Scripts.Foundation.Utils;
using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Contracts;
using GamePlay.Common.Scripts.Entities.Character;
using GamePlay.Common.Scripts.Entities.Skills;
using GamePlay.Common.Scripts.Scene;
using GamePlay.Features.Battle.Scripts.BattleAction;
using GamePlay.Features.Battle.Scripts.BattleMap;
using GamePlay.Features.Battle.Scripts.BattleTurn;
using GamePlay.Features.Battle.Scripts.Unit;
using GamePlay.Features.Explore.Scripts;
using System;
using System.Collections.Generic;
using System.Threading;
using UIs.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace GamePlay.Features.Battle.Scripts
{
    public class BattleController : MonoBehaviour
    {
        #region singleton
        public static BattleController Instance { get; private set; }

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
            UnbindKeywordTurnProcessor();
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (InputManager.Instance.GetBattleQuitQuery())
            {
                //UI 띄워야 함.
                UIManager.Instance.ShowViewAsync(ViewID.BattleQuitQueryView).Forget();
            }
        }

        #endregion
        
        #region Core Field & Property
        
        [SerializeField] private BattleFieldDB battleFieldDB;
        [SerializeField] public AssetReference bgmRef;
        [SerializeField] private float cameraSize = 11;
        [SerializeField] private BattleCameraDriver cameraDriver;
        
        private IBattleSceneSource _sceneSource;
        private IMapLoader _mapLoader;
        private bool _initialized;
        private bool _battleEnding;
        private readonly BattleKeywordTurnProcessor _keywordTurnProcessor = new();
        private CancellationTokenSource _battleLifetimeCts;
        private TurnController _keywordTurnController;
        
        private StageField _battleStage;
        private Party _playerParty;
        private TurnController _turnManager;
        private SystemEnum.eScene _returningScene;
        
        public BattleCameraDriver CameraDriver => cameraDriver;
        
        public Party PlayerParty => _playerParty;
        public SystemEnum.eScene ReturningScene => _returningScene;
        public TurnController TurnController => _turnManager;
        public StageField StageField => _battleStage;
        public BattleStageGrid StageGrid => _battleStage?.GetComponent<BattleStageGrid>();
        public CharBase FocusChar => _turnManager.TurnOwner;
        public bool IsBattleEnding => _battleEnding;
        public bool IsResultViewReady { get; private set; }
        
        
        public event Func<UniTask> OnBattleStartAsync;
        public event Func<TurnEventContext, UniTask> OnFocusedAsync; 
        public event Func<SystemEnum.eCharType, UniTask> OnBattleEndAsync;
        public event Action<BattleActionContext, BattleActionPreviewData> OnActionPreviewStarted;
        public event Action OnBattleActionStarted;
        public event Action<BattleActionBase, BattleActionResult> ActionCompleted;
        public event Action<long> OnCharacterDead;
        
        #endregion
        
        public void SetStageSource(IBattleSceneSource sceneSource) => _sceneSource = sceneSource;

        public void Initialize(
            StageField stage, 
            TurnController turnManager, 
            Party party, 
            SystemEnum.eScene returningScene)
        {
            _battleStage = stage;
            _turnManager = turnManager;
            _playerParty = party;
            _returningScene = returningScene;
            _battleEnding = false;
            
            _initialized = true;
            if(Camera.main != null)
                Camera.main.orthographicSize = cameraSize;

            BindKeywordTurnProcessor();
            _turnManager.OnTurnBeganAsync += HandleCameraFocusAsync;
        }

        private void BindKeywordTurnProcessor()
        {
            UnbindKeywordTurnProcessor();
            _battleLifetimeCts = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy());
            _keywordTurnController = _turnManager;
            _keywordTurnController.OnTurnEndedAsync += HandleKeywordTurnEndedAsync;
        }

        private void UnbindKeywordTurnProcessor()
        {
            if (_keywordTurnController != null)
            {
                _keywordTurnController.OnTurnEndedAsync -= HandleKeywordTurnEndedAsync;
                _keywordTurnController = null;
            }

            _battleLifetimeCts?.Cancel();
            _battleLifetimeCts?.Dispose();
            _battleLifetimeCts = null;
        }

        private async UniTask HandleKeywordTurnEndedAsync(TurnEventContext context)
        {
            CancellationTokenSource lifetimeCts = _battleLifetimeCts;
            if (lifetimeCts == null || lifetimeCts.IsCancellationRequested)
                return;

            try
            {
                await _keywordTurnProcessor.ProcessAsync(context, lifetimeCts.Token);
            }
            catch (OperationCanceledException) when (lifetimeCts.IsCancellationRequested)
            { }
        }
        
        public async UniTask RaiseBattleStartAsync()
        {
            if (OnBattleStartAsync == null)
                return;

            foreach (Delegate d in OnBattleStartAsync.GetInvocationList())
            {
                if (d is Func<UniTask> handler)
                    await handler();
            }
        }

        private async UniTask HandleCameraFocusAsync(TurnEventContext ctx)
        {
            if (cameraDriver && ctx.Actor && ctx.Actor.CharCameraPos)
            {
                await cameraDriver.Focus(ctx.Actor.CharCameraPos, 0.4f);
            }

            if (OnFocusedAsync != null)
            {
                foreach (Delegate d in OnFocusedAsync.GetInvocationList())
                {
                    if (d is Func<TurnEventContext, UniTask> handler)
                    {
                        await handler(ctx);
                    }
                }
            }
        }
        
        #region Battle Action Managing

        private enum BattleActionState {Idle, Preview, Execute}
        private BattleActionState _currentActionState;
        private BattleActionBase _currentActionBase;
        private BattleActionContext _currentActionContext;
        private CancellationTokenSource _actionCts;
        
        
        #region Action Indicator Settings
        [SerializeField] private GameObject indicatorPrefab;
        [SerializeField] private List<BattleActionIndicator> indicatorLists = new();
        [SerializeField] private Color possibleColor;
        [SerializeField] private Color blockedColor;
        #endregion
        
        public bool IsModal => _currentActionState != BattleActionState.Idle;
        public IReadOnlyList<BattleActionIndicator>  IndicatorList => indicatorLists.AsReadOnly();
        
        /// <summary>
        /// BattleAction 시작을 위한 Preview 제시 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="skillSlotIndex"> 몇번 슬롯 눌렀는지 </param>
        public async UniTask StartPreview(ActionType type, int skillSlotIndex=-1)
        {
            if (_currentActionState != BattleActionState.Idle)
            {
                // 기본 상태에서만 선택 가능
                Debug.LogWarning($"[BattleController] Preview Cannot Start in state {_currentActionState}.");
                return;
            } 
            
            #region 턴 행동 가능 여부 계산 
            
            Turn currentTurn = _turnManager.CurrentTurn;
            if (currentTurn == null)
            {
                Debug.LogWarning("[BattleController] 현재 턴이 없습니다.");
                return;
            }

            if (BattleActionRestriction.IsBlocked(FocusChar, type))
            {
                Debug.LogWarning(
                    $"[BattleController] {FocusChar.name}은 이동 불가 키워드로 {type}를 사용할 수 없습니다.",
                    FocusChar);
                AudioClip blockedClip = await ResourceManager.Instance.LoadAsync<AudioClip>("MoveBlocked");
                SoundManager.Instance.Play(blockedClip);
                await FocusChar.BlinkSpriteOnce();
                return;
            }
            
            TurnActionState.ActionCategory category = type.GetActionCategory();
            
            // 이동이 아닌 경우
            if (category == TurnActionState.ActionCategory.SkillAction)
            {
                if (!currentTurn.CanPerformAction(category))
                {
                    Debug.LogWarning($"[BattleController] 이번 턴에는 더 이상 스킬을 사용할 수 없습니다.");
                    return;
                }
            }
            else if (category == TurnActionState.ActionCategory.ExtraAction)
            {
                if (!currentTurn.CanPerformAction(category))
                {
                    Debug.LogWarning($"[BattleController] 이번 턴에는 더 이상 부가행동을 사용할 수 없습니다.");
                    return;
                }
            }
            else
            { 
                // 이동인 경우
                if (!currentTurn.CanPerformAction(category))
                {
                    Debug.Log("[BattleController] 이번 턴에는 더 이상 이동을 할 수 없습니다.");
                    AudioClip clip = await ResourceManager.Instance.LoadAsync<AudioClip>("MoveBlocked");
                    SoundManager.Instance.Play(clip);
                    await FocusChar.BlinkSpriteOnce();
                    return;
                }
            }
            
            #endregion
            
            #region 턴 행동을 위한 베이스 생성
            CancelPreview(); // 깔끔하게 남아있는 필드 초기화
            _currentActionContext = new BattleActionContext
            {
                battleActionType = type, 
                actor = FocusChar, 
                battleField = _battleStage, 
                skillModel = skillSlotIndex == -1 ? 
                    null : FocusChar.SkillInfo.SkillSlots[skillSlotIndex]
            };
            
            // Action을 만들어주고 state를 옮겨준다.
            _currentActionBase = BattleActionFactory.CreateBattleAction(_currentActionContext);
            _actionCts = new CancellationTokenSource();
            #endregion
            
            #region 행동 베이스에 따른 프리뷰 렌더링
            try
            {
                BattleActionPreviewData data = await _currentActionBase.BuildActionPreview(_actionCts.Token);
                RenderPreview(data); // 미리보기를 렌더링한다.
                _currentActionState = BattleActionState.Preview;
                OnActionPreviewStarted?.Invoke(_currentActionContext, data);
            }
            catch (OperationCanceledException) { /*Silence*/ }
            catch (Exception e)
            {
                Debug.LogException(e);
                CancelPreview();
            }
            #endregion
            
        }
        
        /// <summary>
        /// Preview를 취소하고 없애는 메서드
        /// </summary>
        public void CancelPreview()
        {
            if (_actionCts != null)
            {
                _actionCts.Cancel();
                _actionCts.Dispose();
                _actionCts = null;
            }
            
            HideBattleActionPreview();
            _currentActionBase = null;
            _currentActionContext = null;
            _currentActionState = BattleActionState.Idle;
        }

        public void HideBattleActionPreview()
        {
            foreach (BattleActionIndicator go in indicatorLists)
            {
                Destroy(go.gameObject);
            }
            indicatorLists.Clear();
            _battleStage.ShowGridOverlay(false);
        }
        
        private void RenderPreview(BattleActionPreviewData data)
        {
            HideBattleActionPreview(); //혹시 비활성화되어있거나 남아있는 거 제거
            _battleStage.ShowGridOverlay(true);

            foreach (var c in data.MaskedCells) CreateIndicator(c, blocked: false, masked: true);
            foreach (var c in data.PossibleCells) CreateIndicator(c, blocked:false);
            foreach (var c in data.BlockedCells)  CreateIndicator(c, blocked:true);
        }

        private void CreateIndicator(Vector2Int cell, bool blocked, bool masked=false)
        {
            Vector3 pos = _battleStage.CellToWorldCenter(cell);
            GameObject go = Instantiate(indicatorPrefab, pos, Quaternion.identity, _battleStage.transform);
            go.transform.localScale = new Vector3(_battleStage.Grid.cellSize.x, _battleStage.Grid.cellSize.y, 1f);

            var indi = go.GetComponent<BattleActionIndicator>();
            if (!indi) return;
            indicatorLists.Add(indi);
            
            if (masked) return;
            var sr = indi.CellMarkSR;
            if(sr)  sr.color = blocked ? blockedColor : possibleColor;
            
            // 행동마다 콜백이 달라서 이런 형태로 사용
            if (_currentActionContext.battleActionType == ActionType.Skill)
            {
                indi.Init(
                    caster: FocusChar,
                    skill:  _currentActionContext.skillModel,
                    isBlocked: blocked,
                    pivotType: _currentActionContext.skillModel.SkillRange.skillPivot,
                    cell: cell,
                    confirmAction: OnSkillIndicatorConfirm
                );
            }
            else
            {
                indi.InitForSimpleCell(
                    isBlocked: blocked,
                    cell: cell,
                    onClickCell: OnCellClicked
                );
            }
        }

        private async void OnSkillIndicatorConfirm(
            CharBase caster, 
            SkillModel skill, 
            List<IDamageable> targets, 
            Vector2Int cell)
        {
            if (_currentActionState != BattleActionState.Preview) return;
            if (_currentActionBase == null || _currentActionContext == null) return;
            
            Turn currentTurn = _turnManager.CurrentTurn;
            if (!currentTurn.TryUseSkill())
            {
                Debug.LogWarning("스킬 사용 실패: 이미 주요 행동을 사용했습니다.");
                CancelPreview();
                return;
            }
            
            _currentActionContext.TargetCell = cell;
            _currentActionContext.targets =  targets;

            _currentActionState = BattleActionState.Execute;
            
            BattleActionBase finishedAction = _currentActionBase;
            BattleActionResult result = BattleActionResult.Fail(BattleActionResult.ResultReason.BattleActionAborted);
            try
            {
                InputManager.Instance.DisableBattleInput();
                
                HideBattleActionPreview();
                OnBattleActionStarted?.Invoke();
                result = await _currentActionBase.ExecuteAction(_actionCts?.Token ?? CancellationToken.None);
                
            }
            catch (OperationCanceledException)
            { /*Silence*/ }
            catch (Exception ex) { Debug.LogException(ex); }
            finally
            {
                CancelPreview();
                if (!_battleEnding)
                    InputManager.Instance.EnableBattleInput();
                ActionCompleted?.Invoke(finishedAction, result);
                
            }
        }

        private async void OnCellClicked(Vector2Int cell)
        {
            if (_currentActionState != BattleActionState.Preview) return;
            if (_currentActionBase == null || _currentActionContext == null) return;
            
            Turn currentTurn = _turnManager.CurrentTurn;

            if (BattleActionRestriction.IsBlocked(
                    _currentActionContext.actor,
                    _currentActionContext.battleActionType))
            {
                Debug.LogWarning(
                    $"[BattleController] {_currentActionContext.actor.name}의 " +
                    $"{_currentActionContext.battleActionType}가 이동 불가 키워드로 취소됐습니다.",
                    _currentActionContext.actor);
                CancelPreview();
                return;
            }
            
            // 이동 행동 시 이동력 검증 및 소모
            if (_currentActionContext.battleActionType == ActionType.Move)
            {
                BattleStageGrid g = _currentActionContext.battleField.GetComponent<BattleStageGrid>();
                Vector2Int startCell = g.WorldToCell(_currentActionContext.actor.transform.position);
                int moveDistance = Math.Abs(cell.x - startCell.x);
                // 이동 가능 여부
                if (!currentTurn.CanPerformAction(TurnActionState.ActionCategory.Move, moveDistance))
                {
                    CancelPreview();
                    return;
                }
                
                // 이동력 소모
                if (!currentTurn.TryConsumeMove(moveDistance))
                {
                    Debug.LogWarning("이동력 소모 실패");
                    CancelPreview();
                    return;
                }
            }
            else if (_currentActionContext.battleActionType == ActionType.Jump ||
                     _currentActionContext.battleActionType == ActionType.Push)
            {
                if (!currentTurn.TryUseExtra())
                {
                    Debug.LogWarning($"{_currentActionContext.battleActionType} 사용 실패");
                    CancelPreview();
                    return;
                }
            }
            
            _currentActionContext.TargetCell = cell;
            _currentActionState = BattleActionState.Execute;
            
            BattleActionBase finishedAction = _currentActionBase;
            BattleActionResult result = BattleActionResult.Fail(BattleActionResult.ResultReason.BattleActionAborted);

            try
            {
                InputManager.Instance.DisableBattleInput();
                
                HideBattleActionPreview();
                OnBattleActionStarted?.Invoke();
                result = await _currentActionBase.ExecuteAction(_actionCts?.Token ?? CancellationToken.None);
                
            }
            catch (OperationCanceledException)
            { /* Silence */ }
            catch (Exception ex) { Debug.LogException(ex); }
            finally
            {
                CancelPreview();
                if (!_battleEnding)
                    InputManager.Instance.EnableBattleInput();
                ActionCompleted?.Invoke(finishedAction, result);
            }
        }
        
        #endregion

        public void HandleUnitDeath(CharBase unit)
        {
            if(IsModal && _currentActionContext?.actor == unit)
                CancelPreview(); // preview 중에 죽을 일은 없겠지만, 예외처리 용도
            
            // 전투 그리드 적용
            BattleStageGrid grid = _battleStage?.GetComponent<BattleStageGrid>();
            grid?.RemoveUnit(unit);
            
            // 턴 관리도 필요함
            Turn t = _turnManager.FindTurn(unit);
            if (t != null)
            {
                t.KillTurn();
                // 현재 턴이 죽었으면 다음 턴으로 즉시 진행
                if (!_battleEnding && _turnManager.CurrentTurn == t)
                    _ = _turnManager.ChangeTurn();
            }

            OnCharacterDead?.Invoke(unit.GetID());
        }
        
        #region Battle End Management
        
        private enum PostWinFlow
        {
            None,
            NextBattle,     
            ReturnToScene   
        }

        private PostWinFlow _postWinFlow = PostWinFlow.None;
        
        private void RestartBattle()
        {
            SoundManager.Instance.StopBGM();
            GamePlaySceneUtil.LoadBattleScene();
        }

        public void RestartCurrentBattle()
        {
            RestorePartyForRetry(_playerParty);
            RestartBattle();
        }

        private static void RestorePartyForRetry(Party party)
        {
            if (party?.partyMembers == null)
                return;

            foreach (CharacterModel member in party.partyMembers)
            {
                if (member?.BaseStat == null)
                    continue;

                long maxHp = member.BaseStat.GetStat(SystemEnum.eStats.NMHP);
                long hp = member.BaseStat.GetStat(SystemEnum.eStats.NHP);
                member.BaseStat.ChangeStat(SystemEnum.eStats.NHP, maxHp - hp);

                long maxAp = member.BaseStat.GetStat(SystemEnum.eStats.NMACTION_POINT);
                long ap = member.BaseStat.GetStat(SystemEnum.eStats.NACTION_POINT);
                member.BaseStat.ChangeStat(SystemEnum.eStats.NACTION_POINT, maxAp - ap);
            }
        }
        
        /// <summary>
        /// 전투 종료 로직 엔트리 포인트
        /// </summary>
        /// <param name="winnerType">이긴 타입 판정</param>
        public void EndBattle(SystemEnum.eCharType winnerType)
        {
            EndBattleAsync(winnerType).Forget();
        }

        public async UniTask EndBattleAsync(SystemEnum.eCharType winnerType)
        {
            if (_battleEnding)
                return;

            _battleEnding = true;
            IsResultViewReady = false;
            _battleLifetimeCts?.Cancel();
            _turnManager?.Stop();
            CancelPreview();
            InputManager.Instance.DisableBattleInput();

            // 캐릭터 자원 정리
            BattleCharManager.Instance.ClearAll();
            
            // 전투 종료 시의 특정 이벤트 수행
            if (OnBattleEndAsync != null)
            {
                foreach (Delegate d in OnBattleEndAsync.GetInvocationList())
                {
                    if (d is Func<SystemEnum.eCharType, UniTask> handler)
                    {
                        try
                        {
                            await handler(winnerType);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }

            await HandleMultiBattleEndAsync(winnerType);
        }
        
        /// <summary>
        /// 다음 전투 여부 및 승패 여부에 따라서 
        /// </summary>
        /// <returns></returns>
        public bool TryHandleMultiBattleEnd(SystemEnum.eCharType winnerType)
        {
            HandleMultiBattleEndAsync(winnerType).Forget();
            return true;
        }

        private async UniTask HandleMultiBattleEndAsync(SystemEnum.eCharType winnerType)
        {
            BattleSession session = BattleSession.Instance;
            
            if (winnerType == SystemEnum.eCharType.None)
            {
                ReturnToLobbyAndClearSession(session);
                return;
            }
            
            // 승리 시
            if (winnerType == SystemEnum.eCharType.Player)
            {
                // 보상 UI 띄우기
                _postWinFlow = session is { HasNextStage: true }
                    ? PostWinFlow.NextBattle
                    : PostWinFlow.ReturnToScene;
                await UIManager.Instance.ShowViewAsync(ViewID.GameWinView);
                IsResultViewReady = true;
                return;
            }
            
            if (winnerType == SystemEnum.eCharType.Enemy)
            {
                await UIManager.Instance.ShowViewAsync(ViewID.GameOverView);
                IsResultViewReady = true;
                return;
            }

            // 특수한 이유일 경우 재시작
            RestartBattle();
        }
        
        public void OnWinRewardClosed()
        {
            if (!IsResultViewReady)
                return;

            IsResultViewReady = false;
            BattleSession session = BattleSession.Instance;
            bool hasSessionPayload = session is { HasAnyStage: true } && session.PlayerParty != null;
            
            // TODO : 파티의 상태가 이어져야 함.
            if (hasSessionPayload)
                session.UpdateParty(_playerParty);

            PostWinFlow flow = _postWinFlow;
            _postWinFlow = PostWinFlow.None;
            switch (flow)
            {
                case PostWinFlow.NextBattle:
                    // 다음 스테이지 인덱스로 옮기고 배틀 재시작
                    if (hasSessionPayload && session.MoveToNextStage())
                    {
                        RestartBattle();
                    }
                    else
                    {
                        // 혹시라도 다음 스테이지가 없으면 그냥 귀환
                        GoBackToReturningScene(session);
                    }
                    break;

                case PostWinFlow.ReturnToScene:
                    CompleteExploreEncounter(session);
                    GoBackToReturningScene(session);
                    break;
        
                case PostWinFlow.None:
                default:
                    // 멀티 배틀이 아니거나, 별도 계획이 없었던 경우:
                    SceneLoader.LoadSceneWithLoading(ReturningScene);
                    break;
            }

        }
        
        private void ReturnToLobbyAndClearSession(BattleSession session)
        {
            CameraUtil.ReturnMainCamera();
            // TODO : 이 포인트에서 저장할 것
            session?.Clear();
            SceneLoader.LoadSceneWithLoading(SystemEnum.eScene.LobbyScene);
        }
        
        private void GoBackToReturningScene(BattleSession session)
        {
            SystemEnum.eScene scene = ReturningScene;
            
            CameraUtil.ReturnMainCamera();
            
            if (session is { HasAnyStage: true } && session.PlayerParty != null)
            {
                scene = session.ReturningScene;

                // 전투가 끝나고 돌아갈 곳이 탐사인 경우에만 ExploreSession 건드린다.
                if (scene == SystemEnum.eScene.ExploreScene)
                {
                    ExploreSession explore = ExploreSession.Instance;

                    if (!session.IsEndExploreBattle)
                    {
                        explore.SetContinueExplore(
                            session.DungeonName,
                            session.DungeonFloor,
                            session.PlayerParty,
                            explore.PlayerRecentPosition
                        );
                    }
                }

                session.Clear();
            }
            if(scene == SystemEnum.eScene.ExploreScene)
                GamePlaySceneUtil.LoadExploreScene();
            else
                SceneLoader.LoadSceneWithLoading(scene);
        }

        private static void CompleteExploreEncounter(BattleSession session)
        {
            if (session == null ||
                !session.HasAnyStage ||
                session.PlayerParty == null ||
                session.ReturningScene != SystemEnum.eScene.ExploreScene ||
                session.IsEndExploreBattle ||
                session.EncounterCellIndex < 0)
                return;

            ExploreSession.Instance?.AddClearedSymbol(session.EncounterCellIndex);
        }
        
        #endregion
        
#if UNITY_EDITOR
        [ContextMenu("전투 승리 치트")]
        public void GameWinCheat()
        {
            EndBattle(SystemEnum.eCharType.Player);
        }
#endif
        
        #region UI

        public Action<CharacterModel> BattleCharInfoEvent;
        
        public async void ShowCharacterInfoView()
        {
            await UIManager.Instance.ShowViewAsync(ViewID.CharacterInfoPopUpView);
            
            BattleCharInfoEvent?.Invoke(FocusChar.CharInfo);
        }
        
        public void GetSkill(long skillID)
        {
            PlayerParty.AddSkillInCharacter(skillID);
        }
        
        #endregion
    }
    
}


