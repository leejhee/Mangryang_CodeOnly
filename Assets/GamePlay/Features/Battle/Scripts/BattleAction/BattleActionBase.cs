using Cysharp.Threading.Tasks;
using System.Threading;

namespace GamePlay.Features.Battle.Scripts.BattleAction
{
    public abstract class BattleActionBase
    {
        public ActionType ActionType {get; private set;}
        
        protected readonly BattleActionContext Context;

        protected BattleActionBase(BattleActionContext ctx)
        {
            Context = ctx;
            ActionType = ctx.battleActionType;
        }

        public abstract UniTask<BattleActionPreviewData> BuildActionPreview(CancellationToken ct);
        public abstract UniTask<BattleActionResult> ExecuteAction(CancellationToken ct);
    }
}