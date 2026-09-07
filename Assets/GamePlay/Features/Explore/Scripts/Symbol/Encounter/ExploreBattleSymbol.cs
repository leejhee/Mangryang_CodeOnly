using Core.Scripts.Foundation.Define;
using Core.Scripts.Foundation.Utils;
using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Entities.Character;
using GamePlay.Common.Scripts.Scene;
using GamePlay.Common.Scripts.Transition;
using GamePlay.Features.Battle.Scripts;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Features.Explore.Scripts.Symbol.Encounter
{
    public class ExploreBattleSymbol : EncounterSymbol
    {
        [SerializeField] private SystemEnum.Dungeon dungeon;
        [SerializeField] private List<string> mapNames;
        
        [Header("랜덤 모드 옵션")]
        [Tooltip("true면 mapNames에서 랜덤으로 뽑아서 전투 시퀀스를 구성합니다.\nfalse면 mapNames 순서를 그대로 사용합니다.")]
        [SerializeField] private bool useRandomOrder = true;

        [Tooltip("랜덤 모드일 때 몇 번의 전투를 진행할지")]
        [SerializeField] private int randomBattleCount = 2;
        
        [SerializeField] private bool isEndExploreBattle;
        
        private GameRandom random; //temporary

        protected override bool MarkClearedBeforeEncounter => false;

        public void Configure(SystemEnum.MapSymbolType symbolType)
        {
            if (symbolType == SystemEnum.MapSymbolType.BossBattle)
                isEndExploreBattle = true;
        }
        
        private void Start()
        {
            dungeon = ExploreSession.Instance.TargetDungeon;
        }

        protected override void OnEncounter(ExploreController player)
        {
            Party party = ExploreManager.Instance.playerParty;
            if (party == null)
            {
                Debug.LogError("[ExploreBattleSymbol] Player party is null.");
                return;
            }
            
            List<string> stageList = BuildStageList();
            if (stageList == null || stageList.Count == 0)
            {
                Debug.LogError("[ExploreBattleSymbol] Stage list is empty. 전투를 시작할 수 없습니다.");
                return;
            }
            
            
            #region Move to Battle Scene
            
            //TODO : 추후 씬 트랜지션 지침 받고 이 부분에 기입할 것

            BattleSession.Instance.SetBattleData(
                ExploreSession.Instance.PlayerParty,
                ExploreSession.Instance.TargetDungeon,
                ExploreSession.Instance.TargetFloor,
                stageList,
                SystemEnum.eScene.ExploreScene,
                isEndExploreBattle,
                cellIndex);
            
            ExploreSession.Instance.SetContinueExplore(
                ExploreSession.Instance.TargetDungeon,
                ExploreSession.Instance.TargetFloor,
                ExploreSession.Instance.PlayerParty,
                player.transform.position);
            
            //돌아올 때의 기록
            //GamePlaySceneUtil.LoadBattleScene();
            FlowHelper.LoadBattleSceneWithFadeAsync().Forget();
            #endregion
        }

        private List<string> BuildStageList()
        {
            var result = new List<string>();

            if (mapNames != null && mapNames.Count > 0)
            {
                if (!useRandomOrder)
                {
                    result.AddRange(mapNames);
                    return result;
                }

                if (random == null)
                    random = new GameRandom();

                int count = Mathf.Max(1, randomBattleCount);
                for (int i = 0; i < count; i++)
                {
                    int idx = random.Next(mapNames.Count);
                    result.Add(mapNames[idx]);
                }

                return result;
            }
            
            int autoCount = Mathf.Max(1, randomBattleCount);
            for (int i = 0; i < autoCount; i++)
            {
                result.Add("");   // 내용은 의미 없고, '전투 횟수'만 중요
            }

            return result;
        }
    }
}
