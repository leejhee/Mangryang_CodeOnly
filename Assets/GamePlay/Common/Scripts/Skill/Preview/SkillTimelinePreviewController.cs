using Core.Scripts.Boot;
using Core.Scripts.Data;
using Core.Scripts.Foundation.Define;
using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Contracts;
using GamePlay.Common.Scripts.Entities.Character;
using GamePlay.Common.Scripts.Entities.Skills;
using GamePlay.Common.Scripts.Timeline.Marker;
using GamePlay.Features.Battle.Scripts.BattleAction;
using GamePlay.Features.Battle.Scripts.BattleMap;
using GamePlay.Features.Battle.Scripts.Unit;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace GamePlay.Common.Scripts.Skill.Preview
{
    public enum SkillPreviewDataSource
    {
        SkillData,
        DokkaebiSkillData
    }

    /// <summary>
    /// 스킬 Timeline 프리뷰 씬에서 실제 데이터 초기화와 Marker 실행을 담당합니다.
    /// 게임 전투 코드를 복제하지 않고 CharacterModel, CharInit, SkillBase를 그대로 사용합니다.
    /// </summary>
    public sealed class SkillTimelinePreviewController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private CharBase caster;
        [SerializeField] private CharBase target;
        [SerializeField] private SkillBase skill;
        [SerializeField] private BattleStageGrid grid;

        [Header("Data IDs")]
        [SerializeField] private long casterCharacterId = 30001001;
        [SerializeField] private long targetCharacterId = 20001001;
        [SerializeField] private long skillId = 30101001;
        [SerializeField] private SkillPreviewDataSource skillDataSource;

        [Header("Playback")]
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool disableGravityInPreview = true;

        private CancellationTokenSource _playCts;
        private bool _isInitializing;
        private bool _isPlaying;

        public bool IsInitialized { get; private set; }
        public bool IsPlaying => _isPlaying;
        public string Status => _isInitializing
            ? "Initializing"
            : _isPlaying
                ? "Playing"
                : IsInitialized
                    ? "Ready"
                    : "Not Initialized";

        public void Configure(
            CharBase previewCaster,
            CharBase previewTarget,
            SkillBase previewSkill,
            long casterId,
            long targetId,
            long previewSkillId,
            SkillPreviewDataSource previewSkillDataSource)
        {
            caster = previewCaster;
            target = previewTarget;
            skill = previewSkill;
            casterCharacterId = casterId;
            targetCharacterId = targetId;
            skillId = previewSkillId;
            skillDataSource = previewSkillDataSource;
        }

        private void Awake()
        {
            if (!disableGravityInPreview) return;

            StabilizePreviewPhysics(caster);
            StabilizePreviewPhysics(target);
        }

        private void Start()
        {
            if (initializeOnStart)
                InitializeFromStartAsync().Forget();
        }

        private async UniTaskVoid InitializeFromStartAsync()
        {
            bool initialized = await InitializePreviewAsync();
            if (initialized && playOnStart)
                await PlayPreviewAsync();
        }

        public void InitializePreview()
        {
            InitializePreviewAsync().Forget();
        }

        public void PlayPreview()
        {
            PlayPreviewAsync().Forget();
        }

        public void StopPreview()
        {
            _playCts?.Cancel();

            PlayableDirector director = skill
                ? skill.GetComponent<PlayableDirector>()
                : null;
            if (director && director.state == PlayState.Playing)
                director.Stop();
        }

        public async UniTask<bool> InitializePreviewAsync()
        {
            if (IsInitialized) return true;
            if (_isInitializing) return false;
            if (!ValidateSceneReferences()) return false;

            _isInitializing = true;
            try
            {
                await GameReady.InitializeOnceAsync();

                CharacterModel casterModel = CreateCharacterModel(caster, casterCharacterId);
                CharacterModel targetModel = CreateCharacterModel(target, targetCharacterId);
                SkillModel skillModel = CreateSkillModel(skillId, skillDataSource, out long skillOwnerId);

                if (casterModel == null || targetModel == null || skillModel == null)
                {
                    Debug.LogError(
                        "[SkillTimelinePreview] 프리뷰 데이터 생성에 실패했습니다. 캐릭터/스킬 ID를 확인하세요.",
                        this);
                    return false;
                }

                if (skillOwnerId != casterCharacterId)
                {
                    Debug.LogWarning(
                        $"[SkillTimelinePreview] Skill {skillId} owner는 {skillOwnerId}이지만 " +
                        $"현재 caster는 {casterCharacterId}입니다.",
                        this);
                }

                await caster.CharInit(casterModel);
                await target.CharInit(targetModel);

                skill.SetCharBase(caster);
                skill.Init(skillModel);
                IsInitialized = true;

                Debug.Log(
                    $"[SkillTimelinePreview] Initialized: caster={casterCharacterId}, " +
                    $"target={targetCharacterId}, skill={skillId}.",
                    this);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                return false;
            }
            finally
            {
                _isInitializing = false;
            }
        }

        public async UniTask<bool> PlayPreviewAsync()
        {
            if (_isPlaying)
            {
                Debug.LogWarning("[SkillTimelinePreview] Skill is already playing.", this);
                return false;
            }

            if (!IsInitialized && !await InitializePreviewAsync())
                return false;

            if (!ValidateGridRequirement())
                return false;

            _playCts?.Cancel();
            _playCts?.Dispose();
            _playCts = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy());

            _isPlaying = true;
            try
            {
                List<IDamageable> targets = new() { target };
                BattleSkillTimelineEventSink timelineEventSink = new(caster, targets, grid);
                SkillParameter parameter = new(
                    caster,
                    targets,
                    grid,
                    timelineEventSink);

                bool played = await skill.PlaySkillAsync(parameter, _playCts.Token);
                Debug.Log(
                    played
                        ? $"[SkillTimelinePreview] Skill {skillId} preview completed."
                        : $"[SkillTimelinePreview] Skill {skillId} preview failed.",
                    this);
                return played;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                return false;
            }
            finally
            {
                _isPlaying = false;
            }
        }

        private bool ValidateSceneReferences()
        {
            if (!caster || !target || !skill)
            {
                Debug.LogError(
                    "[SkillTimelinePreview] Caster, Target, Skill 참조가 모두 필요합니다. " +
                    "Preview Window에서 Build / Refresh Preview를 다시 실행하세요.",
                    this);
                return false;
            }

            return true;
        }

        private bool ValidateGridRequirement()
        {
            if (grid) return true;

            PlayableDirector director = skill.GetComponent<PlayableDirector>();
            if (!director || director.playableAsset is not TimelineAsset timeline)
                return true;

            if (!ContainsGridMarker(timeline)) return true;

            Debug.LogError(
                "[SkillTimelinePreview] 이 스킬에는 밀치기 Marker가 있어 BattleStageGrid가 필요합니다. " +
                "현재 단계에서는 Nanta처럼 Grid를 사용하지 않는 스킬부터 테스트하세요.",
                this);
            return false;
        }

        private static bool ContainsGridMarker(TimelineAsset timeline)
        {
            if (timeline.markerTrack && TrackContainsGridMarker(timeline.markerTrack))
                return true;

            foreach (TrackAsset track in timeline.GetRootTracks())
            {
                if (TrackContainsGridMarker(track))
                    return true;
            }

            return false;
        }

        private static bool TrackContainsGridMarker(TrackAsset track)
        {
            foreach (IMarker marker in track.GetMarkers())
            {
                if (marker is SkillPushTargetMarker or SkillPushWithDamageMarker)
                    return true;
            }

            foreach (TrackAsset child in track.GetChildTracks())
            {
                if (TrackContainsGridMarker(child))
                    return true;
            }

            return false;
        }

        private static CharacterModel CreateCharacterModel(CharBase character, long characterId)
        {
            if (character is CharMonster)
            {
                MonsterData data = DataManager.Instance.GetData<MonsterData>(characterId);
                return data == null ? null : new CharacterModel(data, includeSkills: false);
            }

            if (character is CharPlayer)
            {
                if (characterId == SystemConst.DokkaebiID)
                {
                    DokkaebiData dokkaebi = DataManager.Instance.GetData<DokkaebiData>(characterId);
                    return dokkaebi == null ? null : new CharacterModel(dokkaebi);
                }

                CompanionData companion = DataManager.Instance.GetData<CompanionData>(characterId);
                return companion == null
                    ? null
                    : new CharacterModel(companion, includeSkills: false);
            }

            Debug.LogError(
                $"[SkillTimelinePreview] 지원하지 않는 캐릭터 컴포넌트입니다: {character.GetType().Name}",
                character);
            return null;
        }

        private static SkillModel CreateSkillModel(
            long previewSkillId,
            SkillPreviewDataSource source,
            out long ownerId)
        {
            switch (source)
            {
                case SkillPreviewDataSource.SkillData:
                {
                    SkillData data = DataManager.Instance.GetData<SkillData>(previewSkillId);
                    ownerId = data?.characterID ?? 0;
                    return data == null ? null : new SkillModel(data);
                }
                case SkillPreviewDataSource.DokkaebiSkillData:
                {
                    DokkaebiSkillData data = DataManager.Instance.GetData<DokkaebiSkillData>(previewSkillId);
                    ownerId = SystemConst.DokkaebiID;
                    return data == null ? null : new SkillModel(data);
                }
                default:
                    ownerId = 0;
                    return null;
            }
        }

        private static void StabilizePreviewPhysics(CharBase character)
        {
            if (!character) return;

            foreach (Rigidbody2D body in character.GetComponentsInChildren<Rigidbody2D>(true))
            {
                body.gravityScale = 0f;
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
        }

        private void OnDisable()
        {
            _playCts?.Cancel();
            _playCts?.Dispose();
            _playCts = null;
        }
    }
}
