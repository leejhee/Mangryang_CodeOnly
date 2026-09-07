using Core.Scripts.Foundation.Define;
using Cysharp.Threading.Tasks;
using GamePlay.Features.Battle.Scripts.BattleAction;
using GamePlay.Features.Battle.Scripts.BattleTurn;
using GamePlay.Features.Battle.Scripts.Unit;
using System;
using System.Collections.Generic;
using System.Threading;
using UIs.Runtime;

namespace GamePlay.Features.Battle.Scripts.UI
{
    public readonly struct BattleHUDSkillData
    {
        public string IconAddress { get; }
        public string DescriptionAddress { get; }

        public BattleHUDSkillData(string iconAddress, string descriptionAddress)
        {
            IconAddress = iconAddress;
            DescriptionAddress = descriptionAddress;
        }
    }

    public sealed class BattleHUDCharacterData
    {
        public long ActorId { get; }
        public string Name { get; }
        public string PortraitAddress { get; }
        public long CurrentHp { get; }
        public long MaxHp { get; }
        public long CurrentAp { get; }
        public long MaxAp { get; }
        public IReadOnlyList<BattleHUDSkillData> Skills { get; }

        public BattleHUDCharacterData(
            long actorId,
            string name,
            string portraitAddress,
            long currentHp,
            long maxHp,
            long currentAp,
            long maxAp,
            IReadOnlyList<BattleHUDSkillData> skills)
        {
            ActorId = actorId;
            Name = name;
            PortraitAddress = portraitAddress;
            CurrentHp = currentHp;
            MaxHp = maxHp;
            CurrentAp = currentAp;
            MaxAp = maxAp;
            Skills = skills;
        }
    }

    public readonly struct BattleHUDTurnData
    {
        public int Round { get; }
        public long ActorId { get; }
        public bool IsPlayerTurn { get; }
        public BattleHUDCharacterData Character { get; }

        public BattleHUDTurnData(
            int round,
            long actorId,
            bool isPlayerTurn,
            BattleHUDCharacterData character)
        {
            Round = round;
            ActorId = actorId;
            IsPlayerTurn = isPlayerTurn;
            Character = character;
        }
    }

    public readonly struct BattleHUDTurnSlotData
    {
        public long ActorId { get; }
        public string PortraitAddress { get; }
        public bool IsDead { get; }

        public BattleHUDTurnSlotData(long actorId, string portraitAddress, bool isDead)
        {
            ActorId = actorId;
            PortraitAddress = portraitAddress;
            IsDead = isDead;
        }
    }

    public readonly struct BattleHUDTurnOrderData
    {
        public IReadOnlyList<BattleHUDTurnSlotData> Slots { get; }

        public BattleHUDTurnOrderData(IReadOnlyList<BattleHUDTurnSlotData> slots)
        {
            Slots = slots;
        }
    }

    public readonly struct BattleHUDActionData
    {
        public long ActorId { get; }
        public bool IsPlayerTurn { get; }
        public bool CanUseSkill { get; }
        public bool CanUseExtra { get; }

        public BattleHUDActionData(
            long actorId,
            bool isPlayerTurn,
            bool canUseSkill,
            bool canUseExtra)
        {
            ActorId = actorId;
            IsPlayerTurn = isPlayerTurn;
            CanUseSkill = canUseSkill;
            CanUseExtra = canUseExtra;
        }
    }

    public readonly struct BattleHUDStatData
    {
        public SystemEnum.eStats Stat { get; }
        public long Result { get; }

        public BattleHUDStatData(SystemEnum.eStats stat, long result)
        {
            Stat = stat;
            Result = result;
        }
    }

    /// <summary>
    /// Presenter가 전투 구현체를 탐색하지 않도록 HUD에 필요한 조회/명령만 제공한다.
    /// 전투 규칙 검증과 실제 실행 책임은 기존 Controller/TurnController에 남긴다.
    /// </summary>
    public interface IBattleHUDModel : IModel, IDisposable
    {
        event Action<BattleHUDTurnData> TurnChanged;
        event Action<BattleHUDTurnOrderData> TurnOrderChanged;
        event Action<BattleHUDActionData> TurnActionChanged;
        event Action<BattleHUDStatData> FocusedCharacterStatChanged;
        event Action<long> CharacterDead;

        bool CanEndTurn { get; }
        void Activate();
        void Deactivate();
        bool CanStartAction(ActionType type);
        UniTask EndTurnAsync(CancellationToken token);
        UniTask StartPreviewAsync(ActionType type, int skillSlotIndex = -1);
        void CancelPreview();
    }

    /// <summary>
    /// 기존 전투 싱글턴을 HUD 전용 데이터/명령 경계로 변환하는 어댑터.
    /// </summary>
    public sealed class BattleHUDModel : IBattleHUDModel
    {
        private readonly BattleController _controller;
        private readonly BattleCharManager _characterManager;
        private readonly BattleInputGate _inputGate;
        private TurnController _turnController;
        private CharBase _focusedCharacter;
        private bool _active;

        public event Action<BattleHUDTurnData> TurnChanged;
        public event Action<BattleHUDTurnOrderData> TurnOrderChanged;
        public event Action<BattleHUDActionData> TurnActionChanged;
        public event Action<BattleHUDStatData> FocusedCharacterStatChanged;
        public event Action<long> CharacterDead;

        public bool CanEndTurn =>
            _controller &&
            !_controller.IsBattleEnding &&
            !_controller.IsModal &&
            _turnController?.CurrentTurn != null;

