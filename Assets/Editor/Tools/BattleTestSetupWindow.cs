#if UNITY_EDITOR
using Core.Scripts.Data;
using Core.Scripts.Foundation.Define;
using GamePlay.Features.Battle.Scripts.BattleMap;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AngelBeat.Editor.Tools
{
    /// <summary>
    /// 스테이지 프리팹을 변경하지 않고 캐릭터, 스킬, 시작 셀을 구성하여
    /// 실제 BattleScene 파이프라인으로 진입하는 에디터 전용 도구.
    /// </summary>
    public sealed class BattleTestSetupWindow : EditorWindow
    {
        private const string DataRoot = "Assets/GamePlay/Common/CommonResources/CSV/MEMCSV/";
        private const string BattleFieldDbPath =
            "Assets/GamePlay/Features/Battle/BattleResources/StageResources/BattleFieldGroup/BattleFieldDB.asset";
        private const string EditorPrefsKey = "AngelBeat.BattleTestSetup.State.v1";

        private readonly List<CharacterOption> _characters = new();
        private readonly List<SkillOption> _skills = new();
        private readonly List<StageOption> _stages = new();

        [SerializeField] private BattleDebugLaunchConfiguration configuration = new();
        [SerializeField] private int selectedUnitIndex = -1;
        [SerializeField] private Vector2 scroll;
        private string _notice;

        private sealed class CharacterOption
        {
            public long Id;
            public string Name;
            public SystemEnum.eCharType Type;
            public string Group;
            public string Label => $"{Name}  [{Id}]";
        }

        private sealed class SkillOption
        {
            public long Id;
            public long OwnerId;
            public string Name;
            public string Label => $"{Name}  [{Id}]";
        }

        private sealed class StageOption
        {
            public SystemEnum.Dungeon Dungeon;
            public string StageName;
            public Vector2Int GridSize;
            public HashSet<Vector2Int> Platforms = new();
            public HashSet<Vector2Int> Obstacles = new();
            public HashSet<Vector2Int> Coverages = new();
            public string Label => $"{Dungeon} / {StageName}";
        }

        private sealed class CharacterDropdownItem : AdvancedDropdownItem
        {
            public CharacterOption Option { get; }

            public CharacterDropdownItem(CharacterOption option) : base(option.Label)
            {
                Option = option;
            }
        }

        private sealed class CharacterDropdown : AdvancedDropdown
        {
            private readonly IReadOnlyList<CharacterOption> _options;
            private readonly Action<CharacterOption> _onSelected;

            public CharacterDropdown(
                IReadOnlyList<CharacterOption> options,
                Action<CharacterOption> onSelected)
                : base(new AdvancedDropdownState())
            {
                _options = options;
                _onSelected = onSelected;
                minimumSize = new Vector2(360f, 320f);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                AdvancedDropdownItem root = new("Select Character");
                Dictionary<string, AdvancedDropdownItem> groups = new();

                foreach (CharacterOption option in _options)
                {
                    if (!groups.TryGetValue(option.Group, out AdvancedDropdownItem group))
                    {
                        group = new AdvancedDropdownItem(option.Group);
                        groups.Add(option.Group, group);
                        root.AddChild(group);
                    }

                    group.AddChild(new CharacterDropdownItem(option));
                }

                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item is CharacterDropdownItem selected)
                    _onSelected?.Invoke(selected.Option);
            }
        }

        [MenuItem("Tools/Battle/Battle Test Setup")]
        public static void ShowWindow()
        {
            BattleTestSetupWindow window = GetWindow<BattleTestSetupWindow>();
            window.titleContent = new GUIContent("Battle Test Setup");
            window.minSize = new Vector2(520f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            ReloadCatalog();
            LoadState();
            EnsureValidConfiguration();
        }

        private void OnDisable()
        {
            SaveState();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Battle Test Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "실제 BattleScene 초기화 흐름을 사용하되 스테이지 프리팹의 스폰 데이터는 수정하지 않습니다. " +
                "캐릭터를 선택한 다음 아래 그리드 셀을 눌러 시작 위치를 배치하세요.",
                MessageType.Info);

            DrawStageSelection();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawUnitToolbar();
            DrawUnits();
            DrawGrid();
            EditorGUILayout.EndScrollView();

            DrawFooter();

            if (GUI.changed)
                SaveState();
        }

        private void DrawStageSelection()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("1. Stage", EditorStyles.boldLabel);

            string[] labels = _stages.Select(stage => stage.Label).ToArray();
            int currentIndex = CurrentStageIndex();
            int newIndex = EditorGUILayout.Popup("Dungeon / Stage", currentIndex, labels);
            if (newIndex >= 0 && newIndex < _stages.Count && newIndex != currentIndex)
            {
                StageOption selected = _stages[newIndex];
                configuration.dungeon = (int)selected.Dungeon;
                configuration.stageName = selected.StageName;
                selectedUnitIndex = -1;
                ReassignInvalidCells();
                _notice = "스테이지가 바뀌어 유효하지 않은 배치를 자동으로 옮겼습니다.";
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reload CSV / Stage DB"))
                {
                    ReloadCatalog();
                    EnsureValidConfiguration();
                    _notice = "캐릭터, 스킬, 스테이지 목록을 다시 불러왔습니다.";
                }

                if (GUILayout.Button("Reset: Yeon Push Test"))
                {
                    BuildQuickPushTest();
                    _notice = "연의 뭉게구름 밀치기 테스트 구성으로 초기화했습니다.";
                }
            }
        }

        private void DrawUnitToolbar()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("2. Characters & Skills", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ Player"))
                    ShowCharacterPicker(SystemEnum.eCharType.Player);
                if (GUILayout.Button("+ Enemy"))
                    ShowCharacterPicker(SystemEnum.eCharType.Enemy);
                if (GUILayout.Button("Clear", GUILayout.Width(70f)))
                {
                    configuration.units.Clear();
                    selectedUnitIndex = -1;
                }
            }
        }

        private void DrawUnits()
        {
            if (configuration.units.Count == 0)
            {
                EditorGUILayout.HelpBox("Player와 Enemy를 한 명 이상 추가하세요.", MessageType.Warning);
                return;
            }

            int removeIndex = -1;
            for (int i = 0; i < configuration.units.Count; i++)
            {
                BattleDebugUnitConfiguration unit = configuration.units[i];
                CharacterOption character = FindCharacter(unit.characterId, unit.characterType);
                bool selected = selectedUnitIndex == i;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string title = character?.Label ?? $"Missing Character [{unit.characterId}]";
                        EditorGUILayout.LabelField(
                            $"{(SystemEnum.eCharType)unit.characterType} · {title}",
                            EditorStyles.boldLabel);

                        GUI.backgroundColor = selected ? new Color(0.45f, 0.9f, 1f) : Color.white;
                        if (GUILayout.Button(selected ? "Placing" : "Place / Move", GUILayout.Width(92f)))
                            selectedUnitIndex = selected ? -1 : i;
                        GUI.backgroundColor = Color.white;

                        if (GUILayout.Button("×", GUILayout.Width(26f)))
                            removeIndex = i;
                    }

                    EditorGUILayout.LabelField(
                        $"Cell: ({unit.cellX}, {unit.cellY})  ·  Skills: {unit.skillIds.Count}/4",
                        EditorStyles.miniLabel);
                    DrawSkillToggles(unit);
                }
            }

            if (removeIndex >= 0)
            {
                configuration.units.RemoveAt(removeIndex);
                if (selectedUnitIndex == removeIndex)
                    selectedUnitIndex = -1;
                else if (selectedUnitIndex > removeIndex)
                    selectedUnitIndex--;
            }
        }

        private void DrawSkillToggles(BattleDebugUnitConfiguration unit)
        {
            List<SkillOption> owned = _skills
                .Where(skill => skill.OwnerId == unit.characterId)
                .OrderBy(skill => skill.Id)
                .ToList();

            if (owned.Count == 0)
            {
                EditorGUILayout.LabelField("등록된 스킬 없음", EditorStyles.wordWrappedMiniLabel);
                return;
            }

            foreach (SkillOption skill in owned)
            {
                bool wasSelected = unit.skillIds.Contains(skill.Id);
                bool isSelected = EditorGUILayout.ToggleLeft(skill.Label, wasSelected);
                if (isSelected == wasSelected)
                    continue;

                if (isSelected)
                {
                    if (unit.skillIds.Count >= 4)
                    {
                        _notice = "캐릭터당 장착 스킬은 최대 4개입니다.";
                        continue;
                    }

                    unit.skillIds.Add(skill.Id);
                }
                else
                {
                    unit.skillIds.Remove(skill.Id);
                }
            }
        }

        private void DrawGrid()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("3. Grid Placement", EditorStyles.boldLabel);
            StageOption stage = CurrentStage();
            if (stage == null)
            {
                EditorGUILayout.HelpBox("선택한 스테이지 정보를 읽을 수 없습니다.", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField(
                selectedUnitIndex >= 0
                    ? "배치할 셀을 클릭하세요. 이미 배치된 캐릭터도 다시 선택해 옮길 수 있습니다."
                    : "먼저 캐릭터 카드의 Place / Move를 누르세요.",
                EditorStyles.wordWrappedMiniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawLegend(new Color(0.55f, 0.82f, 0.58f), "Platform");
                DrawLegend(new Color(0.95f, 0.58f, 0.48f), "Obstacle");
                DrawLegend(new Color(0.95f, 0.83f, 0.4f), "Cover");
                GUILayout.FlexibleSpace();
            }

            for (int y = stage.GridSize.y - 1; y >= 0; y--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    for (int x = 0; x < stage.GridSize.x; x++)
                        DrawGridCell(stage, new Vector2Int(x, y));
                    GUILayout.FlexibleSpace();
                }
            }
        }

        private void DrawGridCell(StageOption stage, Vector2Int cell)
        {
            bool platform = stage.Platforms.Contains(cell);
            bool obstacle = stage.Obstacles.Contains(cell);
            bool coverage = stage.Coverages.Contains(cell);
            int occupantIndex = FindOccupant(cell);

            Color oldColor = GUI.backgroundColor;
            if (occupantIndex >= 0)
            {
                SystemEnum.eCharType type =
                    (SystemEnum.eCharType)configuration.units[occupantIndex].characterType;
                GUI.backgroundColor = type == SystemEnum.eCharType.Player
                    ? new Color(0.35f, 0.72f, 1f)
                    : new Color(1f, 0.42f, 0.32f);
            }
            else if (obstacle)
            {
                GUI.backgroundColor = new Color(0.95f, 0.58f, 0.48f);
            }
            else if (coverage)
            {
                GUI.backgroundColor = new Color(0.95f, 0.83f, 0.4f);
            }
            else if (platform)
            {
                GUI.backgroundColor = new Color(0.55f, 0.82f, 0.58f);
            }
            else
            {
                GUI.backgroundColor = new Color(0.42f, 0.42f, 0.42f);
            }

            string occupant = occupantIndex >= 0
                ? ShortName(configuration.units[occupantIndex])
                : string.Empty;
            GUIContent content = new(
                string.IsNullOrEmpty(occupant) ? $"{cell.x},{cell.y}" : $"{occupant}\n{cell.x},{cell.y}",
                BuildCellTooltip(stage, cell, occupant));

            using (new EditorGUI.DisabledScope(!platform || obstacle))
            {
                if (GUILayout.Button(content, GUILayout.Width(54f), GUILayout.Height(43f)))
                    TryPlaceSelectedUnit(cell, occupantIndex);
            }

            GUI.backgroundColor = oldColor;
        }

        private void DrawFooter()
        {
            string validationError = ValidateConfiguration();
            if (!string.IsNullOrEmpty(_notice))
                EditorGUILayout.HelpBox(_notice, MessageType.Info);
            if (!string.IsNullOrEmpty(validationError))
                EditorGUILayout.HelpBox(validationError, MessageType.Error);

            using (new EditorGUI.DisabledScope(
                       !string.IsNullOrEmpty(validationError) ||
                       EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Start Configured Battle Test", GUILayout.Height(38f)))
                {
                    SaveState();
                    BattlePlayModeLauncher.PlayConfiguredBattle(configuration);
                }
            }
        }

        private void ShowCharacterPicker(SystemEnum.eCharType type)
        {
            List<CharacterOption> options = _characters
                .Where(character => character.Type == type)
                .ToList();
            CharacterDropdown dropdown = new(options, AddUnit);
            dropdown.Show(new Rect(Event.current.mousePosition, Vector2.zero));
        }

        private void AddUnit(CharacterOption character)
        {
            StageOption stage = CurrentStage();
            Vector2Int cell = FindFreeCell(stage, character.Type);
            List<long> defaultSkills = _skills
                .Where(skill => skill.OwnerId == character.Id)
                .OrderBy(skill => skill.Id)
                .Take(1)
                .Select(skill => skill.Id)
                .ToList();

            configuration.units.Add(new BattleDebugUnitConfiguration
            {
                characterId = character.Id,
                characterType = (int)character.Type,
                cellX = cell.x,
                cellY = cell.y,
                skillIds = defaultSkills
            });
            selectedUnitIndex = configuration.units.Count - 1;
            SaveState();
            Repaint();
        }

        private void TryPlaceSelectedUnit(Vector2Int cell, int occupantIndex)
        {
            if (selectedUnitIndex < 0 || selectedUnitIndex >= configuration.units.Count)
            {
                _notice = "먼저 옮길 캐릭터의 Place / Move를 눌러주세요.";
                return;
            }

            if (occupantIndex >= 0 && occupantIndex != selectedUnitIndex)
            {
                _notice = "이미 다른 캐릭터가 배치된 셀입니다.";
                return;
            }

            BattleDebugUnitConfiguration unit = configuration.units[selectedUnitIndex];
            unit.cellX = cell.x;
            unit.cellY = cell.y;
            _notice = $"{ShortName(unit)}을(를) 셀 ({cell.x}, {cell.y})에 배치했습니다.";
        }

        private void ReloadCatalog()
        {
            _characters.Clear();
            _skills.Clear();
            _stages.Clear();

            foreach (SheetData row in LoadTable<DokkaebiData>().Values)
            {
                DokkaebiData data = (DokkaebiData)row;
                _characters.Add(new CharacterOption
                {
                    Id = data.index,
                    Name = data.DokkaebiName,
                    Type = SystemEnum.eCharType.Player,
                    Group = "Dokkaebi"
                });
            }

            foreach (SheetData row in LoadTable<CompanionData>().Values)
            {
                CompanionData data = (CompanionData)row;
                _characters.Add(new CharacterOption
                {
                    Id = data.index,
                    Name = string.IsNullOrEmpty(data.charName) ? data.companionName : data.charName,
                    Type = SystemEnum.eCharType.Player,
                    Group = "Companion"
                });
            }

            foreach (SheetData row in LoadTable<MonsterData>().Values)
            {
                MonsterData data = (MonsterData)row;
                _characters.Add(new CharacterOption
                {
                    Id = data.index,
                    Name = data.charName,
                    Type = SystemEnum.eCharType.Enemy,
                    Group = "Monster"
                });
            }

            foreach (SheetData row in LoadTable<DokkaebiSkillData>().Values)
            {
                DokkaebiSkillData data = (DokkaebiSkillData)row;
                _skills.Add(new SkillOption
                {
                    Id = data.index,
                    OwnerId = SystemConst.DokkaebiID,
                    Name = data.skillName
                });
            }

            foreach (SheetData row in LoadTable<SkillData>().Values)
            {
                SkillData data = (SkillData)row;
                _skills.Add(new SkillOption
                {
                    Id = data.index,
                    OwnerId = data.characterID,
                    Name = data.skillName
                });
            }

            LoadStages();
            _characters.Sort((left, right) => left.Id.CompareTo(right.Id));
            _skills.Sort((left, right) => left.Id.CompareTo(right.Id));
            _stages.Sort((left, right) =>
            {
                int dungeon = left.Dungeon.CompareTo(right.Dungeon);
                return dungeon != 0
                    ? dungeon
                    : string.CompareOrdinal(left.StageName, right.StageName);
            });
        }

        private void LoadStages()
        {
            BattleFieldDB db = AssetDatabase.LoadAssetAtPath<BattleFieldDB>(BattleFieldDbPath);
            if (!db)
                return;

            foreach (BattleFieldDB.BattleFieldEntry dungeonEntry in db.DB)
            {
                BattleFieldGroup group = dungeonEntry.group;
                if (!group || group.stages == null)
                    continue;

                foreach (BattleFieldGroup.StageFieldEntry entry in group.stages)
                {
                    if (entry.stageField == null)
                        continue;

                    string path = AssetDatabase.GUIDToAssetPath(entry.stageField.AssetGUID);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    StageField stage = prefab ? prefab.GetComponent<StageField>() : null;
                    if (!stage)
                        continue;

                    _stages.Add(new StageOption
                    {
                        Dungeon = dungeonEntry.dungeon,
                        StageName = entry.stageName,
                        GridSize = stage.GridSize,
                        Platforms = new HashSet<Vector2Int>(
                            stage.PlatformGridCells ?? new List<Vector2Int>()),
                        Obstacles = new HashSet<Vector2Int>(
                            (stage.ObstacleGridCells ?? new List<ObstacleEntry>())
                            .Select(obstacle => obstacle.cell)),
                        Coverages = new HashSet<Vector2Int>(
                            (stage.CoverageGridCells ?? new List<CoverEntry>())
                            .Select(coverage => coverage.cell))
                    });
                }
            }
        }

        private void EnsureValidConfiguration()
        {
            configuration ??= new BattleDebugLaunchConfiguration();
            configuration.units ??= new List<BattleDebugUnitConfiguration>();

            if (CurrentStage() == null && _stages.Count > 0)
            {
                StageOption preferred = _stages.FirstOrDefault(stage =>
                    stage.Dungeon == SystemEnum.Dungeon.MOUNTAIN_BACK && stage.StageName == "0101")
                    ?? _stages[0];
                configuration.dungeon = (int)preferred.Dungeon;
                configuration.stageName = preferred.StageName;
            }

            configuration.units.RemoveAll(unit =>
                FindCharacter(unit.characterId, unit.characterType) == null);
            foreach (BattleDebugUnitConfiguration unit in configuration.units)
            {
                unit.skillIds ??= new List<long>();
                unit.skillIds.RemoveAll(skillId =>
                    !_skills.Any(skill => skill.Id == skillId && skill.OwnerId == unit.characterId));
                unit.skillIds = unit.skillIds.Distinct().ToList();
                if (unit.skillIds.Count > 4)
                    unit.skillIds.RemoveRange(4, unit.skillIds.Count - 4);
            }

            if (configuration.units.Count == 0)
                BuildQuickPushTest();
            else
                ReassignInvalidCells();
        }

        private void BuildQuickPushTest()
        {
            configuration = new BattleDebugLaunchConfiguration { units = new List<BattleDebugUnitConfiguration>() };
            StageOption stage = _stages.FirstOrDefault(candidate =>
                candidate.Dungeon == SystemEnum.Dungeon.MOUNTAIN_BACK && candidate.StageName == "0101")
                ?? _stages.FirstOrDefault();
            if (stage == null)
                return;

            configuration.dungeon = (int)stage.Dungeon;
            configuration.stageName = stage.StageName;

            List<Vector2Int> cells = ValidCells(stage).ToList();
            Vector2Int playerCell = cells.Count > 0 ? cells[0] : Vector2Int.zero;
            Vector2Int enemyCell = cells.FirstOrDefault(cell =>
                cell.y == playerCell.y && cell.x == playerCell.x + 1);
            if (enemyCell == default && cells.Count > 1)
                enemyCell = cells[1];

            AddQuickUnit(SystemConst.DokkaebiID, SystemEnum.eCharType.Player, playerCell, 10101001);
            AddQuickUnit(30001001, SystemEnum.eCharType.Enemy, enemyCell, 30101001);
            selectedUnitIndex = 0;
            SaveState();
        }

        private void AddQuickUnit(
            long characterId,
            SystemEnum.eCharType type,
            Vector2Int cell,
            long preferredSkillId)
        {
            if (FindCharacter(characterId, (int)type) == null)
                return;

            List<long> skills = _skills.Any(skill =>
                    skill.Id == preferredSkillId && skill.OwnerId == characterId)
                ? new List<long> { preferredSkillId }
                : new List<long>();
            configuration.units.Add(new BattleDebugUnitConfiguration
            {
                characterId = characterId,
                characterType = (int)type,
                cellX = cell.x,
                cellY = cell.y,
                skillIds = skills
            });
        }

        private void ReassignInvalidCells()
        {
            StageOption stage = CurrentStage();
            if (stage == null)
                return;

            HashSet<Vector2Int> used = new();
            foreach (BattleDebugUnitConfiguration unit in configuration.units)
            {
                Vector2Int cell = new(unit.cellX, unit.cellY);
                if (!stage.Platforms.Contains(cell) || stage.Obstacles.Contains(cell) || !used.Add(cell))
                {
                    cell = FindFreeCell(stage, (SystemEnum.eCharType)unit.characterType, used);
                    unit.cellX = cell.x;
                    unit.cellY = cell.y;
                    used.Add(cell);
                }
            }
        }

        private Vector2Int FindFreeCell(StageOption stage, SystemEnum.eCharType type)
        {
            HashSet<Vector2Int> used = configuration.units
                .Select(unit => new Vector2Int(unit.cellX, unit.cellY))
                .ToHashSet();
            return FindFreeCell(stage, type, used);
        }

        private static Vector2Int FindFreeCell(
            StageOption stage,
            SystemEnum.eCharType type,
            HashSet<Vector2Int> used)
        {
            if (stage == null)
                return Vector2Int.zero;

            IEnumerable<Vector2Int> candidates = ValidCells(stage);
            if (type == SystemEnum.eCharType.Enemy)
                candidates = candidates.OrderByDescending(cell => cell.x).ThenBy(cell => cell.y);

            foreach (Vector2Int cell in candidates)
            {
                if (!used.Contains(cell))
                    return cell;
            }

            return Vector2Int.zero;
        }

        private static IEnumerable<Vector2Int> ValidCells(StageOption stage)
        {
            return stage.Platforms
                .Where(cell => !stage.Obstacles.Contains(cell))
                .OrderBy(cell => cell.y)
                .ThenBy(cell => cell.x);
        }

        private string ValidateConfiguration()
        {
            StageOption stage = CurrentStage();
            if (stage == null)
                return "유효한 스테이지를 선택해야 합니다.";
            if (!configuration.units.Any(unit =>
                    (SystemEnum.eCharType)unit.characterType == SystemEnum.eCharType.Player))
                return "Player 캐릭터가 한 명 이상 필요합니다.";
            if (!configuration.units.Any(unit =>
                    (SystemEnum.eCharType)unit.characterType == SystemEnum.eCharType.Enemy))
                return "Enemy 캐릭터가 한 명 이상 필요합니다.";

            HashSet<Vector2Int> occupied = new();
            foreach (BattleDebugUnitConfiguration unit in configuration.units)
            {
                CharacterOption character = FindCharacter(unit.characterId, unit.characterType);
                if (character == null)
                    return $"캐릭터 ID {unit.characterId}의 데이터를 찾을 수 없습니다.";
                if (unit.skillIds.Count > 4)
                    return $"{character.Name}의 장착 스킬이 4개를 초과했습니다.";

                Vector2Int cell = new(unit.cellX, unit.cellY);
                if (!stage.Platforms.Contains(cell))
                    return $"{character.Name}의 셀 {cell}에 플랫폼이 없습니다.";
                if (stage.Obstacles.Contains(cell))
                    return $"{character.Name}의 셀 {cell}에 장애물이 있습니다.";
                if (!occupied.Add(cell))
                    return $"셀 {cell}에 캐릭터가 중복 배치되어 있습니다.";
            }

            return null;
        }

        private StageOption CurrentStage()
        {
            int index = CurrentStageIndex();
            return index >= 0 && index < _stages.Count ? _stages[index] : null;
        }

        private int CurrentStageIndex()
        {
            return _stages.FindIndex(stage =>
                (int)stage.Dungeon == configuration.dungeon &&
                stage.StageName == configuration.stageName);
        }

        private CharacterOption FindCharacter(long id, int type)
        {
            return _characters.FirstOrDefault(character =>
                character.Id == id && (int)character.Type == type);
        }

        private int FindOccupant(Vector2Int cell)
        {
            for (int i = 0; i < configuration.units.Count; i++)
            {
                BattleDebugUnitConfiguration unit = configuration.units[i];
                if (unit.cellX == cell.x && unit.cellY == cell.y)
                    return i;
            }

            return -1;
        }

        private string ShortName(BattleDebugUnitConfiguration unit)
        {
            CharacterOption character = FindCharacter(unit.characterId, unit.characterType);
            return string.IsNullOrEmpty(character?.Name)
                ? unit.characterId.ToString()
                : character.Name.Length <= 4
                    ? character.Name
                    : character.Name.Substring(0, 4);
        }

        private static string BuildCellTooltip(StageOption stage, Vector2Int cell, string occupant)
        {
            List<string> descriptions = new() { $"Cell ({cell.x}, {cell.y})" };
            descriptions.Add(stage.Platforms.Contains(cell) ? "Platform" : "No Platform");
            if (stage.Obstacles.Contains(cell)) descriptions.Add("Obstacle");
            if (stage.Coverages.Contains(cell)) descriptions.Add("Cover");
            if (!string.IsNullOrEmpty(occupant)) descriptions.Add($"Unit: {occupant}");
            return string.Join(" / ", descriptions);
        }

        private static void DrawLegend(Color color, string label)
        {
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUILayout.Box(GUIContent.none, GUILayout.Width(18f), GUILayout.Height(12f));
            GUI.backgroundColor = oldColor;
            GUILayout.Label(label, EditorStyles.miniLabel);
        }

        private static Dictionary<long, SheetData> LoadTable<T>() where T : SheetData, new()
        {
            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>($"{DataRoot}{typeof(T).Name}.csv");
            if (!csv)
                return new Dictionary<long, SheetData>();

            return new T().ParseAsync(csv.text, default).GetAwaiter().GetResult();
        }

        private void LoadState()
        {
            if (!EditorPrefs.HasKey(EditorPrefsKey))
                return;

            string json = EditorPrefs.GetString(EditorPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return;

            BattleDebugLaunchConfiguration loaded =
                JsonUtility.FromJson<BattleDebugLaunchConfiguration>(json);
            if (loaded != null)
                configuration = loaded;
        }

        private void SaveState()
        {
            if (configuration != null)
                EditorPrefs.SetString(EditorPrefsKey, JsonUtility.ToJson(configuration));
        }
    }
}
#endif
