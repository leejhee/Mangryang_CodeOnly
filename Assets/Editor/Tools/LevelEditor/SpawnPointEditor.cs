#if UNITY_EDITOR
using GamePlay.Features.Battle.Scripts.BattleMap;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Core.Scripts.Foundation.Define.SystemEnum;

namespace AngelBeat.Tools.LevelEditor
{
    public class SpawnPointEditor
    {
        public SpawnIndicator SpawnIndicatorPrefab { get; private set; }
        private Dictionary<eCharType, Color> TypeColorMapping { get; set; }
        private Dictionary<eCharType, List<SpawnIndicator>> SpawnIndicators { get; set; }
        private eCharType CurrentCharType { get; set; } = eCharType.None;

        private Transform _indicatorRoot;
        private Transform IndicatorRoot
        {
            get
            {
                if (!_indicatorRoot)
                {
                    GameObject go = GameObject.Find("IndicatorRoot");
                    if (!go)
                        go = new GameObject("IndicatorRoot");
                    _indicatorRoot = go.transform;
                }
                return _indicatorRoot;
            }
        }

        public SpawnPointEditor(SpawnIndicator prefab)
        {
            SpawnIndicatorPrefab = prefab;
            TypeColorMapping = new Dictionary<eCharType, Color>();
            SpawnIndicators = new Dictionary<eCharType, List<SpawnIndicator>>();
        }

        #region 인디케이터 색깔 관리(저장, 로드, 업데이트)
        public void LoadColorPreferences()
        {
            foreach (eCharType type in Enum.GetValues(typeof(eCharType)))
            {
                if (type == eCharType.None || type == eCharType.eMax)
                    continue;
                string key = $"StageEditor_{type}_Color";
                if (EditorPrefs.HasKey($"{key}_R"))
                {
                    float r = EditorPrefs.GetFloat($"{key}_R");
                    float g = EditorPrefs.GetFloat($"{key}_G");
                    float b = EditorPrefs.GetFloat($"{key}_B");
                    float a = EditorPrefs.GetFloat($"{key}_A");
                    TypeColorMapping[type] = new Color(r, g, b, a);
                }
                else
                {
                    TypeColorMapping[type] = type == eCharType.Player ? Color.blue :
                                             type == eCharType.Enemy ? Color.red : Color.white;
                }
            }
        }

        public void SaveColorPreferences()
        {
            foreach (var kv in TypeColorMapping)
            {
                string key = $"StageEditor_{kv.Key}_Color";
                Color color = kv.Value;
                EditorPrefs.SetFloat($"{key}_R", color.r);
                EditorPrefs.SetFloat($"{key}_G", color.g);
                EditorPrefs.SetFloat($"{key}_B", color.b);
                EditorPrefs.SetFloat($"{key}_A", color.a);
            }
        }

        public void UpdateSpawnIndicatorColors(eCharType type, Color newColor)
        {
            if (!SpawnIndicators.ContainsKey(type)) return;
            foreach (var indicator in SpawnIndicators[type])
            {
                if (indicator != null)
                    indicator.UpdateColor(newColor);
            }
        }

