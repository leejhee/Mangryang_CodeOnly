using Core.Scripts.Foundation.Define;
using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using GamePlay.Features.Battle.Scripts.BattleAction;
using GamePlay.Features.Battle.Scripts.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Threading;
using UIs.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace GamePlay.Features.Battle.Scripts.UI
{
    public class BattleHUDView : MonoBehaviour, IView
    {
        [SerializeField] private Button turnEndButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private CharacterHUD characterHUD;
        [SerializeField] private TurnHUD turnHUD;

        public GameObject Root => gameObject;
        public Transform TurnOrderRoot => turnHUD.transform;

        public event Action TurnEndRequested;
        public event Action MenuRequested;
        public event Action CharacterInfoRequested;
        public event Action<BattleHUDActionIntent> ActionSelected;
        public event Action ActionDeselected;

        private void Awake()
        {
            turnEndButton.onClick.AddListener(HandleTurnEndRequested);
            menuButton.onClick.AddListener(HandleMenuRequested);
            characterHUD.CharacterInfoRequested += HandleCharacterInfoRequested;
            characterHUD.SkillSelected += HandleSkillSelected;
            characterHUD.SkillDeselected += HandleActionDeselected;
            characterHUD.JumpSelected += HandleJumpSelected;
            characterHUD.JumpDeselected += HandleActionDeselected;
            characterHUD.PushSelected += HandlePushSelected;
            characterHUD.PushDeselected += HandleActionDeselected;
        }

        private void OnDestroy()
        {
            turnEndButton.onClick.RemoveListener(HandleTurnEndRequested);
            menuButton.onClick.RemoveListener(HandleMenuRequested);
            characterHUD.CharacterInfoRequested -= HandleCharacterInfoRequested;
            characterHUD.SkillSelected -= HandleSkillSelected;
            characterHUD.SkillDeselected -= HandleActionDeselected;
            characterHUD.JumpSelected -= HandleJumpSelected;
            characterHUD.JumpDeselected -= HandleActionDeselected;
            characterHUD.PushSelected -= HandlePushSelected;
            characterHUD.PushDeselected -= HandleActionDeselected;
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
        public UniTask PlayEnterAsync(CancellationToken ct) => UniTask.CompletedTask;
        public UniTask PlayExitAsync(CancellationToken ct) => UniTask.CompletedTask;

        public void ApplyState(BattleHUDViewState state)
        {
            bool acceptsPlayerInput = state.IsPlayerTurn && !state.IsTurnEnding;
            turnEndButton.interactable = acceptsPlayerInput;
            characterHUD.SetVisible(state.CharacterVisible);
            characterHUD.SetSkillInteractable(acceptsPlayerInput && state.CanUseSkill);
            characterHUD.SetExtraInteractable(acceptsPlayerInput && state.CanUseExtra);

            if (state.SelectedAction == BattleHUDActionKind.None)
                characterHUD.ClearSelection();
        }

        public void SetCharacterContent(
            BattleHUDCharacterData character,
            Sprite portrait,
            IReadOnlyList<CharacterHUD.SkillInfo> skills)
        {
            characterHUD.ShowCharacterHUD(
                character.Name,
                character.CurrentHp,
                character.MaxHp,
                character.CurrentAp,
                character.MaxAp,
                portrait);
            characterHUD.SetSkillButtons(skills);
        }

        public void SetFocusedStat(BattleHUDStatData data)
        {
            if (data.Stat == SystemEnum.eStats.NHP)
                characterHUD.SetHp(data.Result);
            else if (data.Stat == SystemEnum.eStats.NACTION_POINT)
                characterHUD.SetAp(data.Result);
        }

        public void ResetTurnOrder()
        {
            turnHUD.ClearList();
            turnHUD.OnRoundStart();
        }

        public void AddTurnPortrait(TurnPortrait portrait) => turnHUD.AddToTurnList(portrait);
        public void AdvanceTurn() => turnHUD.MoveToNextTurn();
        public void MarkCharacterDead(long actorId) => turnHUD.FindDeadCharacter(actorId);

        public RectTransform GetTutorialTarget(string key)
        {
            switch (key)
            {
                case "JumpButton":
                    return characterHUD.JumpButton.transform as RectTransform;
                case "EndTurnButton":
                    return turnEndButton.transform as RectTransform;
                case "PushButton":
                    return characterHUD.PushButton.transform as RectTransform;
                default:
                    return null;
            }
        }

        private void HandleTurnEndRequested() => TurnEndRequested?.Invoke();
        private void HandleMenuRequested() => MenuRequested?.Invoke();
        private void HandleCharacterInfoRequested() => CharacterInfoRequested?.Invoke();
        private void HandleSkillSelected(int slot) =>
            ActionSelected?.Invoke(new BattleHUDActionIntent(BattleHUDActionKind.Skill, slot));
        private void HandleJumpSelected() =>
            ActionSelected?.Invoke(new BattleHUDActionIntent(BattleHUDActionKind.Jump));
        private void HandlePushSelected() =>
            ActionSelected?.Invoke(new BattleHUDActionIntent(BattleHUDActionKind.Push));
        private void HandleActionDeselected() => ActionDeselected?.Invoke();
        private void HandleActionDeselected(int _) => HandleActionDeselected();
    }

    public enum BattleHUDActionKind
    {
        None,
        Skill,
        Jump,
        Push,
    }

    public readonly struct BattleHUDActionIntent
    {
        public BattleHUDActionKind Kind { get; }
        public int SkillSlotIndex { get; }

        public BattleHUDActionIntent(BattleHUDActionKind kind, int skillSlotIndex = -1)
        {
            Kind = kind;
            SkillSlotIndex = skillSlotIndex;
        }
    }

    /// <summary>
    /// Presenter가 소유하고 View가 그대로 반영하는 일시적인 화면 상태.
    /// 전투 규칙이나 전투 엔티티는 포함하지 않는다.
    /// </summary>
    public sealed class BattleHUDViewState
    {
        public bool IsPlayerTurn { get; set; }
        public bool CharacterVisible { get; set; }
        public bool CanUseSkill { get; set; }
        public bool CanUseExtra { get; set; }
        public bool IsTurnEnding { get; set; }
        public BattleHUDActionKind SelectedAction { get; set; }

        public void ResetForTurn(bool isPlayerTurn)
        {
            IsPlayerTurn = isPlayerTurn;
            CharacterVisible = false;
            CanUseSkill = false;
            CanUseExtra = false;
            IsTurnEnding = false;
            SelectedAction = BattleHUDActionKind.None;
        }
    }

    public class BattleHUDPresenter : PresenterBase<BattleHUDView>
    {
        private const string TurnPortraitAddress = "TurnPortrait";

        private readonly IBattleHUDModel _model;
        private readonly BattleHUDViewState _state = new();
        private BattleHUDActionData _lastActionData;
        private long _currentActorId;
        private bool _hasActionData;
        private bool _turnPortraitsReady;
        private bool _pendingFirstHighlight;
        private int _portraitGeneration;
        private int _characterGeneration;

        public BattleHUDPresenter(IView view, IBattleHUDModel model) : base(view)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public override UniTask EnterAction(CancellationToken token)
        {
            ModelEvents.Subscribe<BattleHUDTurnData>(
                add => _model.TurnChanged += add,
                remove => _model.TurnChanged -= remove,
                OnTurnChanged);
            ModelEvents.Subscribe<BattleHUDActionData>(
                add => _model.TurnActionChanged += add,
                remove => _model.TurnActionChanged -= remove,
                OnTurnActionChanged);
            ModelEvents.Subscribe<BattleHUDTurnOrderData>(
                add => _model.TurnOrderChanged += add,
                remove => _model.TurnOrderChanged -= remove,
                OnTurnOrderChanged);
            ModelEvents.Subscribe<BattleHUDStatData>(
                add => _model.FocusedCharacterStatChanged += add,
                remove => _model.FocusedCharacterStatChanged -= remove,
                View.SetFocusedStat);
            ModelEvents.Subscribe<long>(
                add => _model.CharacterDead += add,
                remove => _model.CharacterDead -= remove,
                View.MarkCharacterDead);

            ViewEvents.Subscribe(
                add => View.TurnEndRequested += add,
                remove => View.TurnEndRequested -= remove,
                OnTurnEndRequested);
            ViewEvents.Subscribe(
                add => View.MenuRequested += add,
                remove => View.MenuRequested -= remove,
                OnMenuRequested);
            ViewEvents.Subscribe(
                add => View.CharacterInfoRequested += add,
                remove => View.CharacterInfoRequested -= remove,
                OnCharacterInfoRequested);
            ViewEvents.Subscribe<BattleHUDActionIntent>(
                add => View.ActionSelected += add,
                remove => View.ActionSelected -= remove,
                OnActionSelected);
            ViewEvents.Subscribe(
                add => View.ActionDeselected += add,
                remove => View.ActionDeselected -= remove,
                OnActionDeselected);

            _state.ResetForTurn(false);
            View.ApplyState(_state);
            _model.Activate();
            return UniTask.CompletedTask;
        }

        public override UniTask ExitAction(CancellationToken token)
        {
            _model.Deactivate();
            _characterGeneration++;
            _portraitGeneration++;
            return UniTask.CompletedTask;
        }

        public override void Dispose()
        {
            _model.Dispose();
            base.Dispose();
        }

        private void OnTurnOrderChanged(BattleHUDTurnOrderData data)
        {
            View.ResetTurnOrder();
            _turnPortraitsReady = false;
            _pendingFirstHighlight = false;
            InstantiateTurnPortraitsAsync(data, ++_portraitGeneration).Forget();
        }

        private async UniTask InstantiateTurnPortraitsAsync(
            BattleHUDTurnOrderData data,
            int generation)
        {
            CancellationToken lifetime = Cts?.Token ?? CancellationToken.None;
            foreach (BattleHUDTurnSlotData slot in data.Slots)
            {
                if (!IsCurrentPortraitGeneration(generation, lifetime))
                    return;
                if (slot.IsDead)
                    continue;

                GameObject portraitObject = await ResourceManager.Instance.InstantiateAsync(
                    TurnPortraitAddress,
                    View.TurnOrderRoot);

                if (!IsCurrentPortraitGeneration(generation, lifetime))
                {
                    ResourceManager.Instance.ReleaseInstance(portraitObject);
                    return;
                }

                TurnPortrait portrait = portraitObject.GetComponent<TurnPortrait>();
                if (!portrait)
                {
                    ResourceManager.Instance.ReleaseInstance(portraitObject);
                    continue;
                }

                Sprite sprite = await ResourceManager.Instance.LoadAsync<Sprite>(slot.PortraitAddress);
                if (!IsCurrentPortraitGeneration(generation, lifetime))
                {
                    ResourceManager.Instance.ReleaseInstance(portraitObject);
                    return;
                }

                portrait.SetPortraitImage(sprite, slot.ActorId);
                View.AddTurnPortrait(portrait);
            }

            if (!IsCurrentPortraitGeneration(generation, lifetime))
                return;

            _turnPortraitsReady = true;
            if (_pendingFirstHighlight)
            {
                _pendingFirstHighlight = false;
                View.AdvanceTurn();
            }
        }

        private bool IsCurrentPortraitGeneration(int generation, CancellationToken token)
        {
            return !token.IsCancellationRequested &&
                   generation == _portraitGeneration &&
                   View;
        }

        private void OnTurnChanged(BattleHUDTurnData data)
        {
            if (!_turnPortraitsReady)
                _pendingFirstHighlight = true;
            else
                View.AdvanceTurn();

            _currentActorId = data.ActorId;
            _state.ResetForTurn(data.IsPlayerTurn);
            if (_hasActionData && _lastActionData.ActorId == data.ActorId)
                ApplyActionData(_lastActionData);
            else
                View.ApplyState(_state);

            int generation = ++_characterGeneration;
            if (data.IsPlayerTurn && data.Character != null)
                ShowCharacterAsync(data.Character, generation).Forget();
        }

        private async UniTask ShowCharacterAsync(BattleHUDCharacterData character, int generation)
        {
            CancellationToken lifetime = Cts?.Token ?? CancellationToken.None;
            Sprite portrait = await ResourceManager.Instance.LoadAsync<Sprite>(character.PortraitAddress);
            if (!IsCurrentCharacterGeneration(generation, lifetime))
                return;

            List<CharacterHUD.SkillInfo> skills = new(character.Skills.Count);
            foreach (BattleHUDSkillData skill in character.Skills)
            {
                Sprite icon = await ResourceManager.Instance.LoadAsync<Sprite>(skill.IconAddress);
                if (!IsCurrentCharacterGeneration(generation, lifetime))
                    return;

                Sprite description = await ResourceManager.Instance.LoadAsync<Sprite>(skill.DescriptionAddress);
                if (!IsCurrentCharacterGeneration(generation, lifetime))
                    return;

                skills.Add(new CharacterHUD.SkillInfo(icon, description));
            }

            View.SetCharacterContent(character, portrait, skills);
            _state.CharacterVisible = true;
            View.ApplyState(_state);
        }

        private bool IsCurrentCharacterGeneration(int generation, CancellationToken token)
        {
            return !token.IsCancellationRequested &&
                   generation == _characterGeneration &&
                   View;
        }

        private void OnTurnActionChanged(BattleHUDActionData data)
        {
            _lastActionData = data;
            _hasActionData = true;
            if (data.ActorId == _currentActorId)
                ApplyActionData(data);
        }

        private void ApplyActionData(BattleHUDActionData data)
        {
            _state.IsPlayerTurn = data.IsPlayerTurn;
            _state.CanUseSkill = data.CanUseSkill;
            _state.CanUseExtra = data.CanUseExtra;
            _state.SelectedAction = BattleHUDActionKind.None;
            View.ApplyState(_state);
        }

        private async void OnTurnEndRequested()
        {
            if (_state.IsTurnEnding || !_state.IsPlayerTurn || !_model.CanEndTurn)
                return;

            _state.IsTurnEnding = true;
            _state.SelectedAction = BattleHUDActionKind.None;
            View.ApplyState(_state);

            CancellationToken lifetime = Cts?.Token ?? CancellationToken.None;
            try
            {
                await _model.EndTurnAsync(lifetime);
            }
            catch (OperationCanceledException)
            {
                // Presenter/UI 수명 종료에 따른 정상 취소.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _state.IsTurnEnding = false;
                if (!lifetime.IsCancellationRequested && View)
                    View.ApplyState(_state);
            }
        }

        private void OnMenuRequested()
        {
            Debug.Log("메뉴창 열기");
        }

        private void OnCharacterInfoRequested()
        {
            UIManager.Instance.ShowViewAsync(ViewID.CharacterInfoPopUpView).Forget();
        }

        private void OnActionSelected(BattleHUDActionIntent intent)
        {
            ActionType actionType = ToActionType(intent.Kind);
            if (!_model.CanStartAction(actionType))
            {
                _state.SelectedAction = BattleHUDActionKind.None;
                View.ApplyState(_state);
                Debug.Log($"[Battle HUD] 지금은 {intent.Kind} 행동을 사용할 수 없습니다.");
                return;
            }

            _state.SelectedAction = intent.Kind;
            View.ApplyState(_state);
            _model.StartPreviewAsync(actionType, intent.SkillSlotIndex).Forget();
        }

        private void OnActionDeselected()
        {
            _state.SelectedAction = BattleHUDActionKind.None;
            View.ApplyState(_state);
            _model.CancelPreview();
        }

        private static ActionType ToActionType(BattleHUDActionKind kind)
        {
            switch (kind)
            {
                case BattleHUDActionKind.Skill:
                    return ActionType.Skill;
                case BattleHUDActionKind.Jump:
                    return ActionType.Jump;
                case BattleHUDActionKind.Push:
                    return ActionType.Push;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }
}
