#if UNITY_EDITOR
using Core.Scripts.Data;
using Core.Scripts.Foundation.Define;
using Core.Scripts.Foundation.SceneUtil;
using Core.Scripts.Managers;
using GamePlay.Common.Scripts.Entities.Character;
using GamePlay.Common.Scripts.Entities.Skills;
using GamePlay.Common.Scripts.Scene;
using GamePlay.Features.Battle.Scripts;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AngelBeat.Editor.Tools
{
    [Serializable]
    public sealed class BattleDebugLaunchConfiguration
    {
        public int dungeon;
        public string stageName;
        public List<BattleDebugUnitConfiguration> units = new();
    }

    [Serializable]
    public sealed class BattleDebugUnitConfiguration
    {
        public long characterId;
        public int characterType;
        public int cellX;
        public int cellY;
        public List<long> skillIds = new();
    }

    /// <summary>
    /// 전역 매니저와 로딩 파이프라인을 유지하면서 디버그 전투로 바로 진입한다.
    /// </summary>
    [InitializeOnLoad]
    public static class BattlePlayModeLauncher
    {
        private const string PendingKey = "AngelBeat.BattleDebugLaunchPending";
        private const string ConfigurationKey = "AngelBeat.BattleDebugLaunchConfiguration";
        private const string BootScenePath = "Assets/Scenes/BootScene.unity";

        static BattlePlayModeLauncher()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/Battle/Play Battle Scene (Debug)")]
        private static void PlayBattleScene()
        {
            BeginPlay(null);
        }

        public static void PlayConfiguredBattle(BattleDebugLaunchConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            BeginPlay(configuration);
        }

        private static void BeginPlay(BattleDebugLaunchConfiguration configuration)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            SessionState.SetBool(PendingKey, true);
            if (configuration == null)
                SessionState.EraseString(ConfigurationKey);
            else
                SessionState.SetString(ConfigurationKey, JsonUtility.ToJson(configuration));

            EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Tools/Battle/Play Battle Scene (Debug)", true)]
        private static bool CanPlayBattleScene() => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
            {
                EditorApplication.update -= WaitForBootCompletion;
                EditorApplication.update += WaitForBootCompletion;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.update -= WaitForBootCompletion;
                SessionState.EraseBool(PendingKey);
                SessionState.EraseString(ConfigurationKey);
            }
        }

        private static void WaitForBootCompletion()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= WaitForBootCompletion;
                return;
            }

            UnityEngine.SceneManagement.Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != nameof(SystemEnum.eScene.LobbyScene) || SceneLoader.IsLoading)
                return;

            EditorApplication.update -= WaitForBootCompletion;
            SessionState.EraseBool(PendingKey);

            string json = SessionState.GetString(ConfigurationKey, string.Empty);
            SessionState.EraseString(ConfigurationKey);
            if (string.IsNullOrEmpty(json))
            {
                GamePlaySceneUtil.LoadBattleScene();
                return;
            }

            try
            {
                BattleDebugLaunchConfiguration configuration =
                    JsonUtility.FromJson<BattleDebugLaunchConfiguration>(json);
                GamePlaySceneUtil.LoadBattleScene(BuildSource(configuration));
            }
            catch (Exception exception)
            {
                Debug.LogError("[Battle Test Setup] 테스트 설정으로 전투를 시작하지 못했습니다.");
                Debug.LogException(exception);
                EditorApplication.isPlaying = false;
            }
        }

        private static ConfiguredBattleDebugSource BuildSource(
            BattleDebugLaunchConfiguration configuration)
        {
            if (configuration == null || configuration.units == null)
                throw new InvalidOperationException("전투 테스트 설정이 비어 있습니다.");

            Party party = new();
            List<BattleDebugUnitSetup> setups = new(configuration.units.Count);

            foreach (BattleDebugUnitConfiguration unit in configuration.units)
            {
                SystemEnum.eCharType characterType = (SystemEnum.eCharType)unit.characterType;
                CharacterModel character = CreateCharacterModel(unit.characterId, characterType);
                EquipSkills(character, unit.skillIds);

                if (characterType == SystemEnum.eCharType.Player)
                    party.AddMember(character);

                setups.Add(new BattleDebugUnitSetup(
                    character,
                    new Vector2Int(unit.cellX, unit.cellY)));
            }

            if (party.partyMembers.Count == 0)
                throw new InvalidOperationException("플레이어 캐릭터가 한 명 이상 필요합니다.");

            return new ConfiguredBattleDebugSource(
                (SystemEnum.Dungeon)configuration.dungeon,
                configuration.stageName,
                party,
                setups);
        }

        private static CharacterModel CreateCharacterModel(
            long characterId,
            SystemEnum.eCharType characterType)
        {
            switch (characterType)
            {
                case SystemEnum.eCharType.Player when characterId == SystemConst.DokkaebiID:
                {
                    DokkaebiData data = DataManager.Instance.GetData<DokkaebiData>(characterId);
                    return data == null
                        ? throw new InvalidOperationException($"도깨비 데이터 {characterId}를 찾지 못했습니다.")
                        : new CharacterModel(data);
                }
                case SystemEnum.eCharType.Player:
                {
                    CompanionData data = DataManager.Instance.GetData<CompanionData>(characterId);
                    return data == null
                        ? throw new InvalidOperationException($"동료 데이터 {characterId}를 찾지 못했습니다.")
                        : new CharacterModel(data, false);
                }
                case SystemEnum.eCharType.Enemy:
                {
                    MonsterData data = DataManager.Instance.GetData<MonsterData>(characterId);
                    return data == null
                        ? throw new InvalidOperationException($"몬스터 데이터 {characterId}를 찾지 못했습니다.")
                        : new CharacterModel(data, false);
                }
                default:
                    throw new InvalidOperationException(
                        $"전투 테스트에서 지원하지 않는 캐릭터 타입입니다: {characterType}");
            }
        }

        private static void EquipSkills(CharacterModel character, IReadOnlyList<long> skillIds)
        {
            if (skillIds == null)
                return;

            if (skillIds.Count > 4)
                throw new InvalidOperationException(
                    $"{character.Name}에게 스킬이 4개를 초과하여 설정되었습니다.");

            HashSet<long> equipped = new();
            foreach (long skillId in skillIds)
            {
                if (!equipped.Add(skillId))
                    throw new InvalidOperationException(
                        $"{character.Name}에게 스킬 {skillId}가 중복 설정되었습니다.");

                SkillModel skill;
                if (character.Index == SystemConst.DokkaebiID)
                {
                    DokkaebiSkillData data = DataManager.Instance.GetData<DokkaebiSkillData>(skillId);
                    if (data == null)
                        throw new InvalidOperationException($"도깨비 스킬 {skillId}를 찾지 못했습니다.");
                    skill = new SkillModel(data);
                }
                else
                {
                    SkillData data = DataManager.Instance.GetData<SkillData>(skillId);
                    if (data == null || data.characterID != character.Index)
                        throw new InvalidOperationException(
                            $"스킬 {skillId}는 {character.Name}({character.Index})의 스킬이 아닙니다.");
                    skill = new SkillModel(data);
                }

                character.AddSkill(skill);
            }
        }
    }
}
#endif
