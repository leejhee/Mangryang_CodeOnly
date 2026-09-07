using GamePlay.Features.Battle.Scripts.Unit;
using GamePlay.Features.Battle.Scripts.Unit.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AngelBeat.EditorTools.Animation
{
    /// <summary>
    /// 전투 캐릭터가 공유하는 상태 그래프와 캐릭터별 클립 Override를 생성합니다.
    /// 생성과 프리팹 적용을 분리하여 단계적으로 검증할 수 있게 합니다.
    /// </summary>
    public static class CharacterAnimatorAssetBuilder
    {
        private const string MenuRoot = "Tools/Battle/Animation/";
        private const string OutputRoot =
            "Assets/GamePlay/Features/Battle/BattleResources/Animations/Common";
        private const string OverrideRoot = OutputRoot + "/Overrides";
        private const string ControllerPath = OutputRoot + "/Character_Base.controller";

        private const string YeonAnimationRoot =
            "Assets/GamePlay/Features/Battle/BattleResources/Animations/Ally/Yeon/";

        private sealed class CharacterSpec
        {
            public readonly string Name;
            public readonly string PrefabPath;
            public readonly string Idle;
            public readonly string Move;
            public readonly string Hit;
            public readonly string Evade;
            public readonly string Push;
            public readonly string JumpOut;
            public readonly string JumpIn;

            public string OverridePath => $"{OverrideRoot}/{Name}.overrideController";

            public CharacterSpec(
                string name,
                string prefabPath,
                string idle,
                string move,
                string hit,
                string evade,
                string push,
                string jumpOut = null,
                string jumpIn = null)
            {
                Name = name;
                PrefabPath = prefabPath;
                Idle = idle;
                Move = move;
                Hit = hit;
                Evade = evade;
                Push = push;
                JumpOut = jumpOut;
                JumpIn = jumpIn;
            }
        }

        private sealed class AnimationSlots
        {
            public AnimationClip Idle;
            public AnimationClip Move;
            public AnimationClip Hit;
            public AnimationClip Evade;
            public AnimationClip Push;
            public AnimationClip JumpOut;
            public AnimationClip JumpIn;

            public IEnumerable<AnimationClip> All
            {
                get
                {
                    yield return Idle;
                    yield return Move;
                    yield return Hit;
                    yield return Evade;
                    yield return Push;
                    yield return JumpOut;
                    yield return JumpIn;
                }
            }
        }

        private static readonly CharacterSpec[] CharacterSpecs =
        {
            new(
                "Yeon",
                "Assets/GamePlay/Features/Battle/BattleResources/Prefabs/CharPrefabs/Ally/Yeon.prefab",
                YeonAnimationRoot + "Yeon_Idle.anim",
                YeonAnimationRoot + "Yeon_Move.anim",
                YeonAnimationRoot + "Yeon_OnAttack.anim",
                YeonAnimationRoot + "Yeon_Evation.anim",
                YeonAnimationRoot + "Yeon_Push.anim",
                YeonAnimationRoot + "Yeon_JumpOut.anim",
                YeonAnimationRoot + "Yeon_JumpIn.anim"),
            new(
                "Seol",
                "Assets/GamePlay/Features/Battle/BattleResources/Prefabs/CharPrefabs/Ally/Seol.prefab",
                "Assets/GamePlay/Features/Battle/BattleResources/Animations/Ally/Seol/NormalAction/Seol_Idle.anim",
                "Assets/GamePlay/Features/Battle/BattleResources/Animations/Ally/Seol/NormalAction/Seol_Dash.anim",
                "Assets/GamePlay/Features/Battle/BattleResources/Animations/Ally/Seol/NormalAction/Seol_Attacked.anim",
                "Assets/GamePlay/Features/Battle/BattleResources/Animations/Ally/Seol/NormalAction/Seol_Evade.anim",
                "Assets/GamePlay/Features/Battle/BattleResources/Animations/Ally/Seol/NormalAction/Seol_Push.anim"),
            new(
                "Baby",
                "Assets/GamePlay/Features/Battle/BattleResources/Prefabs/CharPrefabs/Enemy/Baby.prefab",
                "Assets/GamePlay/Features/Battle/BattleResources/Animations/Enemy/Baby/Baby_Idle.anim",
                "Assets/GamePlay/Features/Battle/BattleResources/Animations/Enemy/Baby/Baby_Dash.anim",
                "Assets/GamePlay/Features/Battle/BattleResources/Animations/Enemy/Baby/Baby_Attacked.anim",
                "Assets/GamePlay/Features/Battle/BattleResources/Animations/Enemy/Baby/Baby_Evade.anim",
                "Assets/GamePlay/Features/Battle/BattleResources/Animations/Enemy/Baby/Baby_Push.anim"),
            new(
                "Jerky",
                "Assets/GamePlay/Features/Battle/BattleResources/Prefabs/CharPrefabs/Enemy/Jerky.prefab",
                "Assets/GamePlay/Features/Battle/BattleResources/Animations/Enemy/Jerky/Jerky_Idle.anim",
                null,
                "Assets/GamePlay/Features/Battle/BattleResources/Animations/Enemy/Jerky/Jerky_OnAttack.anim",
                "Assets/GamePlay/Features/Battle/BattleResources/Animations/Enemy/Jerky/Jerky_Evade.anim",
                null),
            new(
                "Scream",
                "Assets/GamePlay/Features/Battle/BattleResources/Prefabs/CharPrefabs/Enemy/Scream.prefab",
                "Assets/GamePlay/Features/Battle/BattleResources/Animations/Enemy/Scream/Scream_Idle.anim",
                "Assets/GamePlay/Features/Battle/BattleResources/Animations/Enemy/Scream/Scream_Dash.anim",
                "Assets/GamePlay/Features/Battle/BattleResources/Animations/Enemy/Scream/Scream_Attacked.anim",
                "Assets/GamePlay/Features/Battle/BattleResources/Animations/Enemy/Scream/Scream_Evade.anim",
                "Assets/GamePlay/Features/Battle/BattleResources/Animations/Enemy/Scream/Scream_Push.anim")
        };

        [MenuItem(MenuRoot + "1. Build Shared Animator Assets")]
        public static void BuildSharedAnimatorAssets()
        {
            try
            {
                BuildAssets();
                Debug.Log(
                    $"[CharacterAnimatorAssetBuilder] 공통 Controller와 {CharacterSpecs.Length}개 Override 생성 완료.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(MenuRoot + "2. Apply Yeon Pilot")]
        public static void ApplyYeonPilot()
        {
            try
            {
                BuildAssets();
                AssignOverride(CharacterSpecs.First(spec => spec.Name == "Yeon"));
                AssetDatabase.SaveAssets();
                Debug.Log("[CharacterAnimatorAssetBuilder] Yeon 파일럿 적용 완료.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [MenuItem(MenuRoot + "3. Apply All Character Overrides")]
        public static void ApplyAllCharacterOverrides()
        {
            if (!EditorUtility.DisplayDialog(
                    "Apply Character Animator Overrides",
                    "Yeon, Seol, Baby, Jerky, Scream 프리팹의 Animator Controller를 공통 Override로 교체합니다.",
                    "Apply All",
                    "Cancel"))
                return;

            try
            {
                BuildAssets();
                foreach (CharacterSpec spec in CharacterSpecs)
                    AssignOverride(spec);

                AssetDatabase.SaveAssets();
                Debug.Log(
                    $"[CharacterAnimatorAssetBuilder] {CharacterSpecs.Length}개 캐릭터 프리팹 적용 완료.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void BuildAssets()
        {
            EnsureFolder(OutputRoot);
            EnsureFolder(OverrideRoot);

            AnimationSlots baseSlots = LoadSlots(CharacterSpecs[0], useIdleFallback: false);
            AnimatorController controller = GetOrCreateController();
            RebuildController(controller, baseSlots);

            foreach (CharacterSpec spec in CharacterSpecs)
            {
                AnimationSlots characterSlots = LoadSlots(spec, useIdleFallback: true);
                CreateOrUpdateOverride(spec, controller, baseSlots, characterSlots);
            }

            AssetDatabase.SaveAssets();
        }

        private static AnimatorController GetOrCreateController()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller) return controller;

            return AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        private static void RebuildController(
            AnimatorController controller,
            AnimationSlots slots)
        {
            while (controller.layers.Length > 1)
                controller.RemoveLayer(controller.layers.Length - 1);

            while (controller.parameters.Length > 0)
                controller.RemoveParameter(controller.parameters.Length - 1);

            controller.AddParameter(
                CharacterAnimatorContract.MovingParameter,
                AnimatorControllerParameterType.Bool);
            controller.AddParameter(
                CharacterAnimatorContract.HitParameter,
                AnimatorControllerParameterType.Trigger);
            controller.AddParameter(
                CharacterAnimatorContract.EvadeParameter,
                AnimatorControllerParameterType.Trigger);
            controller.AddParameter(
                CharacterAnimatorContract.PushParameter,
                AnimatorControllerParameterType.Bool);
            controller.AddParameter(
                CharacterAnimatorContract.JumpOutParameter,
                AnimatorControllerParameterType.Bool);
            controller.AddParameter(
                CharacterAnimatorContract.JumpInParameter,
                AnimatorControllerParameterType.Bool);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in machine.states.ToArray())
                machine.RemoveState(child.state);
            foreach (ChildAnimatorStateMachine child in machine.stateMachines.ToArray())
                machine.RemoveStateMachine(child.stateMachine);
            foreach (AnimatorTransition transition in machine.entryTransitions.ToArray())
                machine.RemoveEntryTransition(transition);
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions.ToArray())
                machine.RemoveAnyStateTransition(transition);

            AnimatorState idle = AddState(machine, "Idle", slots.Idle, 300f, 40f);
            AnimatorState move = AddState(machine, "Move", slots.Move, 550f, 40f);
            AnimatorState hit = AddState(machine, "Hit", slots.Hit, 300f, -100f);
            AnimatorState evade = AddState(machine, "Evade", slots.Evade, 550f, -100f);
            AnimatorState push = AddState(machine, "Push", slots.Push, 300f, 180f);
            AnimatorState jumpOut = AddState(machine, "JumpOut", slots.JumpOut, 550f, 180f);
            AnimatorState jumpIn = AddState(machine, "JumpIn", slots.JumpIn, 800f, 180f);
            machine.defaultState = idle;

            AddBoolTransition(idle, move, CharacterAnimatorContract.MovingParameter, true, 0.1f);
            AddBoolTransition(move, idle, CharacterAnimatorContract.MovingParameter, false, 0.1f);

            AddTriggerTransition(machine, hit, CharacterAnimatorContract.HitParameter);
            AddExitTimeTransition(hit, idle);
            AddTriggerTransition(machine, evade, CharacterAnimatorContract.EvadeParameter);
            AddExitTimeTransition(evade, idle);

            AddAnyStateBoolTransition(machine, push, CharacterAnimatorContract.PushParameter);
            AddBoolTransition(push, idle, CharacterAnimatorContract.PushParameter, false, 0f);
            AddAnyStateBoolTransition(machine, jumpOut, CharacterAnimatorContract.JumpOutParameter);
            AddBoolTransition(jumpOut, idle, CharacterAnimatorContract.JumpOutParameter, false, 0f);
            AddAnyStateBoolTransition(machine, jumpIn, CharacterAnimatorContract.JumpInParameter);
            AddBoolTransition(jumpIn, idle, CharacterAnimatorContract.JumpInParameter, false, 0f);

            EditorUtility.SetDirty(controller);
        }

        private static AnimatorState AddState(
            AnimatorStateMachine machine,
            string name,
            AnimationClip clip,
            float x,
            float y)
        {
            AnimatorState state = machine.AddState(name, new Vector3(x, y));
            state.motion = clip;
            return state;
        }

        private static void AddBoolTransition(
            AnimatorState source,
            AnimatorState destination,
            string parameter,
            bool expected,
            float duration)
        {
            AnimatorStateTransition transition = source.AddTransition(destination);
            ConfigureImmediateTransition(transition, duration);
            transition.AddCondition(
                expected ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                parameter);
        }

        private static void AddTriggerTransition(
            AnimatorStateMachine machine,
            AnimatorState destination,
            string parameter)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(destination);
            ConfigureImmediateTransition(transition, 0f);
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void AddAnyStateBoolTransition(
            AnimatorStateMachine machine,
            AnimatorState destination,
            string parameter)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(destination);
            ConfigureImmediateTransition(transition, 0f);
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void AddExitTimeTransition(
            AnimatorState source,
            AnimatorState destination)
        {
            AnimatorStateTransition transition = source.AddTransition(destination);
            transition.hasExitTime = true;
            transition.exitTime = 0.95f;
            transition.hasFixedDuration = true;
            transition.duration = 0f;
        }

        private static void ConfigureImmediateTransition(
            AnimatorStateTransition transition,
            float duration)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.offset = 0f;
        }

        private static AnimationSlots LoadSlots(CharacterSpec spec, bool useIdleFallback)
        {
            AnimationClip idle = LoadRequiredClip(spec.Idle, spec.Name, "Idle");
            return new AnimationSlots
            {
                Idle = idle,
                Move = LoadClip(spec.Move, spec.Name, "Move", idle, useIdleFallback),
                Hit = LoadClip(spec.Hit, spec.Name, "Hit", idle, useIdleFallback),
                Evade = LoadClip(spec.Evade, spec.Name, "Evade", idle, useIdleFallback),
                Push = LoadClip(spec.Push, spec.Name, "Push", idle, useIdleFallback),
                JumpOut = LoadClip(spec.JumpOut, spec.Name, "JumpOut", idle, useIdleFallback),
                JumpIn = LoadClip(spec.JumpIn, spec.Name, "JumpIn", idle, useIdleFallback)
            };
        }

        private static AnimationClip LoadRequiredClip(
            string path,
            string characterName,
            string slotName)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (!clip)
                throw new InvalidOperationException(
                    $"[{characterName}] {slotName} AnimationClip을 찾을 수 없습니다: {path}");
            return clip;
        }

        private static AnimationClip LoadClip(
            string path,
            string characterName,
            string slotName,
            AnimationClip idleFallback,
            bool useIdleFallback)
        {
            if (!string.IsNullOrWhiteSpace(path))
                return LoadRequiredClip(path, characterName, slotName);

            if (!useIdleFallback)
                throw new InvalidOperationException(
                    $"공통 Controller 기준 캐릭터의 {slotName} 클립이 비어 있습니다.");

            Debug.LogWarning(
                $"[CharacterAnimatorAssetBuilder] {characterName}.{slotName} 클립이 없어 Idle로 대체합니다.");
            return idleFallback;
        }

        private static void CreateOrUpdateOverride(
            CharacterSpec spec,
            AnimatorController controller,
            AnimationSlots baseSlots,
            AnimationSlots characterSlots)
        {
            AnimatorOverrideController overrideController =
                AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(spec.OverridePath);

            if (!overrideController)
            {
                overrideController = new AnimatorOverrideController
                {
                    name = spec.Name,
                    runtimeAnimatorController = controller
                };
                AssetDatabase.CreateAsset(overrideController, spec.OverridePath);
            }
            else
            {
                overrideController.runtimeAnimatorController = controller;
            }

            AnimationClip[] bases = baseSlots.All.ToArray();
            AnimationClip[] replacements = characterSlots.All.ToArray();
            List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new(bases.Length);
            for (int i = 0; i < bases.Length; i++)
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(bases[i], replacements[i]));

            overrideController.ApplyOverrides(overrides);
            EditorUtility.SetDirty(overrideController);
        }

        private static void AssignOverride(CharacterSpec spec)
        {
            AnimatorOverrideController overrideController =
                AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(spec.OverridePath);
            if (!overrideController)
                throw new InvalidOperationException(
                    $"{spec.Name} Override Controller를 찾을 수 없습니다: {spec.OverridePath}");

            GameObject root = PrefabUtility.LoadPrefabContents(spec.PrefabPath);
            try
            {
                CharBase character = root.GetComponent<CharBase>();
                Animator animator = character ? character.Animator : null;
                if (!animator)
                    throw new InvalidOperationException(
                        $"{spec.Name} 프리팹에서 CharBase.Animator를 찾을 수 없습니다: {spec.PrefabPath}");

                animator.runtimeAnimatorController = overrideController;
                EditorUtility.SetDirty(animator);
                PrefabUtility.SaveAsPrefabAsset(root, spec.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string name = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
