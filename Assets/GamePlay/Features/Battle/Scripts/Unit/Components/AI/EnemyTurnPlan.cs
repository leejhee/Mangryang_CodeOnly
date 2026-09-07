using GamePlay.Common.Scripts.Entities.Skills;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.Unit.Components.AI
{
    /// <summary>
    /// 상태 머신의 결정 결과. 실행 순서는 이동 후 상태별 행동이다.
    /// </summary>
    public sealed class EnemyTurnPlan
    {
        public EnemyDecisionState State { get; set; }
        public Vector2Int? MoveTo { get; set; }
        public Vector2Int? TargetCell { get; set; }
        public CharBase Target { get; set; }
        public SkillModel Skill { get; set; }
        public bool FaceRight { get; set; }
        public float Score { get; set; }
        public string Reason { get; set; }

        public override string ToString()
        {
            string move = MoveTo.HasValue ? $"Move({MoveTo.Value}) -> " : string.Empty;
            string target = Target ? $" / {Target.name}" : string.Empty;
            string skill = Skill != null ? $" / {Skill.SkillName}" : string.Empty;
            return $"{State}: {move}{TargetCell?.ToString() ?? "Stay"}{target}{skill} ({Reason})";
        }
    }
}
