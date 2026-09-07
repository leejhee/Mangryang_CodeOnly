using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Contracts;
using GamePlay.Common.Scripts.Entities.Skills;
using GamePlay.Features.Battle.Scripts.BattleAction;
using GamePlay.Features.Battle.Scripts.BattleMap;
using GamePlay.Features.Battle.Scripts.Tutorial;
using System.Linq;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.Unit.Components.AI
{
    public class CharTutorialAI : CharacterAI
    {
        private readonly List<EnemyScriptCommand> _commands;
        private int _scriptIndex;

        public CharTutorialAI(CharBase owner, List<EnemyScriptCommand> commands)
            : base(owner)
        {
            _commands = commands;
        }

        protected override async UniTask ExecuteTurnInternal()
        {
            if (_scriptIndex >= _commands.Count)
            {
                // 더 스크립트 없으면 대기 후 턴 종료
                await UniTask.Delay(300);
                return;
            }

            foreach (EnemyScriptCommand cmd in _commands)
            {
                Vector2Int currentCell = Grid.WorldToCell(Owner.CharTransform.position);
                Vector2Int targetCell = cmd.useAbsoluteCell
                    ? cmd.targetCell
                    : currentCell + cmd.relativeOffset;

                switch (cmd.actionType)
                {
                    case ActionType.Move:
                        await ExecuteMove(targetCell);
                        break;

                    case ActionType.Jump:
                        await ExecuteJump(targetCell);
                        break;

                    case ActionType.Push:
                        await ExecutePush(targetCell);
                        break;

                    case ActionType.Skill:
                        {
                            IDamageable target = FindTargetForCommand(cmd, targetCell);
                            SkillModel model = Owner.SkillInfo.SkillSlots.FirstOrDefault(x => x.SkillIndex == cmd.skillIndex);
                            if (target != null && model != null)
                            {
                                List<IDamageable> list = new() { target };
                                TutorialHitRule old = BattleTutorialRules.HitRule;
                                if (cmd.overrideHitRule)
                                    BattleTutorialRules.HitRule = cmd.hitRule;
                                try
                                {
                                    await ExecuteSkill(model, targetCell, list);
                                }
                                finally
                                {
                                    BattleTutorialRules.HitRule = old;
                                }
                            }
                            break;
                        }
                }
                await UniTask.Delay(300);
            }
        }

        private static IDamageable FindTargetForCommand(EnemyScriptCommand cmd, Vector2Int targetCell)
        {
            if (cmd.actionType != ActionType.Skill) return null;
            
            BattleStageGrid grid = BattleController.Instance.StageGrid;
            FieldCover cover = grid.GetCoverAt(targetCell);
            if (cover) return cover;
            CharBase unit = grid.GetUnitAt(targetCell);
            if (unit) return unit;
            FieldObstacle obstacle = grid.GetObstacleAt(targetCell);
            return obstacle ? obstacle : null;
        }
    }
}
