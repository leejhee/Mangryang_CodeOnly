using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Skill;
using System.Threading;

namespace GamePlay.Common.Scripts.Timeline.Marker
{
    public class SkillKeywordMarker : SkillTimeLineMarker
    {
        public override UniTask BuildTaskAsync(CancellationToken ct) =>
            PublishCueAsync(SkillTimelineCue.ApplyKeywordGrants, ct);
    }
}
