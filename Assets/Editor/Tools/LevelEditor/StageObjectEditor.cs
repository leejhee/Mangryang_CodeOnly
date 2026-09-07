#if UNITY_EDITOR
using AngelBeat;
using GamePlay.Features.Battle.Scripts.BattleMap;
using GamePlay.Features.Battle.Scripts.Unit;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// StageEditor의 플랫폼, 게임플레이 오브젝트, 환경물 배치를 담당한다.
/// </summary>
[System.Serializable]
public class StageObjectEditor
{
    private enum PlacementMode
    {
        Platform = 0,
        Obstacle = 1,
        Object = 2,
        Cover = 3
    }

    private enum PlacementKind
    {
        Platform,
        Obstacle,
        Cover,
        Decoration,
        Invalid
    }

    private const float CellRatioTolerance = 0.01f;
    private const int MinimumCoverSortingOrder = 30;

    private sealed class GridDataSnapshot
    {
        public readonly HashSet<Vector2Int> PlatformCells = new();
        public readonly Dictionary<Vector2Int, List<GameObject>> PlatformSources = new();
        public readonly List<ObstacleEntry> Obstacles = new();
        public readonly List<CoverEntry> Covers = new();
    }

    [SerializeField] private List<GameObject> prefabList = new();
    [SerializeField] private PlacementMode placementMode = PlacementMode.Platform;
    [SerializeField] private bool summaryExpanded = true;
    [SerializeField] private bool gridDataInspectorExpanded = true;
    [SerializeField] private bool rawCollectionsExpanded;
    [SerializeField] private Vector2Int selectedDataCell = new(-1, -1);

    private string gridDataNotice;

    public List<GameObject> PrefabList => prefabList;
    public GameObject SelectedPrefab { get; private set; }
    public bool IsPainting { get; private set; }

    private GameObject previewObject;

    public void DrawGUI(StageField targetInstance)
    {
        EditorGUILayout.LabelField("오브젝트 배치 모드", EditorStyles.boldLabel);

        int currentModeIndex = placementMode switch
        {
            PlacementMode.Platform => 0,
            PlacementMode.Obstacle => 1,
            PlacementMode.Cover => 2,
            _ => 3
        };
        int nextModeIndex = GUILayout.Toolbar(
            currentModeIndex,
            new[] { "Platform", "Obstacle", "Cover", "Object" });
        PlacementMode nextMode = nextModeIndex switch
        {
            0 => PlacementMode.Platform,
            1 => PlacementMode.Obstacle,
            2 => PlacementMode.Cover,
            _ => PlacementMode.Object
        };
        if (nextMode != placementMode)
        {
            StopPainting();
            placementMode = nextMode;
        }

        EditorGUILayout.HelpBox(GetModeDescription(), MessageType.Info);
        if (placementMode != PlacementMode.Object)
            EditorGUILayout.HelpBox("셀 표시: 초록 = 배치 가능, 빨강 = 배치 불가", MessageType.None);
        DrawPrefabList();

        bool validSelection = DrawSelectedPrefabInfo(targetInstance);
        GUILayout.Space(10f);

        using (new EditorGUI.DisabledScope(!targetInstance || !SelectedPrefab || !validSelection))
        {
            if (!IsPainting && GUILayout.Button("Start Painting", GUILayout.Height(30f)))
                StartPainting();
        }

        if (IsPainting && GUILayout.Button("Stop Painting", GUILayout.Height(30f)))
            StopPainting();

        GUILayout.Space(12f);
        DrawPlacementSummary(targetInstance);
        GUILayout.Space(8f);
        DrawGridDataInspector(targetInstance);
    }

    public void OnSceneGUI(
        SceneView sceneView,
        StageField targetInstance,
        IReadOnlyCollection<Vector2Int> unitSpawnCells)
    {
        if (!IsPainting || !SelectedPrefab || !targetInstance)
            return;

        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            StopPainting();
            e.Use();
            return;
        }

        Rect sceneRect = new(0f, 0f, sceneView.position.width, sceneView.position.height);
        bool mouseInScene = sceneRect.Contains(e.mousePosition);
        if (previewObject)
            previewObject.SetActive(mouseInScene);

        if (!mouseInScene ||
            !TryGetWorldPosition(e.mousePosition, targetInstance.transform.position.z, out Vector3 worldPosition))
        {
            sceneView.Repaint();
            return;
        }

        PlacementKind kind = GetSelectedPlacementKind();
        GameObject footprintSource = previewObject ? previewObject : SelectedPrefab;
        if (!TryGetPlacement(
                targetInstance,
                footprintSource,
                kind,
                worldPosition,
                out Vector3 placementPosition,
                out List<Vector2Int> occupiedCells))
        {
            if (previewObject)
                previewObject.SetActive(false);
            sceneView.Repaint();
            return;
        }

        GridDataSnapshot liveGridData = BuildGridDataSnapshot(targetInstance, false);
        bool canPlace = IsPlacementAvailable(
            targetInstance,
            kind,
            occupiedCells,
            unitSpawnCells,
            liveGridData);

        if (previewObject)
        {
            previewObject.SetActive(true);
            previewObject.transform.position = placementPosition;
            if (kind == PlacementKind.Platform && occupiedCells.Count > 0)
                ApplyPlatformSortingOrder(previewObject, occupiedCells[occupiedCells.Count / 2].x);
            else if (kind == PlacementKind.Cover)
                ApplyCoverSortingOrder(previewObject);
        }

        if (occupiedCells.Count > 0)
            StageEditorCellOverlay.Draw(targetInstance, occupiedCells, canPlace);