        #endregion

        
        /// <summary> Spawn Point 관련 OnGUI </summary>
        public void DrawGUI(StageField targetInstance)
        {
            EditorGUILayout.LabelField("스폰 포인트 배치 모드", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "클릭한 셀 중앙에 정렬됩니다. 초록 = 배치 가능, 빨강 = 배치 불가(플랫폼 없음, 장애물 또는 기존 스폰 중복).",
                MessageType.Info);

            SpawnIndicator nextPrefab = (SpawnIndicator)EditorGUILayout.ObjectField(
                "Spawn Indicator Prefab", SpawnIndicatorPrefab, typeof(SpawnIndicator), false);
            if (nextPrefab != SpawnIndicatorPrefab)
            {
                SpawnIndicatorPrefab = nextPrefab;
                RestoreSpawnIndicators(targetInstance);
            }

            if (SpawnIndicatorPrefab == false)
            {
                EditorGUILayout.HelpBox("처음 여셨다면 SpawnIndicator을 할당해 주세요.", MessageType.Error); 
                return;
            }

            // 색상 커스터마이징
            EditorGUILayout.LabelField("타입별 색상 설정", EditorStyles.boldLabel);
            foreach (var key in TypeColorMapping.Keys.ToList())
            {
                Color newColor = EditorGUILayout.ColorField($"{key} 색상", TypeColorMapping[key]);
                if (TypeColorMapping[key] != newColor)
                {
                    TypeColorMapping[key] = newColor;
                    UpdateSpawnIndicatorColors(key, newColor);
                }
            }
            GUILayout.Space(10);

            // 배치 모드 선택
            EditorGUILayout.LabelField("유닛 배치 모드 선택", EditorStyles.boldLabel);
            string[] modeNames = Enum.GetValues(typeof(eCharType))
                                     .Cast<eCharType>()
                                     .Where(mode => mode != eCharType.eMax)
                                     .Select(mode => mode.ToString())
                                     .ToArray();
            CurrentCharType = (eCharType)GUILayout.Toolbar((int)CurrentCharType, modeNames);
            EditorGUILayout.HelpBox($"현재 선택된 배치 모드: {CurrentCharType}", MessageType.Info);
            GUILayout.Space(10);

            // 현재 배치된 SpawnIndicator 리스트 표시
            foreach (var type in SpawnIndicators.Keys.ToList())
            {
                EditorGUILayout.LabelField($"[ {type} ]", EditorStyles.boldLabel);
                for (int i = SpawnIndicators[type].Count - 1; i >= 0; i--)
                {
                    if (!SpawnIndicators[type][i]) continue;
                    EditorGUILayout.BeginHorizontal();
                    SpawnIndicators[type][i] = (SpawnIndicator)EditorGUILayout.ObjectField(
                        $"Indicator {i + 1}", SpawnIndicators[type][i], typeof(SpawnIndicator), true);
                    Vector2Int cell = targetInstance.WorldToCell(SpawnIndicators[type][i].transform.position);
                    SpawnIndicators[type][i].SetCellCoordinate(cell);
                    EditorGUILayout.LabelField($"셀: ({cell.x}, {cell.y})", GUILayout.Width(90));
                    EditorGUILayout.LabelField($"순서: {i + 1}", GUILayout.Width(50));
                    long previousIndex = SpawnIndicators[type][i].spawnFixedIndex;
                    long nextIndex = EditorGUILayout.LongField("정해진 유닛 인덱스", previousIndex);
                    if (nextIndex != previousIndex)
                    {
                        Undo.RecordObject(SpawnIndicators[type][i], "Change Spawn Character Index");
                        SpawnIndicators[type][i].spawnFixedIndex = nextIndex;
                    }
                    
                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    {
                        Undo.DestroyObjectImmediate(SpawnIndicators[type][i].gameObject);
                        SpawnIndicators[type].RemoveAt(i);
                        if (SpawnIndicators[type].Count == 0)
                        {
                            SpawnIndicators.Remove(type);
                            EditorGUILayout.EndHorizontal();
                            break;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            if (GUILayout.Button("Clear All"))
            {
                ClearAllIndicators();
            }
        }

        /// <summary> Spawn Point 배치를 위한 씬 GUI 이벤트를 처리 </summary>
        public void OnSceneGUI(SceneView sceneView, StageField targetInstance)
        {
            if (CurrentCharType == eCharType.None || CurrentCharType == eCharType.eMax || SpawnIndicatorPrefab == null)
                return;
            Event e = Event.current;
            if (e.type == EventType.Layout)
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if (!TryGetWorldPosition(e.mousePosition, targetInstance.transform.position.z, out Vector3 worldPos))
                return;

            Vector2Int cell = targetInstance.WorldToCell(worldPos);
            bool canPlace = IsSpawnCellAvailable(targetInstance, cell);
            StageEditorCellOverlay.Draw(targetInstance, new[] { cell }, canPlace);

            if (e.type != EventType.MouseDown || e.button != 0 || e.alt || e.control || e.command)
                return;

            if (!canPlace)
            {
                e.Use();
                return;
            }

            Vector2 cellCenter = targetInstance.CellToWorldCenter(cell);
            worldPos = new Vector3(cellCenter.x, cellCenter.y, targetInstance.transform.position.z);

            SpawnIndicator newIndicator = UnityEngine.Object.Instantiate
                (SpawnIndicatorPrefab, worldPos, Quaternion.identity, IndicatorRoot);
            newIndicator.SetIndicator(CurrentCharType, TypeColorMapping[CurrentCharType], 0);
            newIndicator.SetCellCoordinate(cell);

            if (!SpawnIndicators.ContainsKey(CurrentCharType))
                SpawnIndicators[CurrentCharType] = new List<SpawnIndicator>();
            SpawnIndicators[CurrentCharType].Add(newIndicator);
            Undo.RegisterCreatedObjectUndo(newIndicator.gameObject, "Create Spawn Indicator");
            e.Use();
        }

        public void ClearAllIndicators()
        {
            GameObject rootObject = _indicatorRoot
                ? _indicatorRoot.gameObject
                : GameObject.Find("IndicatorRoot");
            if (rootObject)
            {
                _indicatorRoot = rootObject.transform;
                for (int i = _indicatorRoot.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.DestroyImmediate(_indicatorRoot.GetChild(i).gameObject);
            }

            SpawnIndicators.Clear();
        }

        public void RestoreSpawnIndicators(StageField targetInstance)
        {
            ClearAllIndicators();
            if (!targetInstance || !SpawnIndicatorPrefab)
                return;

            BattleFieldSpawnInfo spawnerData = targetInstance.LoadSpawnerOnlyInEditor();
            if (spawnerData?.fieldSpawnInfos == null)
                return;

            foreach (var info in spawnerData.fieldSpawnInfos)
            {
                if (!TypeColorMapping.TryGetValue(info.SpawnType, out Color indicatorColor))
                    continue;

                if (!SpawnIndicators.ContainsKey(info.SpawnType))
                    SpawnIndicators[info.SpawnType] = new List<SpawnIndicator>();
                foreach (var data in info.UnitSpawnList)
                {
                    SpawnIndicator newIndicator = UnityEngine.Object.Instantiate
                        (SpawnIndicatorPrefab, 
                        targetInstance.transform.TransformPoint(data.SpawnPosition), 
                        Quaternion.identity, 
                        IndicatorRoot);
                    newIndicator.SetIndicator(
                        info.SpawnType,
                        indicatorColor,
                        data.SpawnCharacterIndex);
                    newIndicator.SetCellCoordinate(targetInstance.WorldToCell(newIndicator.transform.position));
                    SpawnIndicators[info.SpawnType].Add(newIndicator);
                }
            }
            Debug.Log("이전 스폰 데이터를 기반으로 인디케이터를 복원했습니다.");
        }

        public void SaveSpawnPoints(StageField targetInstance)
        {
            if (!targetInstance)
            {
                Debug.LogError("StageMap이 설정되지 않았습니다. 저장 불가.");
                return;
            }
            Undo.RecordObject(targetInstance, "Save Spawn Points");
            SerializedObject so = new(targetInstance);
            SerializedProperty spawnerProp = so.FindProperty("battleSpawnerData");
            if (spawnerProp == null)
            {
                Debug.LogError("battleSpawnerData를 찾을 수 없습니다.");
                return;
            }
            BattleFieldSpawnInfo spawnerInfos = spawnerProp.managedReferenceValue as BattleFieldSpawnInfo;
            spawnerInfos ??= new BattleFieldSpawnInfo();

            spawnerInfos.fieldSpawnInfos.Clear();
            foreach (var kv in SpawnIndicators)
            {
                List<SpawnData> spawnDataList = new();
                foreach (var indicator in kv.Value)
                {
                    if (!indicator)
                        continue;

                    Vector3 localPosition = targetInstance.transform.InverseTransformPoint(
                        indicator.transform.position);
                    spawnDataList.Add(new SpawnData(indicator.spawnFixedIndex, localPosition));
                }
                spawnerInfos.fieldSpawnInfos.Add(new FieldSpawnInfo(kv.Key, spawnDataList));
            }
            spawnerProp.managedReferenceValue = spawnerInfos;
            so.ApplyModifiedProperties();
            Debug.Log("Spawn Points가 StageMap에 저장되었습니다.");
            EditorUtility.SetDirty(targetInstance);
        }

        public bool HasChanges(StageField targetInstance)
        {
            if (!targetInstance || !SpawnIndicatorPrefab)
                return false;

            BattleFieldSpawnInfo saved = targetInstance.LoadSpawnerOnlyInEditor();
            List<FieldSpawnInfo> savedGroups = saved?.fieldSpawnInfos ?? new List<FieldSpawnInfo>();
            Dictionary<eCharType, FieldSpawnInfo> savedByType = savedGroups
                .Where(group => group != null)
                .GroupBy(group => group.SpawnType)
                .ToDictionary(group => group.Key, group => group.First());

            HashSet<eCharType> types = new(savedByType.Keys);
            types.UnionWith(SpawnIndicators.Keys);

            foreach (eCharType type in types)
            {
                List<SpawnIndicator> current = SpawnIndicators.TryGetValue(type, out List<SpawnIndicator> indicators)
                    ? indicators.Where(indicator => indicator).ToList()
                    : new List<SpawnIndicator>();
                List<SpawnData> original = savedByType.TryGetValue(type, out FieldSpawnInfo group)
                    ? group.UnitSpawnList ?? new List<SpawnData>()
                    : new List<SpawnData>();

                if (current.Count != original.Count)
                    return true;

                for (int i = 0; i < current.Count; i++)
                {
                    Vector3 localPosition = targetInstance.transform.InverseTransformPoint(
                        current[i].transform.position);
                    if (current[i].spawnFixedIndex != original[i].SpawnCharacterIndex ||
                        !Approximately(localPosition, original[i].SpawnPosition))
                        return true;
                }
            }

            return false;
        }

        public IReadOnlyCollection<Vector2Int> GetOccupiedCells(StageField targetInstance)
        {
            HashSet<Vector2Int> cells = new();
            if (!targetInstance)
                return cells;

            foreach (List<SpawnIndicator> indicators in SpawnIndicators.Values)
            {
                foreach (SpawnIndicator indicator in indicators)
                {
                    if (indicator)
                        cells.Add(targetInstance.WorldToCell(indicator.transform.position));
                }
            }

            return cells;
        }

        public void CleanupIndicators()
        {
            foreach (var type in SpawnIndicators.Keys.ToList())
            {
                SpawnIndicators[type].RemoveAll(ind => ind == null);
                if (SpawnIndicators[type].Count == 0)
                    SpawnIndicators.Remove(type);
            }
        }

        private static bool TryGetWorldPosition(Vector2 guiPosition, float planeZ, out Vector3 worldPosition)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);
            Plane plane = new(Vector3.forward, new Vector3(0f, 0f, planeZ));
            if (!plane.Raycast(ray, out float distance))
            {
                worldPosition = default;
                return false;
            }

            worldPosition = ray.GetPoint(distance);
            return true;
        }

        private bool IsSpawnCellAvailable(StageField targetInstance, Vector2Int cell)
        {
            if (!targetInstance.IsEditorWalkableCell(cell))
                return false;

            foreach (List<SpawnIndicator> indicators in SpawnIndicators.Values)
            {
                foreach (SpawnIndicator indicator in indicators)
                {
                    if (indicator && targetInstance.WorldToCell(indicator.transform.position) == cell)
                        return false;
                }
            }

            return true;
        }

        private static bool Approximately(Vector3 left, Vector3 right) =>
            (left - right).sqrMagnitude <= 0.000001f;
    }
}


#endif
