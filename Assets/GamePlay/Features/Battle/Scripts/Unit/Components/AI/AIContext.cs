using Core.Scripts.Foundation.Define;
using GamePlay.Features.Battle.Scripts.BattleAction;
using GamePlay.Features.Battle.Scripts.BattleMap;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.Unit.Components.AI
{
    /// <summary>
    /// 한 번의 의사결정에 필요한 전장 스냅샷.
    /// 행동이 실행된 뒤에는 다시 AnalyzeSituation을 호출해 갱신한다.
    /// </summary>
    public sealed class AIContext
    {
        public CharBase Actor { get; }
        public BattleStageGrid Grid { get; }

        public bool LowHP { get; private set; }
        public CharBase PrimaryTarget { get; private set; }
        public bool HasPrimaryTarget => PrimaryTarget;
        public Vector2Int CurrentCell { get; private set; }
        public int AvailableMoveRange { get; private set; }
        public List<Vector2Int> MovableCells { get; } = new();
        public List<CharBase> AllEnemies { get; private set; } = new();

        public AIContext(CharBase actor, BattleStageGrid grid)
        {
            Actor = actor ? actor : throw new ArgumentNullException(nameof(actor));
            Grid = grid ? grid : throw new ArgumentNullException(nameof(grid));
        }

        public void AnalyzeSituation()
        {
            CurrentCell = Grid.WorldToCell(Actor.CharTransform.position);
            AvailableMoveRange = Mathf.Max(0, (int)Actor.RuntimeStat.GetStat(SystemEnum.eStats.NACTION_POINT));

            MovableCells.Clear();
            BattleActionPreviewData moveData = BattleRangeHelper.ComputeMoveRangeFromPos(
                Grid,
                CurrentCell,
                AvailableMoveRange);
            MovableCells.Add(CurrentCell);
            foreach (Vector2Int cell in moveData.PossibleCells)
            {
                if (!MovableCells.Contains(cell))
                    MovableCells.Add(cell);
            }

            AllEnemies = BattleCharManager.Instance.GetEnemies(Actor.GetCharType());
            AllEnemies.RemoveAll(enemy => !enemy || enemy.CurrentHP <= 0);

            LowHP = Actor.MaxHP > 0 && Actor.CurrentHP <= Actor.MaxHP * 0.3f;
            PrimaryTarget = FindClosestEnemy(CurrentCell);
        }

        public CharBase FindClosestEnemy(Vector2Int from)
        {
            CharBase best = null;
            int bestDistance = int.MaxValue;
            float bestHpRatio = float.MaxValue;

            foreach (CharBase enemy in AllEnemies)
            {
                if (!enemy || enemy.CurrentHP <= 0)
                    continue;

                int distance = Distance(from, CellOf(enemy));
                float hpRatio = enemy.MaxHP > 0 ? enemy.CurrentHP / enemy.MaxHP : 1f;
                if (distance < bestDistance ||
                    (distance == bestDistance && hpRatio < bestHpRatio))
                {
                    best = enemy;
                    bestDistance = distance;
                    bestHpRatio = hpRatio;
                }
            }

            return best;
        }

        public int DistanceToNearestEnemy(Vector2Int from)
        {
            int best = int.MaxValue;
            foreach (CharBase enemy in AllEnemies)
            {
                if (!enemy || enemy.CurrentHP <= 0)
                    continue;
                best = Mathf.Min(best, Distance(from, CellOf(enemy)));
            }

            return best == int.MaxValue ? 0 : best;
        }

        public Vector2Int CellOf(CharBase character) =>
            Grid.WorldToCell(character.CharTransform.position);

        public static int Distance(Vector2Int lhs, Vector2Int rhs) =>
            Mathf.Abs(lhs.x - rhs.x) + Mathf.Abs(lhs.y - rhs.y);
    }
}
