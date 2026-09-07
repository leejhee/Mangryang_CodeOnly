using GamePlay.Features.Battle.Scripts.BattleAction;
using GamePlay.Features.Battle.Scripts.Tutorial;
using System;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.Unit.Components.AI
{
    [Serializable]
    public class EnemyScriptCommand
    {
        public int round;                  // 몇 라운드의 내 턴?
        public ActionType actionType;
        public int skillIndex;             // SkillIndex
    
        public bool useAbsoluteCell;
        public Vector2Int targetCell;      // 절대 좌표
        public Vector2Int relativeOffset;  // 현재 셀 기준 상대 좌표
         
        [Header("선택: 이 스텝에서 명중/회피를 강제할지")]
        public bool overrideHitRule;
        public TutorialHitRule hitRule = TutorialHitRule.None;
    }
}