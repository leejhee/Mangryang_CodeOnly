// using Core.Scripts.Data;
// using Core.Scripts.Foundation.Define;
// using GamePlay.Features.Battle.Scripts.Unit;
// using GamePlay.Features.Scripts.Battle.Unit;
// using UnityEngine;
//
// namespace AngelBeat
// {
//     /// <summary>
//     /// 몇 턴(또는 영구)에 걸쳐 장기적으로 유지되어야 하는 버프의 예다.
//     /// </summary>
//     public abstract class ExecutionBase
//     {
//         protected CharBase _TargetChar = null; 
//         protected CharBase _CastChar = null; // 기능 캐스팅 캐릭터
//         protected ExecutionData _ExecutionData = null; // 기능 데이터
//
//         protected int _LifeTurn = -1;
//         protected int _StartTurn = 0;
//         protected int _RunningTurn = 0;
//
//         public SystemEnum.eExecutionType ExecutionType;
//
//         public ExecutionBase(ExecutionParameter buffParam)
//         {
//             _TargetChar = buffParam.TargetChar;
//             _CastChar = buffParam.CastChar;
//             _ExecutionData = global::Core.Scripts.Managers.DataManager.Instance.GetData<ExecutionData>(buffParam.ExecutionIndex);
//
//             if (_ExecutionData == null)
//             {
//                 Debug.LogError($"Execution : {buffParam.ExecutionIndex} 데이터 획득 실패");
//                 return;
//             }
//
//             ExecutionType = _ExecutionData.executionType;
//         }
//
//         // '턴'으로 바꿔야한다!
//         public virtual void InitFunction() => _StartTurn = (int)Time.time;
//
//
//         /// <summary>
//         /// 버프 시작과 종료
//         /// </summary>
//         /// <param name="StartFunction"> true: 행동 시작 false 행동 종료 </param>
//         public virtual void RunFunction(bool StartFunction = true)
//         {
//             if (StartFunction)
//             {
//                 _TargetChar.ExecutionInfo.EnqueueFunction(this);
//             }
//             else
//             {
//                 _TargetChar.ExecutionInfo.EnqueueKill(this);
//             }
//         }
//
//         public virtual void Update(float delta) { }
//
//         /// <summary>
//         /// 기능 시간 완료 체크
//         /// </summary>
//         public void CheckTimeOver()
//         {
//             if (_LifeTurn == -1f) return;
//
//             int runTime = _RunningTurn - _StartTurn;
//             if (runTime > _LifeTurn || _LifeTurn == 0)
//             {
//                 RunFunction(false);
//             }
//
//         }
//
//         // 구체적 작동은 상속으로 작성할 것.
//     }
//
// }
