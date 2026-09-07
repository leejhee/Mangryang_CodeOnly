// using Core.Scripts.Foundation;
// using Modules.RoguelikeNodeMap.MapSkeleton;
// using UnityEngine;
// using static Core.Scripts.Foundation.Define.SystemEnum;
//
// namespace Modules.RoguelikeNodeMap
// {
//     /// <summary>
//     /// 탐험 단계에서 스테이지를 관리하는 역할. 스테이지 View에 대한 Controller 역할
//     /// </summary>
//     public class StageManager : SingletonObject<StageManager>
//     {
//         private MapParameterList _parameters;
//         private int stageNum = 0;
//
//         private Map _stageMap;
//         public Map StageMap { get { return _stageMap; } }
//
//         #region 생성자
//         StageManager() { }
//         #endregion
//
//         public override void Init()
//         {
//             _parameters = Core.Scripts.Managers.ResourceManager.Instance.Load<MapParameterList>
//                 ("ScriptableObjects/MapParameterList");
//         }
//
//         /// <summary> 탐험 시작 시 호출 </summary>
//         public void SetStage(int stageNum, bool isFirst = false)
//         {
//             if (isFirst) 
//             { 
//                 MapParameter param = _parameters.Objects[stageNum];
//                 _stageMap = MapGenerator.CreateMap(param);
//                 _stageMap.DebugMap();
//             }
//             else
//             {
//                 //불러오기. 추후 여기에 작업 예정
//             }
//
//             // 맵 불러오기
//
//         }
//
//         /// <summary> 노드 이벤트 진행 시 호출 </summary>
//         /// [TODO] : 저 미친 방식 개선 필요.
//         public void ProceedStage(eNodeType nodeType)
//         {
//             switch(nodeType)
//             {
//                 case eNodeType.Hospital:
//                     Debug.Log("Hospital 노드 방문");
//                     break;
//                 case eNodeType.Resistance:
//                     Debug.Log("Resistance 노드 방문");
//                     break;
//                 case eNodeType.Treasure:
//                     Debug.Log("Treasure 노드 방문");
//                     break;
//                 case eNodeType.Inn:
//                     Debug.Log("Inn 노드 방문");
//                     break;
//                 case eNodeType.Hazard:
//                     Debug.Log("Hazard 노드 방문");
//                     break;
//                 case eNodeType.Combat:
//                     Debug.Log("Combat 노드 방문");
//                     break;
//                 default:
//                     break;
//             }
//         }
//     
//         /// <summary> 탐험 이탈 시 호출 </summary>
//         public void ExitStage()
//         {
//             //저장하기. 추후 여기에 작업 예정
//         }
//     }
// }