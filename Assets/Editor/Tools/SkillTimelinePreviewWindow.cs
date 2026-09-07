#if UNITY_EDITOR
using Core.Scripts.Data;
using Core.Scripts.Foundation.Define;
using GamePlay.Common.Scripts.Skill;
using GamePlay.Common.Scripts.Skill.Preview;
using GamePlay.Features.Battle.Scripts;
using GamePlay.Features.Battle.Scripts.Unit;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace AngelBeat.Tools.SkillPreview
{
    /// <summary>
    /// Timeline 스킬 제작 시 캐릭터와 스킬 프리팹을 같은 씬에 배치하고
    /// Animation Track 바인딩까지 준비해 주는 최소 프리뷰 도구입니다.
    /// </summary>
    public sealed class SkillTimelinePreviewWindow : EditorWindow
    {
        private const string PreviewScenePath =
            "Assets/Scenes/Editing/SkillTimelinePreviewScene.unity";
        private const string PreviewRootName = "@SkillTimelinePreview";

        private const string DamageFxPrefabPath =
            "Assets/GamePlay/Features/Battle/BattleResources/Prefabs/FX/Common/DamageObject.prefab";
        private const string DataRoot =
            "Assets/GamePlay/Common/CommonResources/CSV/MEMCSV/";

        [SerializeField] private CharBase casterPrefab;
        [SerializeField] private CharBase targetPrefab;
        [SerializeField] private SkillBase skillPrefab;
        [SerializeField] private long casterCharacterId = 30001001;
        [SerializeField] private long targetCharacterId = 20001001;
        [SerializeField] private long skillId = 30101001;
        [SerializeField] private SkillPreviewDataSource skillDataSource;
        [SerializeField] private Vector3 casterPosition = new(-2f, 0f, 0f);
        [SerializeField] private Vector3 targetPosition = new(2f, 0f, 0f);
        private readonly List<CharacterOption> characterOptions = new();
        private readonly List<SkillOption> skillOptions = new();
        private string selectionError;

        private sealed class CharacterOption
        {
            public long Id;
            public string Name;
            public string Address;
            public string Group;
            public string Label => $"{Name}  [{Id}]";
        }

        private sealed class SkillOption
        {
            public long Id;
            public long OwnerId;
            public string Name;
            public string Address;
            public SkillPreviewDataSource Source;
            public string Label => $"{Name}  [{Id}]";
        }

        private sealed class PickerOption
        {
            public long Id;
            public string Label;
            public string Group;
            public SkillPreviewDataSource SkillSource;
        }

        private sealed class PickerDropdownItem : AdvancedDropdownItem
        {
            public PickerOption Option { get; }

            public PickerDropdownItem(PickerOption option) : base(option.Label)
            {
                Option = option;
            }
        }

        private sealed class SearchablePickerDropdown : AdvancedDropdown
        {
            private readonly IReadOnlyList<PickerOption> options;
            private readonly System.Action<PickerOption> onSelected;

            public SearchablePickerDropdown(
                IReadOnlyList<PickerOption> options,
                System.Action<PickerOption> onSelected)
                : base(new AdvancedDropdownState())
            {
                this.options = options;
                this.onSelected = onSelected;
                minimumSize = new Vector2(340f, 320f);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                AdvancedDropdownItem root = new("Select");
                Dictionary<string, AdvancedDropdownItem> groups = new();

                foreach (PickerOption option in options)
                {
                    string groupName = string.IsNullOrEmpty(option.Group) ? "Items" : option.Group;
                    if (!groups.TryGetValue(groupName, out AdvancedDropdownItem group))
                    {
                        group = new AdvancedDropdownItem(groupName);
                        groups.Add(groupName, group);
                        root.AddChild(group);
                    }

                    group.AddChild(new PickerDropdownItem(option));
                }

                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item is PickerDropdownItem selected)
                    onSelected?.Invoke(selected.Option);
            }
        }

        [MenuItem("Tools/Battle/Skill Timeline Preview")]
        public static void ShowWindow()
        {
            SkillTimelinePreviewWindow window = GetWindow<SkillTimelinePreviewWindow>();
            window.titleContent = new GUIContent("Skill Timeline Preview");
            window.minSize = new Vector2(390f, 330f);
            window.Show();
        }

        private void OnEnable()
        {
            ReloadCatalog();
            EnsureValidSelection();
            ResolveSelectionFromIds(false);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Skill Timeline Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "검색 가능한 선택 창에서 Caster, 해당 Caster의 Skill, Target을 고릅니다. " +
                "Timeline의 Animation Track은 시전자 Animator에 연결됩니다. " +
                "Edit Mode에서는 Timeline을 스크러빙하고, Play Mode에서는 실제 데이터를 " +
                "초기화하여 Marker까지 자동 재생합니다. " +
                "현재 런타임 검증은 Grid가 필요 없는 스킬부터 지원합니다.",
                MessageType.Info);

            EditorGUILayout.Space(6f);
            DrawCharacterPicker("Caster", casterCharacterId, option =>
            {
                casterCharacterId = option.Id;
                SelectFirstOwnedSkillIfNeeded();
                ResolveSelectionFromIds(false);
            });
            DrawSkillPicker();
            DrawCharacterPicker("Target", targetCharacterId, option =>
            {
                targetCharacterId = option.Id;
                ResolveSelectionFromIds(false);
            });

            if (GUILayout.Button("Reload CSV / Addressables"))
            {
                ReloadCatalog();
                EnsureValidSelection();
                ResolveSelectionFromIds(true);
            }

            EditorGUILayout.Space(4f);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Resolved Caster", casterPrefab, typeof(CharBase), false);
                EditorGUILayout.ObjectField("Resolved Target", targetPrefab, typeof(CharBase), false);
                EditorGUILayout.ObjectField("Resolved Skill", skillPrefab, typeof(SkillBase), false);
            }

            if (!string.IsNullOrEmpty(selectionError))
                EditorGUILayout.HelpBox(selectionError, MessageType.Error);

            EditorGUILayout.Space(4f);
            casterPosition = EditorGUILayout.Vector3Field("Caster Position", casterPosition);
            targetPosition = EditorGUILayout.Vector3Field("Target Position", targetPosition);

            EditorGUILayout.Space(10f);
            if (GUILayout.Button("1. Open Preview Scene", GUILayout.Height(30f)))
                OpenOrCreatePreviewScene();

            using (new EditorGUI.DisabledScope(!casterPrefab || !skillPrefab))
            {
                if (GUILayout.Button("2. Build / Refresh Preview", GUILayout.Height(34f)))
                    BuildPreview();
            }

            if (!casterPrefab || !skillPrefab)
            {
                EditorGUILayout.HelpBox(
                    "입력한 ID에 해당하는 Caster, Target, Skill Prefab을 모두 찾을 수 있어야 합니다.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Build 후 생성된 스킬 오브젝트와 Timeline 창이 자동으로 선택됩니다.",
                EditorStyles.wordWrappedMiniLabel);
        }

        private void BuildPreview()
        {
            if (!ResolveSelectionFromIds(true))
                return;

            if (!ValidatePrefab(casterPrefab, "Caster") ||
                !ValidatePrefab(skillPrefab, "Skill") ||
                (targetPrefab && !ValidatePrefab(targetPrefab, "Target")))
            {
                return;
            }

            UnityScene scene = OpenOrCreatePreviewScene();
            if (!scene.IsValid()) return;

            GameObject oldRoot = FindRoot(scene, PreviewRootName);
            if (oldRoot)
                Undo.DestroyObjectImmediate(oldRoot);

            GameObject previewRoot = new(PreviewRootName);
            SceneManager.MoveGameObjectToScene(previewRoot, scene);
            Undo.RegisterCreatedObjectUndo(previewRoot, "Create Skill Timeline Preview");

            CharBase caster = InstantiatePrefab(casterPrefab, scene, previewRoot.transform);
            caster.gameObject.name = $"Caster_{casterPrefab.name}";
            caster.transform.position = casterPosition;

            CharBase target = null;
            if (targetPrefab)
            {
                target = InstantiatePrefab(targetPrefab, scene, previewRoot.transform);
                target.gameObject.name = $"Target_{targetPrefab.name}";
                target.transform.position = targetPosition;
            }

            Transform skillParent = caster.SkillRoot
                ? caster.SkillRoot.transform
                : caster.transform;
            SkillBase skill = InstantiatePrefab(skillPrefab, scene, skillParent);
            skill.gameObject.name = $"Preview_{skillPrefab.name}";
            skill.transform.localPosition = Vector3.zero;
            skill.SetCharBase(caster);

            PlayableDirector director = skill.GetComponent<PlayableDirector>();
            if (!director || !director.playableAsset)
            {
                Debug.LogError(
                    $"[SkillTimelinePreview] '{skillPrefab.name}'에 유효한 PlayableDirector/Timeline이 없습니다.",
                    skill);
                return;
            }

            Animator animator = caster.Animator
                ? caster.Animator
                : caster.GetComponentInChildren<Animator>(true);
            if (!animator)
            {
                Debug.LogError(
                    $"[SkillTimelinePreview] '{casterPrefab.name}'에서 Animator를 찾을 수 없습니다.",
                    caster);
                return;
            }

            int bindingCount = BindAnimatorOutputs(director, animator);

            SkillTimelinePreviewController controller =
                Undo.AddComponent<SkillTimelinePreviewController>(previewRoot);
            controller.Configure(
                caster,
                target,
                skill,
                casterCharacterId,
                targetCharacterId,
                skillId,
                skillDataSource);
            EditorUtility.SetDirty(controller);

            CreateFxController(previewRoot.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = skill.gameObject;
            EditorGUIUtility.PingObject(skill.gameObject);
            EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");

            Debug.Log(
                $"[SkillTimelinePreview] Ready: {casterPrefab.name} + {skillPrefab.name} " +
                $"(Animation Track bindings: {bindingCount}).",
                skill);
        }

        private static void CreateFxController(Transform parent)
        {
            GameObject fxController = new("FXController");
            fxController.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(fxController, "Create Preview FX Controller");

            BattleFXManager manager = Undo.AddComponent<BattleFXManager>(fxController);
            SerializedObject serializedManager = new(manager);
            SerializedProperty entries = serializedManager.FindProperty("entries");
            entries.arraySize = 1;

            SerializedProperty entry = entries.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("fx").enumValueIndex = (int)FX.DamageFX;

            string damageFxGuid = AssetDatabase.AssetPathToGUID(DamageFxPrefabPath);
            SerializedProperty reference = entry.FindPropertyRelative("fxReference");
            reference.FindPropertyRelative("m_AssetGUID").stringValue = damageFxGuid;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
        }

        private static int BindAnimatorOutputs(PlayableDirector director, Animator animator)
        {
            int bindingCount = 0;
            foreach (PlayableBinding output in director.playableAsset.outputs)
            {
                System.Type targetType = output.outputTargetType;
                if (targetType == null || !typeof(Animator).IsAssignableFrom(targetType))
                    continue;

                if (!output.sourceObject)
                {
                    Debug.LogWarning(
                        $"[SkillTimelinePreview] Animation output '{output.streamName}'에 source track이 없습니다.",
                        director);
                    continue;
                }

                director.SetGenericBinding(output.sourceObject, animator);
                bindingCount++;
            }

            return bindingCount;
        }

        private static UnityScene OpenOrCreatePreviewScene()
        {
            UnityScene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.path == PreviewScenePath)
                return activeScene;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return default;

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(PreviewScenePath);
            if (sceneAsset)
                return EditorSceneManager.OpenScene(PreviewScenePath, OpenSceneMode.Single);

            UnityScene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            ConfigureDefaultCamera(scene);
            EditorSceneManager.SaveScene(scene, PreviewScenePath);
            AssetDatabase.SaveAssets();
            return scene;
        }

        private static void ConfigureDefaultCamera(UnityScene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Camera camera = root.GetComponent<Camera>();
                if (!camera) continue;

                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
                camera.transform.position = new Vector3(0f, 0f, -10f);
                break;
            }
        }

        private static T InstantiatePrefab<T>(T prefab, UnityScene scene, Transform parent)
            where T : Component
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, scene);
            Undo.RegisterCreatedObjectUndo(instance, $"Instantiate {prefab.name}");
            instance.transform.SetParent(parent, true);
            return instance.GetComponent<T>();
        }

        private static bool ValidatePrefab(Component prefab, string label)
        {
            if (!prefab)
            {
                Debug.LogError($"[SkillTimelinePreview] {label} Prefab이 지정되지 않았습니다.");
                return false;
            }

            if (PrefabUtility.IsPartOfPrefabAsset(prefab.gameObject)) return true;

            Debug.LogError(
                $"[SkillTimelinePreview] {label}에는 씬 오브젝트가 아니라 Prefab Asset을 지정해야 합니다.",
                prefab);
            return false;
        }

        private static T LoadPrefabComponent<T>(string path) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab ? prefab.GetComponent<T>() : null;
        }

        private bool ResolveSelectionFromIds(bool logError)
        {
            selectionError = null;

            CharacterOption caster = FindCharacter(casterCharacterId);
            CharacterOption target = FindCharacter(targetCharacterId);
            SkillOption selectedSkill = FindSkill(skillId, skillDataSource);

            if (caster == null)
                return FailSelection($"Caster ID {casterCharacterId}의 캐릭터 데이터를 찾을 수 없습니다.", logError);
            if (target == null)
                return FailSelection($"Target ID {targetCharacterId}의 캐릭터 데이터를 찾을 수 없습니다.", logError);
            if (selectedSkill == null)
                return FailSelection($"Skill ID {skillId}의 스킬 데이터를 찾을 수 없습니다.", logError);
            if (selectedSkill.OwnerId != casterCharacterId)
            {
                return FailSelection(
                    $"선택한 Skill은 Caster {casterCharacterId}의 스킬이 아닙니다.",
                    logError);
            }

            if (!TryLoadAddressablePrefabComponent(caster.Address, out casterPrefab, out string casterError))
                return FailSelection($"Caster '{caster.Name}' 로드 실패: {casterError}", logError);
            if (!TryLoadAddressablePrefabComponent(target.Address, out targetPrefab, out string targetError))
                return FailSelection($"Target '{target.Name}' 로드 실패: {targetError}", logError);
            if (!TryLoadAddressablePrefabComponent(selectedSkill.Address, out skillPrefab, out string skillError))
                return FailSelection($"Skill '{selectedSkill.Name}' 로드 실패: {skillError}", logError);

            Repaint();
            return true;
        }

        private bool FailSelection(string message, bool logError)
        {
            casterPrefab = null;
            targetPrefab = null;
            skillPrefab = null;
            selectionError = message;
            if (logError)
                Debug.LogError($"[SkillTimelinePreview] {message}");
            Repaint();
            return false;
        }

        private void ReloadCatalog()
        {
            characterOptions.Clear();
            skillOptions.Clear();

            foreach (SheetData row in LoadTable<MonsterData>().Values)
            {
                MonsterData data = (MonsterData)row;
                characterOptions.Add(new CharacterOption
                {
                    Id = data.index,
                    Name = data.charName,
                    Address = data.charPrefabName,
                    Group = "Monster"
                });
            }

            foreach (SheetData row in LoadTable<CompanionData>().Values)
            {
                CompanionData data = (CompanionData)row;
                characterOptions.Add(new CharacterOption
                {
                    Id = data.index,
                    Name = string.IsNullOrEmpty(data.charName) ? data.companionName : data.charName,
                    Address = data.charPrefabName,
                    Group = "Companion"
                });
            }

            foreach (SheetData row in LoadTable<DokkaebiData>().Values)
            {
                DokkaebiData data = (DokkaebiData)row;
                characterOptions.Add(new CharacterOption
                {
                    Id = data.index,
                    Name = data.DokkaebiName,
                    Address = data.PrefabRoot,
                    Group = "Dokkaebi"
                });
            }

            foreach (SheetData row in LoadTable<SkillData>().Values)
            {
                SkillData data = (SkillData)row;
                skillOptions.Add(new SkillOption
                {
                    Id = data.index,
                    OwnerId = data.characterID,
                    Name = data.skillName,
                    Address = data.skillTimeLine,
                    Source = SkillPreviewDataSource.SkillData
                });
            }

            foreach (SheetData row in LoadTable<DokkaebiSkillData>().Values)
            {
                DokkaebiSkillData data = (DokkaebiSkillData)row;
                skillOptions.Add(new SkillOption
                {
                    Id = data.index,
                    OwnerId = SystemConst.DokkaebiID,
                    Name = data.skillName,
                    Address = data.skillTimeLine,
                    Source = SkillPreviewDataSource.DokkaebiSkillData
                });
            }

            characterOptions.Sort((left, right) => left.Id.CompareTo(right.Id));
            skillOptions.Sort((left, right) => left.Id.CompareTo(right.Id));
        }

        private void EnsureValidSelection()
        {
            if (FindCharacter(casterCharacterId) == null && characterOptions.Count > 0)
                casterCharacterId = characterOptions[0].Id;

            SelectFirstOwnedSkillIfNeeded();

            if (FindCharacter(targetCharacterId) == null && characterOptions.Count > 0)
                targetCharacterId = characterOptions[0].Id;
        }

        private void SelectFirstOwnedSkillIfNeeded()
        {
            SkillOption current = FindSkill(skillId, skillDataSource);
            if (current != null && current.OwnerId == casterCharacterId)
                return;

            foreach (SkillOption option in skillOptions)
            {
                if (option.OwnerId != casterCharacterId) continue;
                skillId = option.Id;
                skillDataSource = option.Source;
                return;
            }

            skillId = 0;
        }

        private void DrawCharacterPicker(
            string label,
            long selectedId,
            System.Action<PickerOption> onSelected)
        {
            List<PickerOption> options = new();
            foreach (CharacterOption character in characterOptions)
            {
                options.Add(new PickerOption
                {
                    Id = character.Id,
                    Label = character.Label,
                    Group = character.Group
                });
            }

            CharacterOption selected = FindCharacter(selectedId);
            DrawSearchablePicker(label, selected?.Label ?? "Select Character", options, onSelected);
        }

        private void DrawSkillPicker()
        {
            List<PickerOption> options = new();
            foreach (SkillOption skill in skillOptions)
            {
                if (skill.OwnerId != casterCharacterId) continue;
                options.Add(new PickerOption
                {
                    Id = skill.Id,
                    Label = skill.Label,
                    Group = "Skills",
                    SkillSource = skill.Source
                });
            }

            SkillOption selected = FindSkill(skillId, skillDataSource);
            DrawSearchablePicker(
                "Skill",
                selected?.Label ?? "No Skill",
                options,
                option =>
                {
                    skillId = option.Id;
                    skillDataSource = option.SkillSource;
                    ResolveSelectionFromIds(false);
                });
        }

        private static void DrawSearchablePicker(
            string label,
            string selectedLabel,
            IReadOnlyList<PickerOption> options,
            System.Action<PickerOption> onSelected)
        {
            Rect row = EditorGUILayout.GetControlRect();
            Rect buttonRect = EditorGUI.PrefixLabel(row, new GUIContent(label));
            if (!GUI.Button(buttonRect, selectedLabel, EditorStyles.popup)) return;

            SearchablePickerDropdown dropdown = new(options, onSelected);
            dropdown.Show(buttonRect);
        }

        private CharacterOption FindCharacter(long id)
        {
            foreach (CharacterOption option in characterOptions)
            {
                if (option.Id == id) return option;
            }

            return null;
        }

        private SkillOption FindSkill(long id, SkillPreviewDataSource source)
        {
            foreach (SkillOption option in skillOptions)
            {
                if (option.Id == id && option.Source == source) return option;
            }

            return null;
        }

        private static Dictionary<long, SheetData> LoadTable<T>() where T : SheetData, new()
        {
            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>($"{DataRoot}{typeof(T).Name}.csv");
            if (!csv) return new Dictionary<long, SheetData>();

            return new T()
                .ParseAsync(csv.text, default)
                .GetAwaiter()
                .GetResult();
        }

        private static bool TryLoadAddressablePrefabComponent<T>(
            string address,
            out T component,
            out string error) where T : Component
        {
            component = null;
            error = null;

            if (string.IsNullOrWhiteSpace(address))
            {
                error = "Address 값이 비어 있습니다.";
                return false;
            }

            address = address.Trim();
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                error = "활성 AddressableAssetSettings를 불러오지 못했습니다.";
                return false;
            }

            bool foundAddress = false;
            var failures = new List<string>();
            foreach (var group in settings.groups)
            {
                if (!group) continue;
                foreach (var entry in group.entries)
                {
                    if (entry.address != address) continue;
                    foundAddress = true;

                    string path = entry.AssetPath;
                    if (string.IsNullOrWhiteSpace(path))
                        path = AssetDatabase.GUIDToAssetPath(entry.guid);

                    if (string.IsNullOrWhiteSpace(path))
                    {
                        failures.Add($"그룹 '{group.Name}'의 엔트리 GUID '{entry.guid}'에서 AssetPath를 해석하지 못했습니다.");
                        continue;
                    }

                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (!prefab)
                    {
                        failures.Add($"'{path}'를 GameObject Prefab으로 불러오지 못했습니다.");
                        continue;
                    }

                    component = prefab.GetComponent<T>();
                    if (component)
                        return true;

                    Component[] attached = prefab.GetComponents<Component>();
                    string componentNames = string.Join(", ", attached
                        .Where(item => item)
                        .Select(item => item.GetType().Name));
                    failures.Add(
                        $"'{path}'의 루트 '{prefab.name}'에 {typeof(T).Name} 컴포넌트가 없습니다. " +
                        $"현재 컴포넌트: [{componentNames}]");
                }
            }

            if (!foundAddress)
            {
                error = $"Address '{address}'가 활성 Addressables Settings에 없습니다.";
                return false;
            }

            error = $"Address '{address}'는 찾았지만 사용할 수 있는 Prefab이 아닙니다. " +
                    string.Join(" | ", failures);
            return false;
        }

        private static GameObject FindRoot(UnityScene scene, string rootName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == rootName)
                    return root;
            }

            return null;
        }
    }

    [CustomEditor(typeof(SkillTimelinePreviewController))]
    public sealed class SkillTimelinePreviewControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SkillTimelinePreviewController controller =
                (SkillTimelinePreviewController)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField($"Status: {controller.Status}", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Initialize Preview"))
                    controller.InitializePreview();

                if (GUILayout.Button("Play Skill"))
                    controller.PlayPreview();

                if (GUILayout.Button("Stop"))
                    controller.StopPreview();
            }
        }
    }
}
#endif
