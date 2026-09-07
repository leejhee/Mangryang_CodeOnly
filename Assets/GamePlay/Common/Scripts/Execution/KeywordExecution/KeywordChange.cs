// using AngelBeat;
// using Core.Scripts.Data;
// using Core.Scripts.Foundation.Define;
// using GamePlay.Contracts;
// using GamePlay.Features.Scripts.Keyword;
// using DataManager = Core.Scripts.Managers.DataManager;
//
// namespace GamePlay.Features.Scripts.Execution.KeywordExecution
// {
//     public class KeywordChange : ExecutionBase
//     {
//         private SystemEnum.eKeyword keywordType;
//         private long effectCount;
//         private long effectValue;
//         
//         public KeywordChange(ExecutionParameter buffParam) : base(buffParam)
//         {
//             var data = DataManager.Instance.GetData<ExecutionData>(buffParam.ExecutionIndex);
//             keywordType = (SystemEnum.eKeyword)(data.input1);
//             effectCount = data.input2;
//             effectValue = data.input3;
//         }
//
//         public override void RunFunction(bool StartFunction = true)
//         {
//             base.RunFunction(StartFunction);
//             if (StartFunction)
//             {
//                 var keyword = DataManager.Instance.KeywordMap[keywordType];
//                 var keywordBase = KeywordFactory.CreateKeyword(keyword);
//                 //keywordBase.SyncTo(new KeywordRuntime());
//                 _TargetChar.KeywordInfo.AddKeyword(keywordBase);
//             }
//             
//         }
//     }
// }