// using Core.Scripts.Foundation.Define;
// using GamePlay.Features.Scripts.Execution.KeywordExecution;
//
// namespace AngelBeat
// {
//     public static class ExecutionFactory
//     {
//         //execution type에 따라 만들어야함. 종류 많을 예정
//       
//         public static ExecutionBase ExecutionGenerate(ExecutionParameter buffParam)
//         {
//             switch (buffParam.eExecutionType)
//             {
//                 case SystemEnum.eExecutionType.STACK_CHANGE : return new KeywordChange(buffParam);
//                 default:
//                     return null;
//             }
//
//         }
//
//     
//     }
// }