        public BattleHUDModel(
            BattleController controller,
            BattleCharManager characterManager,
            BattleInputGate inputGate)
        {
            _controller = controller;
            _characterManager = characterManager;
            _inputGate = inputGate;
        }

        public static BattleHUDModel CreateFromCurrentBattle()
        {
            return new BattleHUDModel(
                BattleController.Instance,
                BattleCharManager.Instance,
                BattleInputGate.Instance);
        }

        public void Activate()
        {
            if (_active)
                return;

            if (!_controller || _controller.TurnController == null)
                throw new InvalidOperationException("Battle HUD requires an initialized BattleController.");

            _active = true;
            _turnController = _controller.TurnController;
            _turnController.OnTurnChanged += HandleTurnChanged;
            _turnController.OnTurnOrderChanged += HandleTurnOrderChanged;
            _turnController.OnCurrentTurnActionChanged += HandleTurnActionChanged;
            _controller.OnCharacterDead += HandleCharacterDead;
        }

        public void Deactivate()
        {
            if (!_active)
                return;

            _active = false;
            if (_turnController != null)
            {
                _turnController.OnTurnChanged -= HandleTurnChanged;
                _turnController.OnTurnOrderChanged -= HandleTurnOrderChanged;
                _turnController.OnCurrentTurnActionChanged -= HandleTurnActionChanged;
            }

            if (_controller)
                _controller.OnCharacterDead -= HandleCharacterDead;

            SetFocusedCharacter(null);
            _turnController = null;
        }

        public bool CanStartAction(ActionType type)
        {
            return _controller &&
                   !_controller.IsBattleEnding &&
                   (_inputGate == null || _inputGate.CanStartAction(type));
        }

        public UniTask EndTurnAsync(CancellationToken token)
        {
            if (!CanEndTurn)
                return UniTask.CompletedTask;

            return _turnController.ChangeTurn().AttachExternalCancellation(token);
        }

        public UniTask StartPreviewAsync(ActionType type, int skillSlotIndex = -1)
        {
            if (!_controller || !CanStartAction(type))
                return UniTask.CompletedTask;

            return _controller.StartPreview(type, skillSlotIndex);
        }

        public void CancelPreview()
        {
            if (_controller)
                _controller.CancelPreview();
        }

        public void Dispose()
        {
            Deactivate();
            TurnChanged = null;
            TurnOrderChanged = null;
            TurnActionChanged = null;
            FocusedCharacterStatChanged = null;
            CharacterDead = null;
        }

        private void HandleTurnChanged(TurnChangedDTO dto)
        {
            CharBase character = _characterManager?.GetFieldChar(dto.ActorId);
            bool isPlayerTurn = dto.Side == SystemEnum.eCharType.Player && character;
            SetFocusedCharacter(isPlayerTurn ? character : null);

            TurnChanged?.Invoke(new BattleHUDTurnData(
                dto.Round,
                dto.ActorId,
                isPlayerTurn,
                isPlayerTurn ? CreateCharacterData(character) : null));
        }

        private void HandleTurnOrderChanged(TurnOrderDTO dto)
        {
            List<BattleHUDTurnSlotData> slots = new(dto.Slots.Count);
            foreach (TurnSlotDTO slot in dto.Slots)
            {
                CharBase character = _characterManager?.GetFieldChar(slot.ActorId);
                if (!character)
                    continue;

                slots.Add(new BattleHUDTurnSlotData(
                    slot.ActorId,
                    character.CharInfo.IconSpriteRoot,
                    slot.IsDead));
            }

            TurnOrderChanged?.Invoke(new BattleHUDTurnOrderData(slots));
        }

        private void HandleTurnActionChanged(TurnActionDTO dto)
        {
            CharBase character = _characterManager?.GetFieldChar(dto.ActorId);
            bool isPlayerTurn = character && character.GetCharType() == SystemEnum.eCharType.Player;
            TurnActionChanged?.Invoke(new BattleHUDActionData(
                dto.ActorId,
                isPlayerTurn,
                dto.CanUseSkill,
                dto.CanUseExtra));
        }

        private void HandleCharacterDead(long actorId)
        {
            CharacterDead?.Invoke(actorId);
        }

        private void SetFocusedCharacter(CharBase character)
        {
            if (_focusedCharacter)
                _focusedCharacter.RuntimeStat.OnStatChanged -= HandleFocusedCharacterStatChanged;

            _focusedCharacter = character;

            if (_focusedCharacter)
                _focusedCharacter.RuntimeStat.OnStatChanged += HandleFocusedCharacterStatChanged;
        }

        private void HandleFocusedCharacterStatChanged(SystemEnum.eStats stat, long delta, long result)
        {
            if (delta != 0)
                FocusedCharacterStatChanged?.Invoke(new BattleHUDStatData(stat, result));
        }

        private static BattleHUDCharacterData CreateCharacterData(CharBase character)
        {
            List<BattleHUDSkillData> skills = new(character.SkillInfo.SkillSlots.Count);
            foreach (var skill in character.SkillInfo.SkillSlots)
                skills.Add(new BattleHUDSkillData(skill.Icon, skill.TooltipName));

            return new BattleHUDCharacterData(
                character.GetID(),
                character.CharInfo.Name,
                $"BattlePanel_{character.CharInfo.IconSpriteRoot}",
                character.RuntimeStat.GetStat(SystemEnum.eStats.NHP),
                character.RuntimeStat.GetStat(SystemEnum.eStats.NMHP),
                character.RuntimeStat.GetStat(SystemEnum.eStats.NACTION_POINT),
                character.RuntimeStat.GetStat(SystemEnum.eStats.NMACTION_POINT),
                skills);
        }
    }
}
