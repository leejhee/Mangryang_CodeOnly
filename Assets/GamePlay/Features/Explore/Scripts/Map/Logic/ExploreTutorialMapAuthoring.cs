// Assets/GamePlay/Features/Explore/Scripts/Map/Logic/TutorialMapAuthoring.cs
using Core.Scripts.Foundation.Define;
using Cysharp.Threading.Tasks;
using GamePlay.Features.Explore.Scripts.Map.Data;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GamePlay.Features.Explore.Scripts.Map.Logic
{
    /// <summary>
    /// 튜토리얼용 ExploreGrid 프리팹에서
    /// Floor/Wall/심볼들을 스캔해 ExploreMapSkeleton으로 굽는 Authoring 스크립트.
    /// </summary>
    public class ExploreTutorialMapAuthoring : MonoBehaviour
    {
        [Header("타일맵")]
        [SerializeField] private Tilemap floorTilemap;
        [SerializeField] private Tilemap wallTilemap;   // 없어도 되면 비워둬도 됨

        [Header("심볼들이 들어있는 루트 (여러 개면 배열로)")]
        [SerializeField] private Transform[] symbolRoots;

        [Header("메타 정보")]
        [SerializeField] private ExploreMapConfig mapConfig;          // 이 튜토리얼이 속한 던전/층 정보
        [SerializeField] private ExploreTutorialAsset outputAsset;   // 결과를 저장할 SO

#if UNITY_EDITOR
        [ContextMenu("Bake Skeleton From Prefab")]
        private void BakeSkeletonFromPrefab()
        {
            if (outputAsset == null)
            {
                Debug.LogError("[TutorialMapAuthoring] outputAsset이 비어 있음");
                return;
            }
            if (floorTilemap == null)
            {
                Debug.LogError("[TutorialMapAuthoring] floorTilemap이 필요함");
                return;
            }
            if (mapConfig == null)
            {
                Debug.LogError("[TutorialMapAuthoring] mapConfig를 지정해야 던전/층 정보를 넣을 수 있음");
                return;
            }

            // 1) 타일맵 Bounds로 크기 계산
            var bounds = floorTilemap.cellBounds;
            int width  = bounds.size.x;
            int height = bounds.size.y;

            if (width <= 0 || height <= 0)
            {
                Debug.LogError("[TutorialMapAuthoring] floorTilemap에 타일이 없음");
                return;
            }

            // 2) Skeleton 생성
            var skel = new ExploreMapSkeleton(
                dungeonName: mapConfig.dungeonName.ToString(),
                floor: mapConfig.floor,
                width: width,
                height: height,
                seed: 0   // 튜토리얼은 고정이므로 아무 값이나
            );

            // 일단 전체를 Wall로 초기화
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                skel.SetCellType(x, y, SystemEnum.MapCellType.Wall);
            }

            // 3) Floor 타일을 Floor로 세팅 (좌표를 0-based로 정규화)
            foreach (var cellPos in bounds.allPositionsWithin)
            {
                if (!floorTilemap.HasTile(cellPos)) continue;

                int x = cellPos.x - bounds.xMin;
                int y = cellPos.y - bounds.yMin;

                skel.SetCellType(x, y, SystemEnum.MapCellType.Floor);
            }

            // (선택) wallTilemap이 있으면 추가로 벽 지정하고 싶을 때 여기서 처리

            // 4) 심볼 스캔
            var grid = floorTilemap.layoutGrid;
            if (symbolRoots != null)
            {
                foreach (var root in symbolRoots)
                {
                    if (root == null) continue;

                    var authors = root.GetComponentsInChildren<ExploreSymbolAuthoring>(true);
                    foreach (var author in authors)
                    {
                        Vector3 worldPos = author.transform.position;
                        var cell = grid.WorldToCell(worldPos);

                        int x = cell.x - bounds.xMin;
                        int y = cell.y - bounds.yMin;

                        if (!skel.InBounds(x, y))
                        {
                            Debug.LogWarning($"[TutorialMapAuthoring] 심볼 {author.name} 이 Skeleton 범위 밖 ({x},{y})");
                            continue;
                        }

                        AddSymbolToSkeleton(skel, x, y, author);
                    }
                }
            }

            // 5) SO에 저장
            outputAsset.skeleton = skel;
            EditorUtility.SetDirty(outputAsset);
            AssetDatabase.SaveAssets();

            Debug.Log($"[TutorialMapAuthoring] Skeleton bake 완료 - Size {width}x{height}");
        }

        /// <summary>
        /// Authoring 정보에 따라 적절한 TryAddXXXSymbol 호출
        /// </summary>
        private void AddSymbolToSkeleton(ExploreMapSkeleton skel, int x, int y, ExploreSymbolAuthoring author)
        {
            var type = author.symbolType;

            // Event
            if (type == SystemEnum.MapSymbolType.Event && author.useEventType)
            {
                if (!skel.TryAddEventSymbol(x, y, author.eventType, out _, out var err))
                {
                    Debug.LogWarning($"[TutorialMapAuthoring] Event 심볼 추가 실패 ({x},{y}) : {err}");
                }
                return;
            }

            // Item
            if (type == SystemEnum.MapSymbolType.Item && author.useItemIndex)
            {
                if (!skel.TryAddItemSymbol(x, y, author.itemIndex, out _, out var err))
                {
                    Debug.LogWarning($"[TutorialMapAuthoring] Item 심볼 추가 실패 ({x},{y}) : {err}");
                }
                return;
            }

            // 나머지는 전부 SimpleSymbol로
            if (!skel.TryAddSimpleSymbol(x, y, type, out _, out var simpleErr))
            {
                Debug.LogWarning($"[TutorialMapAuthoring] Simple 심볼 추가 실패 ({x},{y}) : {simpleErr}");
            }

            // author.novelId는 지금은 안 쓰지만,
            // 나중에 SkeletonSymbol에 NovelId 같은 필드를 추가해서 여기서 세팅해도 됨.
        }
#endif
    }
}
