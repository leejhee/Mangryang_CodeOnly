// using System.Collections.Generic;
// using UnityEngine;
// using static Core.Scripts.Foundation.Define.SystemEnum;
//
// namespace AngelBeat
// {
//     public class ExecutionInfo
//     {
//         private Dictionary<eExecutionType, List<ExecutionBase>> _functionBaseDic = new(); // 기능 
//
//         private Queue<ExecutionBase> _functionReadyQueue = new();
//
//         private Queue<ExecutionBase> _functionKillQueue = new();
//
//         public void Init()
//         {
//             for (eExecutionType i = 0; i < eExecutionType.eMax; i++)
//             {
//                 _functionBaseDic[i] = new List<ExecutionBase>();
//             }
//         }
//
//         public void UpdateFunctionDic()
//         {
//             // 준비 큐에서 딕셔너리로 추가
//             while (_functionReadyQueue.Count != 0)
//             {
//                 ExecutionBase target = _functionReadyQueue.Dequeue();
//                 if (!_functionBaseDic[target.ExecutionType].Contains(target))
//                 {
//                     target.InitFunction();
//                     _functionBaseDic[target.ExecutionType].Add(target);
//                 }
//
//             }
//
//             foreach (var functionBaseList in _functionBaseDic)
//             {
//                 foreach (var function in functionBaseList.Value)
//                 {
//                     function.CheckTimeOver();
//                     function.Update(Time.deltaTime);
//                 }
//             }
//
//             // 순회 후 제거 큐로 복사한 타겟 딕셔너리에서 제거 
//             while (_functionKillQueue.Count != 0)
//             {
//                 ExecutionBase target = _functionKillQueue.Dequeue();
//                 if (_functionBaseDic[target.ExecutionType].Contains(target))
//                     _functionBaseDic[target.ExecutionType].Remove(target);
//             }
//         }
//
//         // Function Dictionary로의 접근 통제.
//         public void EnqueueFunction(ExecutionBase target)
//         {
//             _functionReadyQueue.Enqueue(target);
//         }
//
//         public void EnqueueKill(ExecutionBase target)
//         {
//             _functionKillQueue.Enqueue(target);
//         }
//
//         public void AddExecution(ExecutionParameter param)
//         {
//             EnqueueFunction(ExecutionFactory.ExecutionGenerate(param));
//         }
//
//     }
// }