        if (e.type == EventType.Layout)
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && !e.control && !e.command)
        {
            if (!canPlace)
            {
                e.Use();
                return;
            }

            Transform parent = kind == PlacementKind.Platform
                ? targetInstance.PlatformsRoot
                : targetInstance.ObjectsRoot;
            GameObject placedObject = PlaceObject(placementPosition, parent);
            if (kind == PlacementKind.Cover && placedObject)
                ApplyCoverSortingOrder(placedObject);
            SynchronizeGridData(targetInstance);
            e.Use();
        }

        sceneView.Repaint();
    }

    public void StartPainting()
    {
        PlacementKind kind = GetSelectedPlacementKind();
        if (!SelectedPrefab || !IsValidSelection(kind, SelectedPrefab))
            return;

        IsPainting = true;
        if (!previewObject)
        {
            previewObject = Object.Instantiate(SelectedPrefab);
            previewObject.name = "PreviewObject";
            previewObject.hideFlags = HideFlags.HideAndDontSave;
            previewObject.AddComponent<StageEditorPreviewMarker>();
            previewObject.transform.rotation = Quaternion.identity;
        }
    }

    public void StopPainting()
    {
        IsPainting = false;
        if (previewObject)
        {
            Object.DestroyImmediate(previewObject);
            previewObject = null;
        }
    }

    public GameObject PlaceObject(Vector3 position, Transform parent = null)
    {
        if (!SelectedPrefab)
            return null;

        GameObject newObject = parent
            ? PrefabUtility.InstantiatePrefab(SelectedPrefab, parent) as GameObject
            : PrefabUtility.InstantiatePrefab(SelectedPrefab) as GameObject;
        if (!newObject)
            newObject = Object.Instantiate(SelectedPrefab, parent);

        newObject.transform.SetPositionAndRotation(position, Quaternion.identity);
        Undo.RegisterCreatedObjectUndo(newObject, "Place Stage Object");
        Selection.activeGameObject = newObject;
        return newObject;
    }

    public void SaveObjects(StageField targetInstance)
    {
        if (!targetInstance)
        {
            Debug.LogError("StageMap이 설정되지 않았습니다. Object 저장 불가.");
            return;
        }

        Undo.RecordObject(targetInstance, "Save Object Placements");
        MigrateLegacyHierarchy(targetInstance);
        SynchronizeGridData(targetInstance, false);

        SerializedObject serializedStage = new(targetInstance);
        SerializedProperty spawnerProperty = serializedStage.FindProperty("battleSpawnerData");
        if (spawnerProperty == null)
        {
            Debug.LogError("battleSpawnerData를 찾을 수 없습니다.");
            return;
        }

        BattleFieldSpawnInfo info = spawnerProperty.managedReferenceValue as BattleFieldSpawnInfo
                                    ?? new BattleFieldSpawnInfo();
        IEnumerable<Transform> managedObjects = targetInstance.PlatformsRoot.Cast<Transform>()
            .Concat(targetInstance.ObjectsRoot.Cast<Transform>());
        info.fieldObjectInfos = managedObjects
            .Where(child => child && child.gameObject != previewObject)
            .Select(child => new FieldObjectInfo(
                GetPrefabName(child.gameObject),
                targetInstance.transform.InverseTransformPoint(child.position)))
            .ToList();

        spawnerProperty.managedReferenceValue = info;
        serializedStage.ApplyModifiedProperties();

        Debug.Log("Object 배치 및 Grid Cell 정보가 StageMap에 저장되었습니다.");
        EditorUtility.SetDirty(targetInstance);
    }

    public void SynchronizeGridData(StageField targetInstance, bool recordUndo = true)
    {
        if (!targetInstance)
            return;

        GridDataSnapshot snapshot = BuildGridDataSnapshot(targetInstance, true);
        ApplyGridDataSnapshot(targetInstance, snapshot, recordUndo);
    }

    public bool SynchronizeGridDataIfNeeded(StageField targetInstance, bool recordUndo = false)
    {
        if (!targetInstance)
            return false;

        GridDataSnapshot snapshot = BuildGridDataSnapshot(targetInstance, true);
        if (GridDataMatches(targetInstance, snapshot))
            return false;

        ApplyGridDataSnapshot(targetInstance, snapshot, recordUndo);
        return true;
    }

    private static GridDataSnapshot BuildGridDataSnapshot(
        StageField targetInstance,
        bool applyVisualRules)
    {
        GridDataSnapshot snapshot = new();

        // 스냅샷 조회만으로 빈 관리 루트를 만들지 않는다.
        Transform platformsRoot = targetInstance.transform.Find("Platforms");
        HashSet<Vector2Int> obstacleCells = new();
        HashSet<Vector2Int> coverCells = new();

        foreach (GameObject placedObject in EnumeratePlacedRoots(targetInstance))
        {
            PlacementKind kind = InferPlacedKind(placedObject, platformsRoot);
            if (kind is PlacementKind.Decoration or PlacementKind.Invalid)
                continue;

            Vector2Int anchorCell = GetObjectAnchorCell(targetInstance, placedObject, kind);
            switch (kind)
            {
                case PlacementKind.Platform:
                    int width = GetPlatformCellWidth(targetInstance, placedObject, out _);
                    int leftCell = anchorCell.x - width / 2;
                    if (applyVisualRules)
                        ApplyPlatformSortingOrder(placedObject, anchorCell.x);
                    for (int offset = 0; offset < width; offset++)
                    {
                        Vector2Int cell = new(leftCell + offset, anchorCell.y);
                        if (targetInstance.InBounds(cell))
                        {
                            snapshot.PlatformCells.Add(cell);
                            if (!snapshot.PlatformSources.TryGetValue(cell, out List<GameObject> sources))
                            {
                                sources = new List<GameObject>();
                                snapshot.PlatformSources[cell] = sources;
                            }
                            if (!sources.Contains(placedObject))
                                sources.Add(placedObject);
                        }
                    }
                    break;

                case PlacementKind.Obstacle:
                    FieldObstacle obstacle = placedObject.GetComponentInChildren<FieldObstacle>(true);
                    if (obstacle && targetInstance.InBounds(anchorCell) && obstacleCells.Add(anchorCell))
                        snapshot.Obstacles.Add(new ObstacleEntry { cell = anchorCell, obstacle = obstacle });
                    break;

                case PlacementKind.Cover:
                    FieldCover cover = placedObject.GetComponentInChildren<FieldCover>(true);
                    if (applyVisualRules)
                        ApplyCoverSortingOrder(placedObject);
                    if (cover && targetInstance.InBounds(anchorCell) && coverCells.Add(anchorCell))
                        snapshot.Covers.Add(new CoverEntry { cell = anchorCell, cover = cover });
                    break;
            }
        }

        return snapshot;
    }

    private static void ApplyGridDataSnapshot(
        StageField targetInstance,
        GridDataSnapshot snapshot,
        bool recordUndo)
    {
        if (recordUndo)
            Undo.RecordObject(targetInstance, "Synchronize Stage Grid Data");

        targetInstance.PlatformGridCells.Clear();
        targetInstance.PlatformGridCells.AddRange(snapshot.PlatformCells.OrderBy(cell => cell.y).ThenBy(cell => cell.x));
        targetInstance.ObstacleGridCells.Clear();
        targetInstance.ObstacleGridCells.AddRange(snapshot.Obstacles.OrderBy(entry => entry.cell.y).ThenBy(entry => entry.cell.x));
        targetInstance.CoverageGridCells.Clear();
        targetInstance.CoverageGridCells.AddRange(snapshot.Covers.OrderBy(entry => entry.cell.y).ThenBy(entry => entry.cell.x));
        EditorUtility.SetDirty(targetInstance);
    }

    private static bool GridDataMatches(StageField targetInstance, GridDataSnapshot snapshot)
    {
        if (targetInstance.PlatformGridCells.Count != snapshot.PlatformCells.Count ||
            !snapshot.PlatformCells.SetEquals(targetInstance.PlatformGridCells))
            return false;
        if (targetInstance.ObstacleGridCells.Count != snapshot.Obstacles.Count ||
            targetInstance.CoverageGridCells.Count != snapshot.Covers.Count)
            return false;

        bool obstaclesMatch = targetInstance.ObstacleGridCells.All(stored =>
            stored != null && snapshot.Obstacles.Any(expected =>
                expected.cell == stored.cell && expected.obstacle == stored.obstacle));
        bool coversMatch = targetInstance.CoverageGridCells.All(stored =>
            stored != null && snapshot.Covers.Any(expected =>
                expected.cell == stored.cell && expected.cover == stored.cover));
        return obstaclesMatch && coversMatch;
    }

    public void SavePrefabList()
    {
        IEnumerable<string> paths = prefabList
            .Where(prefab => prefab)
            .Select(AssetDatabase.GetAssetPath);
        EditorPrefs.SetString("ObjectPrefabList", string.Join(";", paths));
    }

    public void LoadPrefabList()
    {
        prefabList.Clear();
        if (!EditorPrefs.HasKey("ObjectPrefabList"))
            return;

        foreach (string path in EditorPrefs.GetString("ObjectPrefabList").Split(';'))
        {
            if (string.IsNullOrEmpty(path))
                continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab)
                prefabList.Add(prefab);
        }
    }

    private void DrawPrefabList()
    {
        int removeIndex = -1;
        for (int i = 0; i < prefabList.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GameObject previousPrefab = prefabList[i];
            GameObject nextPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Prefab " + (i + 1), previousPrefab, typeof(GameObject), false);
            if (nextPrefab != previousPrefab)
            {
                prefabList[i] = nextPrefab;
                if (SelectedPrefab == previousPrefab)
                    SelectPrefab(nextPrefab);
            }
            if (GUILayout.Button("Select", GUILayout.Width(52f)) && prefabList[i])
                SelectPrefab(prefabList[i]);
            if (GUILayout.Button("X", GUILayout.Width(24f)))
                removeIndex = i;
            EditorGUILayout.EndHorizontal();

            if (prefabList[i] && SelectedPrefab == prefabList[i])
            {
                Texture2D previewTexture = AssetPreview.GetAssetPreview(SelectedPrefab);
                if (previewTexture)
                    GUILayout.Label(previewTexture, GUILayout.Width(100f), GUILayout.Height(100f));
            }
        }

        if (removeIndex >= 0)
        {
            if (SelectedPrefab == prefabList[removeIndex])
                SelectPrefab(null);
            prefabList.RemoveAt(removeIndex);
        }

        if (GUILayout.Button("Add Prefab"))
            prefabList.Add(null);
        if (GUILayout.Button("Clear List"))
        {
            SelectPrefab(null);
            prefabList.Clear();
        }
    }

    private void DrawPlacementSummary(StageField targetInstance)
    {
        summaryExpanded = EditorGUILayout.Foldout(
            summaryExpanded,
            "Stage 배치 Summary",
            true,
            EditorStyles.foldoutHeader);
        if (!summaryExpanded || !targetInstance)
            return;

        Dictionary<PlacementKind, List<GameObject>> groups = CollectPlacedObjects(targetInstance);
        EditorGUILayout.HelpBox(
            $"Platforms {groups[PlacementKind.Platform].Count}  |  " +
            $"Obstacles {groups[PlacementKind.Obstacle].Count}  |  " +
            $"Covers {groups[PlacementKind.Cover].Count}  |  " +
            $"Objects {groups[PlacementKind.Decoration].Count}",
            MessageType.None);

        GameObject removeTarget = null;
        DrawSummaryGroup("Platforms", groups[PlacementKind.Platform], targetInstance, PlacementKind.Platform, ref removeTarget);
        DrawSummaryGroup("Obstacles", groups[PlacementKind.Obstacle], targetInstance, PlacementKind.Obstacle, ref removeTarget);
        DrawSummaryGroup("Covers", groups[PlacementKind.Cover], targetInstance, PlacementKind.Cover, ref removeTarget);
        DrawSummaryGroup("Objects", groups[PlacementKind.Decoration], targetInstance, PlacementKind.Decoration, ref removeTarget);

        if (!removeTarget)
            return;

        Undo.DestroyObjectImmediate(removeTarget);
        SynchronizeGridData(targetInstance);
        GUIUtility.ExitGUI();
    }

    private static Dictionary<PlacementKind, List<GameObject>> CollectPlacedObjects(StageField targetInstance)
    {
        Dictionary<PlacementKind, List<GameObject>> groups = new()
        {
            [PlacementKind.Platform] = new List<GameObject>(),
            [PlacementKind.Obstacle] = new List<GameObject>(),
            [PlacementKind.Cover] = new List<GameObject>(),
            [PlacementKind.Decoration] = new List<GameObject>()
        };
        HashSet<GameObject> found = new();

        Transform platformsRoot = targetInstance.transform.Find("Platforms");
        Transform objectsRoot = targetInstance.transform.Find("Objects");
        Transform legacyRoot = targetInstance.transform.Find("ObjectRoot");

        AddRootChildren(platformsRoot, PlacementKind.Platform, platformsRoot, groups, found);
        AddRootChildren(objectsRoot, null, platformsRoot, groups, found);
        AddRootChildren(legacyRoot, null, platformsRoot, groups, found);

        // 구형 스테이지의 루트 직하 배치물도 저장 전부터 조회할 수 있게 한다.
        foreach (Transform child in targetInstance.transform)
        {
            if (!child || child == platformsRoot || child == objectsRoot || child == legacyRoot ||
                found.Contains(child.gameObject) || IsEditorPreview(child.gameObject))
                continue;

            PlacementKind kind = InferPlacedKind(child.gameObject, platformsRoot);
            if (kind is PlacementKind.Platform or PlacementKind.Obstacle or PlacementKind.Cover)
            {
                groups[kind].Add(child.gameObject);
                found.Add(child.gameObject);
            }
        }

        return groups;
    }

    private static void AddRootChildren(
        Transform root,
        PlacementKind? forcedKind,
        Transform platformsRoot,
        IDictionary<PlacementKind, List<GameObject>> groups,
        ISet<GameObject> found)
    {
        if (!root)
            return;

        foreach (Transform child in root)
        {
            if (!child || IsEditorPreview(child.gameObject) || !found.Add(child.gameObject))
                continue;

            PlacementKind kind = forcedKind ?? InferPlacedKind(child.gameObject, platformsRoot);
            if (kind != PlacementKind.Invalid && groups.TryGetValue(kind, out List<GameObject> group))
                group.Add(child.gameObject);
        }
    }

    private static void DrawSummaryGroup(
        string label,
        IReadOnlyList<GameObject> objects,
        StageField targetInstance,
        PlacementKind kind,
        ref GameObject removeTarget)
    {
        EditorGUILayout.LabelField($"{label} ({objects.Count})", EditorStyles.boldLabel);
        if (objects.Count == 0)
        {
            EditorGUILayout.LabelField("배치된 항목 없음", EditorStyles.miniLabel);
            return;
        }

        foreach (GameObject placedObject in objects)
        {
            if (!placedObject)
                continue;

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField(placedObject, typeof(GameObject), true);
            EditorGUILayout.LabelField(
                GetPlacementSummary(targetInstance, placedObject, kind),
                EditorStyles.miniLabel,
                GUILayout.Width(150f));
            if (GUILayout.Button("Select", GUILayout.Width(52f)))
            {
                Selection.activeGameObject = placedObject;
                EditorGUIUtility.PingObject(placedObject);
            }
            if (GUILayout.Button("Remove", GUILayout.Width(62f)))
                removeTarget = placedObject;
            EditorGUILayout.EndHorizontal();
        }
    }

    private static string GetPlacementSummary(
        StageField targetInstance,
        GameObject placedObject,
        PlacementKind kind)
    {
        if (kind == PlacementKind.Decoration)
        {
            Vector3 local = targetInstance.transform.InverseTransformPoint(placedObject.transform.position);
            return $"Local ({local.x:0.##}, {local.y:0.##})";
        }

        Vector2Int anchor = GetObjectAnchorCell(targetInstance, placedObject, kind);
        if (kind != PlacementKind.Platform)
            return $"Cell ({anchor.x}, {anchor.y})";

        int width = GetPlatformCellWidth(targetInstance, placedObject, out _);
        int left = anchor.x - width / 2;
        int right = left + width - 1;
        return left == right
            ? $"Cell ({left}, {anchor.y})"
            : $"Cells ({left}, {anchor.y})~({right}, {anchor.y})";
    }

    private void DrawGridDataInspector(StageField targetInstance)
    {
        gridDataInspectorExpanded = EditorGUILayout.Foldout(
            gridDataInspectorExpanded,
            "Internal Grid Collections",
            true,
            EditorStyles.foldoutHeader);
        if (!gridDataInspectorExpanded || !targetInstance)
            return;

        GridDataSnapshot live = BuildGridDataSnapshot(targetInstance, false);
        bool matches = GridDataMatches(targetInstance, live);
        EditorGUILayout.HelpBox(
            matches
                ? "저장 컬렉션과 현재 하이어라키 계산 결과가 일치합니다."
                : "저장 컬렉션과 현재 하이어라키가 다릅니다. 주황색 셀과 Raw Collections의 상태를 확인하세요.",
            matches ? MessageType.Info : MessageType.Warning);

        EditorGUILayout.LabelField(
            $"Stored  P:{targetInstance.PlatformGridCells.Count}  " +
            $"O:{targetInstance.ObstacleGridCells.Count}  C:{targetInstance.CoverageGridCells.Count}",
            EditorStyles.miniLabel);
        EditorGUILayout.LabelField(
            $"Hierarchy  P:{live.PlatformCells.Count}  O:{live.Obstacles.Count}  C:{live.Covers.Count}",
            EditorStyles.miniLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Rebuild From Hierarchy"))
            {
                SynchronizeGridData(targetInstance);
                live = BuildGridDataSnapshot(targetInstance, false);
                gridDataNotice = "현재 하이어라키를 기준으로 내부 컬렉션을 다시 만들었습니다.";
            }

            if (GUILayout.Button("Remove Stale Only"))
            {
                int removed = RemoveStaleGridData(targetInstance, live);
                gridDataNotice = removed > 0
                    ? $"잘못되었거나 중복된 데이터 {removed}개를 제거했습니다."
                    : "제거할 잘못된 데이터가 없습니다.";
            }

            if (GUILayout.Button("Clear Collections"))
            {
                if (EditorUtility.DisplayDialog(
                        "Clear Grid Collections",
                        "플랫폼/장애물/커버 내부 셀 데이터만 모두 비웁니다. 배치된 오브젝트는 삭제하지 않습니다.",
                        "Clear",
                        "Cancel"))
                {
                    ClearGridCollections(targetInstance);
                    gridDataNotice = "내부 셀 컬렉션을 모두 비웠습니다. Rebuild로 복구할 수 있습니다.";
                }
            }
        }

        if (!string.IsNullOrEmpty(gridDataNotice))
            EditorGUILayout.HelpBox(gridDataNotice, MessageType.None);

        DrawInternalGrid(targetInstance, live);
        DrawSelectedDataCell(targetInstance, live);

        rawCollectionsExpanded = EditorGUILayout.Foldout(
            rawCollectionsExpanded,
            "Raw Collections",
            true);
        if (rawCollectionsExpanded)
            DrawRawCollections(targetInstance, live);
    }

    private void DrawInternalGrid(StageField targetInstance, GridDataSnapshot live)
    {
        EditorGUILayout.Space(5f);
        EditorGUILayout.LabelField("Stored Cell Map", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawDataLegend(new Color(0.55f, 0.82f, 0.58f), "Platform");
            DrawDataLegend(new Color(0.95f, 0.58f, 0.48f), "Obstacle");
            DrawDataLegend(new Color(0.95f, 0.83f, 0.4f), "Cover");
            DrawDataLegend(new Color(1f, 0.58f, 0.2f), "Mismatch");
            GUILayout.FlexibleSpace();
        }

        int width = Mathf.Max(1, targetInstance.GridSize.x);
        float availableWidth = Mathf.Max(200f, EditorGUIUtility.currentViewWidth - 55f);
        float cellWidth = Mathf.Clamp(availableWidth / width - 2f, 32f, 56f);
        for (int y = targetInstance.GridSize.y - 1; y >= 0; y--)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                for (int x = 0; x < targetInstance.GridSize.x; x++)
                {
                    Vector2Int cell = new(x, y);
                    bool platform = targetInstance.PlatformGridCells.Contains(cell);
                    bool obstacle = targetInstance.ObstacleGridCells.Any(entry =>
                        entry != null && entry.cell == cell);
                    bool cover = targetInstance.CoverageGridCells.Any(entry =>
                        entry != null && entry.cell == cell);
                    bool mismatch = CellDataMismatch(targetInstance, live, cell);

                    Color previousColor = GUI.backgroundColor;
                    if (selectedDataCell == cell)
                        GUI.backgroundColor = new Color(0.35f, 0.72f, 1f);
                    else if (mismatch)
                        GUI.backgroundColor = new Color(1f, 0.58f, 0.2f);
                    else if (obstacle)
                        GUI.backgroundColor = new Color(0.95f, 0.58f, 0.48f);
                    else if (cover)
                        GUI.backgroundColor = new Color(0.95f, 0.83f, 0.4f);
                    else if (platform)
                        GUI.backgroundColor = new Color(0.55f, 0.82f, 0.58f);
                    else
                        GUI.backgroundColor = new Color(0.42f, 0.42f, 0.42f);

                    string flags = $"{(platform ? "P" : "-")}{(obstacle ? "O" : "-")}{(cover ? "C" : "-")}";
                    GUIContent content = new(
                        $"{x},{y}\n{flags}",
                        BuildDataCellTooltip(targetInstance, live, cell));
                    if (GUILayout.Button(content, GUILayout.Width(cellWidth), GUILayout.Height(40f)))
                        selectedDataCell = cell;
                    GUI.backgroundColor = previousColor;
                }
                GUILayout.FlexibleSpace();
            }
        }
    }

    private void DrawSelectedDataCell(StageField targetInstance, GridDataSnapshot live)
    {
        if (!targetInstance.InBounds(selectedDataCell))
            return;

        Vector2Int cell = selectedDataCell;
        bool storedPlatform = targetInstance.PlatformGridCells.Contains(cell);
        bool storedObstacle = targetInstance.ObstacleGridCells.Any(entry =>
            entry != null && entry.cell == cell);
        bool storedCover = targetInstance.CoverageGridCells.Any(entry =>
            entry != null && entry.cell == cell);
        bool livePlatform = live.PlatformCells.Contains(cell);
        bool liveObstacle = live.Obstacles.Any(entry => entry.cell == cell);
        bool liveCover = live.Covers.Any(entry => entry.cell == cell);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField($"Selected Cell ({cell.x}, {cell.y})", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"Stored: P={storedPlatform}, O={storedObstacle}, C={storedCover}  |  " +
                $"Hierarchy: P={livePlatform}, O={liveObstacle}, C={liveCover}",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField(
                "Hierarchy Sources: " + GetHierarchySourceLabel(live, cell),
                EditorStyles.wordWrappedMiniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!storedPlatform))
                {
                    if (GUILayout.Button("Remove P Data"))
                        RemoveCellData(targetInstance, cell, PlacementKind.Platform);
                }
                using (new EditorGUI.DisabledScope(!storedObstacle))
                {
                    if (GUILayout.Button("Remove O Data"))
                        RemoveCellData(targetInstance, cell, PlacementKind.Obstacle);
                }
                using (new EditorGUI.DisabledScope(!storedCover))
                {
                    if (GUILayout.Button("Remove C Data"))
                        RemoveCellData(targetInstance, cell, PlacementKind.Cover);
                }
            }
        }
    }

    private static bool CellDataMismatch(
        StageField targetInstance,
        GridDataSnapshot live,
        Vector2Int cell)
    {
        int storedPlatformCount = targetInstance.PlatformGridCells.Count(candidate => candidate == cell);
        int expectedPlatformCount = live.PlatformCells.Contains(cell) ? 1 : 0;
        List<ObstacleEntry> storedObstacles = targetInstance.ObstacleGridCells
            .Where(entry => entry != null && entry.cell == cell)
            .ToList();
        List<ObstacleEntry> expectedObstacles = live.Obstacles
            .Where(entry => entry.cell == cell)
            .ToList();
        List<CoverEntry> storedCovers = targetInstance.CoverageGridCells
            .Where(entry => entry != null && entry.cell == cell)
            .ToList();
        List<CoverEntry> expectedCovers = live.Covers
            .Where(entry => entry.cell == cell)
            .ToList();

        return storedPlatformCount != expectedPlatformCount ||
               storedObstacles.Count != expectedObstacles.Count ||
               storedObstacles.Any(stored => !stored.obstacle || !expectedObstacles.Any(expected =>
                   expected.obstacle == stored.obstacle)) ||
               storedCovers.Count != expectedCovers.Count ||
               storedCovers.Any(stored => !stored.cover || !expectedCovers.Any(expected =>
                   expected.cover == stored.cover));
    }

    private static string BuildDataCellTooltip(
        StageField targetInstance,
        GridDataSnapshot live,
        Vector2Int cell)
    {
        string stored = $"Stored: " +
                        $"P={targetInstance.PlatformGridCells.Contains(cell)}, " +
                        $"O={targetInstance.ObstacleGridCells.Any(entry => entry != null && entry.cell == cell)}, " +
                        $"C={targetInstance.CoverageGridCells.Any(entry => entry != null && entry.cell == cell)}";
        string hierarchy = $"Hierarchy: " +
                           $"P={live.PlatformCells.Contains(cell)}, " +
                           $"O={live.Obstacles.Any(entry => entry.cell == cell)}, " +
                           $"C={live.Covers.Any(entry => entry.cell == cell)}";
        return $"Cell ({cell.x}, {cell.y}) / {stored} / {hierarchy} / " +
               $"Sources: {GetHierarchySourceLabel(live, cell)}";
    }

    private static string GetHierarchySourceLabel(GridDataSnapshot live, Vector2Int cell)
    {
        List<string> sources = new();
        if (live.PlatformSources.TryGetValue(cell, out List<GameObject> platforms))
            sources.AddRange(platforms.Where(platform => platform).Select(platform => $"P:{platform.name}"));
        sources.AddRange(live.Obstacles
            .Where(entry => entry.cell == cell && entry.obstacle)
            .Select(entry => $"O:{entry.obstacle.name}"));
        sources.AddRange(live.Covers
            .Where(entry => entry.cell == cell && entry.cover)
            .Select(entry => $"C:{entry.cover.name}"));
        return sources.Count > 0 ? string.Join(", ", sources) : "None";
    }

    private static void DrawDataLegend(Color color, string label)
    {
        Color previousColor = GUI.backgroundColor;
        GUI.backgroundColor = color;
        GUILayout.Box(GUIContent.none, GUILayout.Width(16f), GUILayout.Height(11f));
        GUI.backgroundColor = previousColor;
        GUILayout.Label(label, EditorStyles.miniLabel);
    }

    private void DrawRawCollections(StageField targetInstance, GridDataSnapshot live)
    {
        DrawRawPlatformCells(targetInstance, live);
        DrawRawObstacleCells(targetInstance, live);
        DrawRawCoverCells(targetInstance, live);
    }

    private void DrawRawPlatformCells(StageField targetInstance, GridDataSnapshot live)
    {
        EditorGUILayout.LabelField($"PlatformGridCells ({targetInstance.PlatformGridCells.Count})", EditorStyles.boldLabel);
        for (int i = 0; i < targetInstance.PlatformGridCells.Count; i++)
        {
            Vector2Int cell = targetInstance.PlatformGridCells[i];
            string status = !targetInstance.InBounds(cell)
                ? "Out of Bounds"
                : targetInstance.PlatformGridCells.Count(candidate => candidate == cell) > 1
                    ? "Duplicate"
                    : !live.PlatformCells.Contains(cell)
                        ? "Stale"
                        : "OK";
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"[{i}] ({cell.x}, {cell.y})", GUILayout.Width(105f));
                EditorGUILayout.LabelField(status, EditorStyles.miniLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(62f)))
                {
                    RemoveRawEntry(targetInstance, targetInstance.PlatformGridCells, i, "Remove Platform Cell Data");
                    return;
                }
            }
        }
    }

    private void DrawRawObstacleCells(StageField targetInstance, GridDataSnapshot live)
    {
        EditorGUILayout.LabelField($"ObstacleGridCells ({targetInstance.ObstacleGridCells.Count})", EditorStyles.boldLabel);
        for (int i = 0; i < targetInstance.ObstacleGridCells.Count; i++)
        {
            ObstacleEntry entry = targetInstance.ObstacleGridCells[i];
            string status = GetObstacleEntryStatus(targetInstance, live, entry);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    entry == null ? $"[{i}] null" : $"[{i}] ({entry.cell.x}, {entry.cell.y})",
                    GUILayout.Width(105f));
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField(entry?.obstacle, typeof(FieldObstacle), true);
                EditorGUILayout.LabelField(status, EditorStyles.miniLabel, GUILayout.Width(105f));
                if (GUILayout.Button("Remove", GUILayout.Width(62f)))
                {
                    RemoveRawEntry(targetInstance, targetInstance.ObstacleGridCells, i, "Remove Obstacle Cell Data");
                    return;
                }
            }
        }
    }

    private void DrawRawCoverCells(StageField targetInstance, GridDataSnapshot live)
    {
        EditorGUILayout.LabelField($"CoverageGridCells ({targetInstance.CoverageGridCells.Count})", EditorStyles.boldLabel);
        for (int i = 0; i < targetInstance.CoverageGridCells.Count; i++)
        {
            CoverEntry entry = targetInstance.CoverageGridCells[i];
            string status = GetCoverEntryStatus(targetInstance, live, entry);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    entry == null ? $"[{i}] null" : $"[{i}] ({entry.cell.x}, {entry.cell.y})",
                    GUILayout.Width(105f));
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField(entry?.cover, typeof(FieldCover), true);
                EditorGUILayout.LabelField(status, EditorStyles.miniLabel, GUILayout.Width(105f));
                if (GUILayout.Button("Remove", GUILayout.Width(62f)))
                {
                    RemoveRawEntry(targetInstance, targetInstance.CoverageGridCells, i, "Remove Cover Cell Data");
                    return;
                }
            }
        }
    }

    private static string GetObstacleEntryStatus(
        StageField targetInstance,
        GridDataSnapshot live,
        ObstacleEntry entry)
    {
        if (entry == null || !entry.obstacle)
            return "Missing Reference";
        if (!targetInstance.InBounds(entry.cell))
            return "Out of Bounds";
        if (targetInstance.ObstacleGridCells.Count(candidate =>
                candidate != null && candidate.cell == entry.cell) > 1)
            return "Duplicate";
        return live.Obstacles.Any(expected =>
            expected.cell == entry.cell && expected.obstacle == entry.obstacle)
            ? "OK"
            : "Stale / Moved";
    }

    private static string GetCoverEntryStatus(
        StageField targetInstance,
        GridDataSnapshot live,
        CoverEntry entry)
    {
        if (entry == null || !entry.cover)
            return "Missing Reference";
        if (!targetInstance.InBounds(entry.cell))
            return "Out of Bounds";
        if (targetInstance.CoverageGridCells.Count(candidate =>
                candidate != null && candidate.cell == entry.cell) > 1)
            return "Duplicate";
        return live.Covers.Any(expected =>
            expected.cell == entry.cell && expected.cover == entry.cover)
            ? "OK"
            : "Stale / Moved";
    }

    private static int RemoveStaleGridData(StageField targetInstance, GridDataSnapshot live)
    {
        Undo.RecordObject(targetInstance, "Remove Stale Stage Grid Data");
        int removed = 0;
        HashSet<Vector2Int> seenPlatforms = new();
        removed += targetInstance.PlatformGridCells.RemoveAll(cell =>
            !targetInstance.InBounds(cell) ||
            !live.PlatformCells.Contains(cell) ||
            !seenPlatforms.Add(cell));

        HashSet<Vector2Int> seenObstacles = new();
        removed += targetInstance.ObstacleGridCells.RemoveAll(entry =>
            entry == null || !entry.obstacle || !targetInstance.InBounds(entry.cell) ||
            !live.Obstacles.Any(expected =>
                expected.cell == entry.cell && expected.obstacle == entry.obstacle) ||
            !seenObstacles.Add(entry.cell));

        HashSet<Vector2Int> seenCovers = new();
        removed += targetInstance.CoverageGridCells.RemoveAll(entry =>
            entry == null || !entry.cover || !targetInstance.InBounds(entry.cell) ||
            !live.Covers.Any(expected =>
                expected.cell == entry.cell && expected.cover == entry.cover) ||
            !seenCovers.Add(entry.cell));

        if (removed > 0)
            EditorUtility.SetDirty(targetInstance);
        return removed;
    }

    private static void ClearGridCollections(StageField targetInstance)
    {
        Undo.RecordObject(targetInstance, "Clear Stage Grid Collections");
        targetInstance.PlatformGridCells.Clear();
        targetInstance.ObstacleGridCells.Clear();
        targetInstance.CoverageGridCells.Clear();
        EditorUtility.SetDirty(targetInstance);
    }

    private static void RemoveCellData(
        StageField targetInstance,
        Vector2Int cell,
        PlacementKind kind)
    {
        Undo.RecordObject(targetInstance, "Remove Stage Cell Data");
        switch (kind)
        {
            case PlacementKind.Platform:
                targetInstance.PlatformGridCells.RemoveAll(candidate => candidate == cell);
                break;
            case PlacementKind.Obstacle:
                targetInstance.ObstacleGridCells.RemoveAll(entry => entry != null && entry.cell == cell);
                break;
            case PlacementKind.Cover:
                targetInstance.CoverageGridCells.RemoveAll(entry => entry != null && entry.cell == cell);
                break;
        }
        EditorUtility.SetDirty(targetInstance);
    }

    private static void RemoveRawEntry<T>(
        StageField targetInstance,
        List<T> collection,
        int index,
        string undoName)
    {
        Undo.RecordObject(targetInstance, undoName);
        collection.RemoveAt(index);
        EditorUtility.SetDirty(targetInstance);
    }

    private bool DrawSelectedPrefabInfo(StageField targetInstance)
    {
        if (!SelectedPrefab)
        {
            EditorGUILayout.LabelField("현재 선택된 프리팹이 없습니다.");
            return false;
        }

        EditorGUILayout.LabelField("현재 선택된 프리팹: " + SelectedPrefab.name);
        PlacementKind kind = GetSelectedPlacementKind();
        bool valid = IsValidSelection(kind, SelectedPrefab);
        if (!valid)
        {
            string errorMessage = placementMode switch
            {
                PlacementMode.Platform =>
                    "Platform 모드에는 폭 계산용 BoxCollider2D가 필요하며 FieldObstacle/FieldCover 프리팹은 사용할 수 없습니다.",
                PlacementMode.Obstacle =>
                    "Obstacle 모드에는 BoxCollider2D와 FieldObstacle이 있는 프리팹이 필요합니다.",
                PlacementMode.Cover =>
                    "Cover 모드에는 BoxCollider2D와 FieldCover가 있는 프리팹이 필요합니다.",
                _ => "FieldObstacle/FieldCover 프리팹은 Object가 아니라 Obstacle / Cover 모드에서 배치하세요."
            };
            EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
            return false;
        }

        if (kind == PlacementKind.Platform && targetInstance)
        {
            int width = GetPlatformCellWidth(targetInstance, SelectedPrefab, out float ratio);
            EditorGUILayout.HelpBox(
                $"Platform · {width}칸 점유 (Collider Width / Cell Width = {ratio:0.##})",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox($"배치 분류: {kind}", MessageType.Info);
        }
        return true;
    }

    private string GetModeDescription() => placementMode switch
    {
        PlacementMode.Platform =>
            "플랫폼: 클릭한 셀의 하단 경계에 스냅합니다. BoxCollider2D 폭으로 점유 셀 수를 계산하고 Platforms 아래에 배치합니다.",
        PlacementMode.Obstacle =>
            "장애물: 플랫폼이 있고 유닛/커버/장애물이 없는 셀에 배치합니다. 해당 셀은 이동 불가가 됩니다.",
        PlacementMode.Cover =>
            "엄폐물: 플랫폼이 있고 장애물/커버가 없는 셀에 배치합니다. 유닛과 같은 셀을 사용할 수 있습니다.",
        _ => "환경물: 그리드 스냅 없이 클릭한 위치에 배치하고 Objects 아래에 정리합니다."
    };

    private void SelectPrefab(GameObject prefab)
    {
        if (SelectedPrefab == prefab)
            return;

        StopPainting();
        SelectedPrefab = prefab;
    }

    private PlacementKind GetSelectedPlacementKind()
    {
        if (!SelectedPrefab)
            return PlacementKind.Invalid;

        return placementMode switch
        {
            PlacementMode.Platform => PlacementKind.Platform,
            PlacementMode.Obstacle => GetGameplayObjectKind(SelectedPrefab) == PlacementKind.Obstacle
                ? PlacementKind.Obstacle
                : PlacementKind.Invalid,
            PlacementMode.Cover => GetGameplayObjectKind(SelectedPrefab) == PlacementKind.Cover
                ? PlacementKind.Cover
                : PlacementKind.Invalid,
            _ => PlacementKind.Decoration
        };
    }

    private static PlacementKind GetGameplayObjectKind(GameObject instanceOrPrefab)
    {
        if (instanceOrPrefab.GetComponentInChildren<FieldObstacle>(true))
            return PlacementKind.Obstacle;
        if (instanceOrPrefab.GetComponentInChildren<FieldCover>(true))
            return PlacementKind.Cover;
        return PlacementKind.Invalid;
    }

    private static bool IsValidSelection(PlacementKind kind, GameObject prefab)
    {
        if (!prefab || kind == PlacementKind.Invalid)
            return false;

        PlacementKind gameplayKind = GetGameplayObjectKind(prefab);
        return kind switch
        {
            PlacementKind.Platform => gameplayKind == PlacementKind.Invalid &&
                                      prefab.GetComponentInChildren<BoxCollider2D>(true),
            PlacementKind.Obstacle or PlacementKind.Cover =>
                prefab.GetComponentInChildren<BoxCollider2D>(true),
            PlacementKind.Decoration => gameplayKind == PlacementKind.Invalid,
            _ => false
        };
    }

    private static PlacementKind InferPlacedKind(GameObject placedObject, Transform platformsRoot)
    {
        // Transform.IsChildOf는 자기 자신에도 true를 반환한다. 빈 Platforms 루트를
        // 실제 1칸 플랫폼으로 분류하지 않도록 반드시 루트 자신을 제외한다.
        if (platformsRoot && placedObject.transform != platformsRoot &&
            placedObject.transform.IsChildOf(platformsRoot))
            return PlacementKind.Platform;

        PlacementKind gameplayKind = GetGameplayObjectKind(placedObject);
        if (gameplayKind != PlacementKind.Invalid)
            return gameplayKind;

        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(placedObject);
        string path = AssetDatabase.GetAssetPath(source ? source : placedObject).Replace('\\', '/');
        return path.Contains("/FieldObjects/FootHolders/")
            ? PlacementKind.Platform
            : PlacementKind.Decoration;
    }

    private static int GetPlatformCellWidth(
        StageField targetInstance,
        GameObject instanceOrPrefab,
        out float cellRatio)
    {
        BoxCollider2D collider = instanceOrPrefab.GetComponentInChildren<BoxCollider2D>(true);
        float cellWidth = Mathf.Abs(targetInstance.Grid.cellSize.x);
        if (!collider || cellWidth <= Mathf.Epsilon)
        {
            cellRatio = 1f;
            return 1;
        }

        float objectWidth = collider.size.x * Mathf.Abs(collider.transform.lossyScale.x);
        if (instanceOrPrefab.transform.IsChildOf(targetInstance.transform))
        {
            float stageScale = Mathf.Abs(targetInstance.transform.lossyScale.x);
            if (stageScale > Mathf.Epsilon)
                objectWidth /= stageScale;
        }

        cellRatio = objectWidth / cellWidth;
        return Mathf.Max(1, Mathf.CeilToInt(cellRatio - CellRatioTolerance));
    }

    private static bool TryGetPlacement(
        StageField targetInstance,
        GameObject footprintSource,
        PlacementKind kind,
        Vector3 worldPosition,
        out Vector3 position,
        out List<Vector2Int> occupiedCells)
    {
        occupiedCells = new List<Vector2Int>();
        if (kind == PlacementKind.Decoration)
        {
            position = worldPosition;
            return true;
        }

        if (kind == PlacementKind.Invalid)
        {
            position = default;
            return false;
        }

        Vector2Int clickedCell = targetInstance.WorldToCell(worldPosition);

        int width = kind == PlacementKind.Platform
            ? GetPlatformCellWidth(targetInstance, footprintSource, out _)
            : 1;
        for (int offset = 0; offset < width; offset++)
        {
            Vector2Int cell = new(clickedCell.x + offset, clickedCell.y);
            occupiedCells.Add(cell);
        }

        Vector2 firstCenter = targetInstance.CellToWorldCenter(occupiedCells[0]);
        Vector2 lastCenter = targetInstance.CellToWorldCenter(occupiedCells[^1]);
        Vector2 cellBottom = targetInstance.CellToWorldBottomCenter(clickedCell);
        float x = (firstCenter.x + lastCenter.x) * 0.5f;
        float y = cellBottom.y;

        if (kind is PlacementKind.Obstacle or PlacementKind.Cover)
        {
            if (!TryGetColliderOffsets(targetInstance, footprintSource, out float centerOffsetX, out float bottomOffsetY))
            {
                position = default;
                return false;
            }
            x = firstCenter.x - centerOffsetX;
            y = cellBottom.y - bottomOffsetY;
        }

        position = new Vector3(x, y, worldPosition.z);
        return true;
    }

    private static bool IsPlacementAvailable(
        StageField targetInstance,
        PlacementKind kind,
        IReadOnlyCollection<Vector2Int> occupiedCells,
        IReadOnlyCollection<Vector2Int> unitSpawnCells,
        GridDataSnapshot liveGridData)
    {
        if (kind == PlacementKind.Decoration)
            return true;
        if (kind == PlacementKind.Invalid || occupiedCells.Count == 0 ||
            occupiedCells.Any(cell => !targetInstance.InBounds(cell)))
            return false;

        return kind switch
        {
            PlacementKind.Platform => occupiedCells.All(cell =>
                !liveGridData.PlatformCells.Contains(cell) &&
                !liveGridData.Obstacles.Any(entry => entry.cell == cell) &&
                !liveGridData.Covers.Any(entry => entry.cell == cell)),
            PlacementKind.Obstacle => occupiedCells.All(cell =>
                liveGridData.PlatformCells.Contains(cell) &&
                !liveGridData.Obstacles.Any(entry => entry.cell == cell) &&
                !liveGridData.Covers.Any(entry => entry.cell == cell) &&
                (unitSpawnCells == null || !unitSpawnCells.Contains(cell))),
            PlacementKind.Cover => occupiedCells.All(cell =>
                liveGridData.PlatformCells.Contains(cell) &&
                !liveGridData.Obstacles.Any(entry => entry.cell == cell) &&
                !liveGridData.Covers.Any(entry => entry.cell == cell)),
            _ => false
        };
    }

    private static Vector2Int GetObjectAnchorCell(
        StageField targetInstance,
        GameObject placedObject,
        PlacementKind kind)
    {
        float epsilonX = Mathf.Abs(targetInstance.Grid.cellSize.x * targetInstance.transform.lossyScale.x) * 0.01f;
        float epsilonY = Mathf.Abs(targetInstance.Grid.cellSize.y * targetInstance.transform.lossyScale.y) * 0.01f;
        Vector2 samplePosition = placedObject.transform.position;

        if (kind == PlacementKind.Platform)
        {
            if (GetPlatformCellWidth(targetInstance, placedObject, out _) % 2 == 0)
                samplePosition.x += epsilonX;
            samplePosition.y += epsilonY;
        }
        else if ((kind is PlacementKind.Obstacle or PlacementKind.Cover) &&
                 TryGetColliderOffsets(
                     targetInstance,
                     placedObject,
                     out float centerOffsetX,
                     out float bottomOffsetY))
        {
            samplePosition = new Vector2(
                placedObject.transform.position.x + centerOffsetX,
                placedObject.transform.position.y + bottomOffsetY + epsilonY);
        }

        return targetInstance.WorldToCell(samplePosition);
    }

    private static bool TryGetColliderOffsets(
        StageField targetInstance,
        GameObject source,
        out float centerOffsetX,
        out float bottomOffsetY)
    {
        BoxCollider2D collider = source.GetComponentInChildren<BoxCollider2D>(true);
        if (!collider || !TryGetTransformRelativeToRoot(collider.transform, source.transform, out Matrix4x4 relative))
        {
            centerOffsetX = 0f;
            bottomOffsetY = 0f;
            return false;
        }

        Vector2 half = collider.size * 0.5f;
        Vector2 offset = collider.offset;
        Vector2[] corners =
        {
            offset + new Vector2(-half.x, -half.y),
            offset + new Vector2(-half.x, half.y),
            offset + new Vector2(half.x, half.y),
            offset + new Vector2(half.x, -half.y)
        };

        float scaleX = source.transform.lossyScale.x;
        float scaleY = source.transform.lossyScale.y;
        if (!source.transform.IsChildOf(targetInstance.transform))
        {
            scaleX *= targetInstance.transform.lossyScale.x;
            scaleY *= targetInstance.transform.lossyScale.y;
        }

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        foreach (Vector2 corner in corners)
        {
            Vector3 rootLocalCorner = relative.MultiplyPoint3x4(corner);
            float x = rootLocalCorner.x * scaleX;
            float y = rootLocalCorner.y * scaleY;
            minX = Mathf.Min(minX, x);
            maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y);
        }

        centerOffsetX = (minX + maxX) * 0.5f;
        bottomOffsetY = minY;
        return true;
    }

    private static bool TryGetTransformRelativeToRoot(
        Transform child,
        Transform root,
        out Matrix4x4 relative)
    {
        relative = Matrix4x4.identity;
        Transform current = child;
        while (current && current != root)
        {
            relative = Matrix4x4.TRS(
                current.localPosition,
                current.localRotation,
                current.localScale) * relative;
            current = current.parent;
        }

        return current == root;
    }

    private static void ApplyPlatformSortingOrder(GameObject platform, int horizontalCell)
    {
        foreach (SpriteRenderer renderer in platform.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.sortingOrder == horizontalCell)
                continue;

            renderer.sortingOrder = horizontalCell;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static void ApplyCoverSortingOrder(GameObject cover)
    {
        foreach (SpriteRenderer renderer in cover.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.sortingOrder >= MinimumCoverSortingOrder)
                continue;

            renderer.sortingOrder = MinimumCoverSortingOrder;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static void MigrateLegacyHierarchy(StageField targetInstance)
    {
        Transform platformsRoot = targetInstance.PlatformsRoot;
        Transform objectsRoot = targetInstance.ObjectsRoot;
        Transform legacyRoot = targetInstance.transform.Find("ObjectRoot");

        if (legacyRoot)
        {
            foreach (Transform child in legacyRoot.Cast<Transform>().ToList())
            {
                PlacementKind kind = InferPlacedKind(child.gameObject, platformsRoot);
                Undo.SetTransformParent(
                    child,
                    kind == PlacementKind.Platform ? platformsRoot : objectsRoot,
                    "Migrate Stage Object Hierarchy");
            }
        }

        foreach (Transform child in targetInstance.transform.Cast<Transform>().ToList())
        {
            if (!child || child == platformsRoot || child == objectsRoot || child == legacyRoot)
                continue;

            PlacementKind kind = InferPlacedKind(child.gameObject, platformsRoot);
            if (kind == PlacementKind.Platform)
                Undo.SetTransformParent(child, platformsRoot, "Migrate Platform Hierarchy");
            else if (kind is PlacementKind.Obstacle or PlacementKind.Cover)
                Undo.SetTransformParent(child, objectsRoot, "Migrate Gameplay Object Hierarchy");
        }

        if (legacyRoot && legacyRoot.childCount == 0)
            Undo.DestroyObjectImmediate(legacyRoot.gameObject);
    }

    private static IEnumerable<GameObject> EnumeratePlacedRoots(StageField targetInstance)
    {
        Transform platformsRoot = targetInstance.transform.Find("Platforms");
        Transform objectsRoot = targetInstance.transform.Find("Objects");
        Transform legacyRoot = targetInstance.transform.Find("ObjectRoot");

        foreach (Transform child in targetInstance.GetComponentsInChildren<Transform>(true))
        {
            if (!child || child == targetInstance.transform ||
                child == platformsRoot || child == objectsRoot || child == legacyRoot)
                continue;

            GameObject placedObject = child.gameObject;
            if (IsEditorPreview(placedObject))
                continue;

            GameObject nearestPrefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(placedObject);
            if (nearestPrefabRoot && nearestPrefabRoot != placedObject &&
                nearestPrefabRoot != targetInstance.gameObject)
                continue;

            yield return placedObject;
        }
    }

    private static bool IsEditorPreview(GameObject candidate)
    {
        if (!candidate)
            return false;
        if (candidate.GetComponentInParent<StageEditorPreviewMarker>(true))
            return true;

        Transform current = candidate.transform;
        while (current)
        {
            if (current.name == "PreviewObject" ||
                (current.gameObject.hideFlags & HideFlags.HideAndDontSave) != 0)
                return true;
            current = current.parent;
        }
        return false;
    }

    private static string GetPrefabName(GameObject instance)
    {
        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
        if (source)
            return source.name;

        const string cloneSuffix = "(Clone)";
        return instance.name.EndsWith(cloneSuffix)
            ? instance.name[..^cloneSuffix.Length]
            : instance.name;
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
}

/// <summary>오브젝트와 스폰 배치가 공유하는 Scene View 셀 유효성 표시.</summary>
internal static class StageEditorCellOverlay
{
    public static void Draw(StageField targetInstance, IEnumerable<Vector2Int> cells, bool valid)
    {
        Color fill = valid
            ? new Color(0.12f, 0.9f, 0.25f, 0.18f)
            : new Color(0.95f, 0.12f, 0.12f, 0.18f);
        Color outline = new(fill.r, fill.g, fill.b, 0.9f);
        Vector2 cellSize = new(
            Mathf.Abs(targetInstance.Grid.cellSize.x * targetInstance.transform.lossyScale.x),
            Mathf.Abs(targetInstance.Grid.cellSize.y * targetInstance.transform.lossyScale.y));

        foreach (Vector2Int cell in cells)
        {
            Vector2 center = targetInstance.CellToWorldCenter(cell);
            Vector3 half = new(cellSize.x * 0.5f, cellSize.y * 0.5f, 0f);
            Vector3 center3 = new(center.x, center.y, targetInstance.transform.position.z);
            Vector3[] corners =
            {
                center3 + new Vector3(-half.x, -half.y, 0f),
                center3 + new Vector3(-half.x, half.y, 0f),
                center3 + new Vector3(half.x, half.y, 0f),
                center3 + new Vector3(half.x, -half.y, 0f)
            };
            Handles.DrawSolidRectangleWithOutline(corners, fill, outline);
        }
    }
}

/// <summary>페인팅 미리보기를 실제 Stage 배치물 스캔에서 제외하기 위한 표식.</summary>
[DisallowMultipleComponent]
internal sealed class StageEditorPreviewMarker : MonoBehaviour
{
}
#endif
