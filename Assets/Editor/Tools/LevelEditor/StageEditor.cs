#if UNITY_EDITOR
using GamePlay.Features.Battle.Scripts.BattleMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AngelBeat.Tools.LevelEditor
{
    /// <summary>
    /// StageEditor는 Stage(SpawnPoint) 에디팅과 오브젝트 배치를 위한 EditorWindow입니다.
    /// 내부에서는 SpawnPointEditor와 ObjectPlacer로 GUI와 기능을 위임합니다.
    /// </summary>
    public class StageEditor : EditorWindow
    {
        private enum ePlacementMode
        {
            SpawnPoint,
            Object
        }

        private const string TARGET_SCENE_PATH = "Assets/Scenes/Editing/LevelEditingScene.unity";
        private const string PREF_SPAWN_INDICATOR = "PREF_SPAWN_INDICATOR";

        /// <summary> 현재 편집할 맵의 프리팹이다. </summary>
        /// <remarks> <b>저장 및 로드 용도로만 사용함에 주의</b> </remarks>
        [SerializeField]
        private StageField targetStageMap;

        /// <summary> 현재 올라가 있는 프리팹의 인스턴스이다. </summary>
        /// <remarks> <b>이거로만 내부에서 편집 작업한다!</b> </remarks>
        [SerializeField]
        private StageField targetInstance;

        /// <summary> 배치 모드. 무엇을 배치할 건가요? </summary>
        [SerializeField]
        private ePlacementMode currentPlacementMode = ePlacementMode.SpawnPoint;

        /// <summary> 에디터 내 기능별 모듈 1 : SpawnPoint를 지정 </summary>
        private SpawnPointEditor spawnPointEditor;

        /// <summary> 에디터 내 기능별 모듈 2 : Object를 배치 </summary>
        [SerializeField]
        private StageObjectEditor objectPlacer;

        #region SpawnPointEditor 초기화용
        private SpawnIndicator spawnIndicatorPrefab;
        #endregion

        private Vector2 scrollPosition;

        [MenuItem("Tools/Stage Editor")]
        public static void ShowWindow()
        {
            StageEditor window = GetWindow<StageEditor>();
            window.titleContent = new GUIContent("Stage Editor");
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(TARGET_SCENE_PATH);
            else
                window.Close();
        }

        private void OnEnable()
        {
            #region Module Initialization
            LoadSpawnIndicatorPrefab();
            spawnPointEditor = new SpawnPointEditor(spawnIndicatorPrefab);
            spawnPointEditor.LoadColorPreferences();

            objectPlacer = new StageObjectEditor();
            objectPlacer.LoadPrefabList();
            if (targetInstance)
                spawnPointEditor.RestoreSpawnIndicators(targetInstance);
            #endregion

            #region Basic Subscribe
            EditorSceneManager.sceneOpened += OnSceneOpened;        
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Undo.undoRedoPerformed += OnUndoRedo;
            #endregion

            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SaveSpawnIndicatorPrefab();
            spawnPointEditor?.SaveColorPreferences();
            objectPlacer?.SavePrefabList();
            objectPlacer?.StopPainting();

            EditorSceneManager.sceneOpened -= OnSceneOpened;
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnGUI()
        {
            HandleKeyboardShortcuts();

            // 모드 선택 토글
            ePlacementMode nextPlacementMode = (ePlacementMode)GUILayout.Toolbar((int)currentPlacementMode,
                new string[] { "Spawn Point Mode", "Object Mode" });
            if (nextPlacementMode != currentPlacementMode)
            {
                objectPlacer.StopPainting();
                currentPlacementMode = nextPlacementMode;
            }

            EditorGUILayout.HelpBox(
                "Scene View에서 좌클릭으로 배치합니다. Alt+드래그는 Scene View 탐색에 그대로 사용할 수 있습니다.",
                MessageType.Info);
            GUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // StageMap 선택 및 로드
            EditorGUILayout.LabelField("레벨 디자인 맵 선택", EditorStyles.boldLabel);
            StageField newStageMap = EditorGUILayout.ObjectField(
                "Target StageMap", targetStageMap, typeof(StageField), false) as StageField;
            if (newStageMap != targetStageMap)
                TryChangeStageMap(newStageMap);

            DrawMapSettings();

            GUILayout.Space(10);

            // 현재 배치 모드에 따른 GUI 위임
            switch (currentPlacementMode)
            {
                case ePlacementMode.SpawnPoint:
                    spawnPointEditor.DrawGUI(targetInstance);
                    break;
                case ePlacementMode.Object:
                    objectPlacer.DrawGUI(targetInstance);
                    break;
            }

            EditorGUILayout.EndScrollView();
            GUILayout.FlexibleSpace();

            bool hasChanges = HasUnsavedChanges();
            if (targetInstance)
            {
                EditorGUILayout.HelpBox(
                    hasChanges ? "저장되지 않은 변경 사항이 있습니다." : "모든 변경 사항이 저장되었습니다.",
                    hasChanges ? MessageType.Warning : MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(!targetInstance))
            {
                if (GUILayout.Button("Save Map  (Ctrl/Cmd + S)", GUILayout.Height(28f)))
                    SaveMap();
            }
            if (GUILayout.Button("Close Editor"))
            {
                TryCloseEditor();
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (targetInstance == null) return;
            if (Event.current.type == EventType.Layout)
                objectPlacer.SynchronizeGridDataIfNeeded(targetInstance);
            switch (currentPlacementMode)
            {
                case ePlacementMode.SpawnPoint:
                    spawnPointEditor.OnSceneGUI(sceneView, targetInstance);
                    break;
                case ePlacementMode.Object:
                    objectPlacer.OnSceneGUI(
                        sceneView,
                        targetInstance,
                        spawnPointEditor.GetOccupiedCells(targetInstance));
                    break;
            }
        }

        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            if (scene.path != TARGET_SCENE_PATH)
            {
                EditorApplication.delayCall += () => {
                    EditorSceneManager.OpenScene(TARGET_SCENE_PATH);
                    Debug.LogError("목표 씬 외 이동은 허용되지 않습니다.");
                };
            }
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                if (SceneManager.GetActiveScene().path == TARGET_SCENE_PATH)
                {
                    EditorApplication.isPlaying = false;
                    Debug.LogError("LevelEditingScene에서는 게임 실행이 불가합니다.");
                }
            }
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Debug.Log("게임 실행으로 Stage Editor 창이 닫힙니다.");
                Close();
            }
        }

        private void LoadStageMapIntoScene()
        {
            if (SceneManager.GetActiveScene().path != TARGET_SCENE_PATH)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(TARGET_SCENE_PATH);
                else
                {
                    Debug.LogWarning("씬 전환 취소: 현재 씬 저장 안됨.");
                    return;
                }
            }

            objectPlacer.StopPainting();
            spawnPointEditor.ClearAllIndicators();

            if (targetInstance)
            {
                Undo.ClearUndo(targetInstance);
                DestroyImmediate(targetInstance.gameObject);
                targetInstance = null;
            }

            if (!targetStageMap)
                return;

            GameObject targetGO = PrefabUtility.InstantiatePrefab(targetStageMap.gameObject) as GameObject;
            if (!targetGO)
            {
                Debug.LogError("StageMap 프리팹 인스턴스를 생성하지 못했습니다.");
                return;
            }

            targetInstance = targetGO.GetComponent<StageField>();
            SceneManager.MoveGameObjectToScene(targetInstance.gameObject, SceneManager.GetActiveScene());
            Undo.RegisterCreatedObjectUndo(targetInstance.gameObject, "Load StageMap for Editing");
            targetInstance.RefreshGridProviderSpec();
            objectPlacer.SynchronizeGridDataIfNeeded(targetInstance);
            Debug.Log("StageMap이 로드되었습니다.");
            spawnPointEditor.RestoreSpawnIndicators(targetInstance);
        }

        private void DrawMapSettings()
        {
            if (!targetInstance)
                return;

            SerializedObject serializedStage = new(targetInstance);
            serializedStage.Update();
            SerializedProperty gridSizeProperty = serializedStage.FindProperty("gridSize");
            if (gridSizeProperty == null)
                return;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(gridSizeProperty, new GUIContent("Map Size"));
            if (EditorGUI.EndChangeCheck())
            {
                gridSizeProperty.vector2IntValue = new Vector2Int(
                    Mathf.Max(1, gridSizeProperty.vector2IntValue.x),
                    Mathf.Max(1, gridSizeProperty.vector2IntValue.y));
                serializedStage.ApplyModifiedProperties();
                targetInstance.RefreshGridProviderSpec();
                SceneView.RepaintAll();
            }

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.Vector2Field("Cell Size (Fixed)", targetInstance.Grid.cellSize);
        }

        private void SaveSpawnIndicatorPrefab()
        {
            if (spawnPointEditor != null)
                spawnIndicatorPrefab = spawnPointEditor.SpawnIndicatorPrefab;

            if (spawnIndicatorPrefab != null)
            {
                string path = AssetDatabase.GetAssetPath(spawnIndicatorPrefab);
                EditorPrefs.SetString(PREF_SPAWN_INDICATOR, path);
            }
            else
            {
                EditorPrefs.DeleteKey(PREF_SPAWN_INDICATOR);
            }
        }

        private void LoadSpawnIndicatorPrefab()
        {
            if (EditorPrefs.HasKey(PREF_SPAWN_INDICATOR))
            {
                string path = EditorPrefs.GetString(PREF_SPAWN_INDICATOR);
                spawnIndicatorPrefab = AssetDatabase.LoadAssetAtPath<SpawnIndicator>(path);
            }
        }

        private bool SaveMap()
        {
            if (!targetInstance)
            {
                Debug.LogError("StageMap이 설정되지 않았습니다. 저장 불가.");
                return false;
            }

            spawnPointEditor.SaveSpawnPoints(targetInstance);
            objectPlacer.SaveObjects(targetInstance);
            targetInstance.RefreshGridProviderSpec();

            // 저장 전에 Spawn Point 데이터 업데이트 (필요에 따라 spawnPointEditor에서 저장)
            if (PrefabUtility.IsPartOfPrefabInstance(targetInstance))
            {
                PrefabUtility.ApplyPrefabInstance(targetInstance.gameObject, InteractionMode.UserAction);
                AssetDatabase.SaveAssets();
                Debug.Log("StageMap 프리팹 인스턴스 변경사항이 저장되었습니다.");
                ShowNotification(new GUIContent("StageMap 저장 완료"));
                Repaint();
                return true;
            }

            Debug.LogWarning("씬 오브젝트일 가능성이 높습니다.");
            return false;
        }

        private void TryCloseEditor()
        {
            if (!HasUnsavedChanges())
            {
                ClearEnvironmentAndClose();
                return;
            }
            int choice = EditorUtility.DisplayDialogComplex("변경 사항 감지됨", "변경된 내용이 있습니다. 저장 후 닫으시겠습니까?", "예 (저장 후 닫기)", "아니오 (저장 없이 닫기)", "취소");
            switch (choice)
            {
                case 0:
                    if (SaveMap())
                        ClearEnvironmentAndClose();
                    break;
                case 1:
                    ClearEnvironmentAndClose();
                    break;
                default:
                    break;
            }
        }

        private bool HasUnsavedChanges()
        {
            if (!targetInstance || !targetStageMap)
                return false;

            return spawnPointEditor.HasChanges(targetInstance) ||
                   PrefabUtility.HasPrefabInstanceAnyOverrides(targetInstance.gameObject, false);
        }

        private void ClearEnvironmentAndClose()
        {
            spawnPointEditor.ClearAllIndicators();
            objectPlacer.StopPainting();
            if (targetInstance)
            {
                Undo.ClearUndo(targetInstance);
                DestroyImmediate(targetInstance.gameObject);
                targetInstance = null;
            }
            Close();
        }

        private void OnUndoRedo()
        {
            spawnPointEditor.CleanupIndicators();
            if (targetInstance)
                objectPlacer.SynchronizeGridDataIfNeeded(targetInstance);
            Repaint();
        }

        private void OnHierarchyChange()
        {
            if (!targetInstance || objectPlacer == null || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            objectPlacer.SynchronizeGridDataIfNeeded(targetInstance);
            Repaint();
        }

        private void TryChangeStageMap(StageField newStageMap)
        {
            if (HasUnsavedChanges())
            {
                int choice = EditorUtility.DisplayDialogComplex(
                    "StageMap 변경",
                    "현재 StageMap에 저장되지 않은 변경 사항이 있습니다.",
                    "저장 후 변경",
                    "변경 사항 버리기",
                    "취소");
                if (choice == 2)
                    return;
                if (choice == 0 && !SaveMap())
                    return;
            }

            targetStageMap = newStageMap;
            LoadStageMapIntoScene();
        }

        private void HandleKeyboardShortcuts()
        {
            Event currentEvent = Event.current;
            if (currentEvent.type != EventType.KeyDown ||
                currentEvent.keyCode != KeyCode.S ||
                (!currentEvent.control && !currentEvent.command))
                return;

            if (targetInstance)
                SaveMap();
            currentEvent.Use();
        }
    }

}



#endif
