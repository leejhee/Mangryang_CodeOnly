using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace GamePlay.Features.Battle.Scripts.BattleMap
{
    public class StageLoader : IMapLoader
    {
        private readonly IBattleSceneSource _sceneSource;
        private readonly BattleFieldDB _db;
        
        public StageLoader(IBattleSceneSource source, BattleFieldDB db)
        {
            _sceneSource = source ?? throw new ArgumentNullException(nameof(source));
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Load에서 바로 instantiate로 변경 : Load만 하고 들고만 있진 않을 것 같아서.
        /// </summary>
        public async UniTask<StageField> InstantiateBattleFieldAsync(
            string stageName = null, Transform parent = null, CancellationToken ct = default)
        {
            BattleFieldGroup group = _db.Resolve(_sceneSource.Dungeon);
            if (!group)
            {
                throw new Exception($"BattleFieldGroup not found for {_sceneSource.Dungeon}");
            }

            AssetReferenceT<GameObject> stageRef;
           //if (stageRef == null) throw new Exception($"Stage not found : {stageName}");
            
            if (!string.IsNullOrEmpty(stageName))
            {
               stageRef = group.GetStageRef(stageName);
               if (stageRef == null)
                   throw new Exception($"Stage not found : {stageName}");
            }
            else
            {
               int stageIndex = _sceneSource.StageIndex;
               if (stageIndex < 0 || stageIndex >= group.StageCount)
                   throw new Exception(
                       $"No more stages available in group {_sceneSource.Dungeon}. " +
                       $"Requested index = {stageIndex}, Count = {group.StageCount}");

               stageRef = group.GetStageRefByIndex(stageIndex);
               if (stageRef == null)
                   throw new Exception(
                       $"Stage not found at index {stageIndex} in group {_sceneSource.Dungeon}");
            }

            GameObject go = await ResourceManager.Instance.InstantiateAsync(stageRef, parent, false, ct);
            StageField field = go.GetComponent<StageField>();
            return !field ? throw new Exception($"Stage Component Missing : {stageName}") : field;
        }
    }
}
