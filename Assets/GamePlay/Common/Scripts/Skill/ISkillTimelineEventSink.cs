using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Entities.Skills;
using System.Threading;

namespace GamePlay.Common.Scripts.Skill
{
    /// <summary>
    /// Timeline은 연출 타이밍만 알리고, 실제 효과는 이 수신기가 처리합니다.
    /// </summary>
    public enum SkillTimelineCue
    {
        Impact,
        Push,
        ImpactAndPush,
        ApplyKeywordGrants
    }

    public interface ISkillTimelineEventSink
    {
        UniTask PublishAsync(
            SkillTimelineCue cue,
            SkillModel model,
            CancellationToken ct);
    }
}
