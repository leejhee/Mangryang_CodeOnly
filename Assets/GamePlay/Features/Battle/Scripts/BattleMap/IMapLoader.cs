using AngelBeat;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.BattleMap
{
    public interface IMapLoader
    {
        UniTask<StageField> InstantiateBattleFieldAsync(
            string stageName=null, Transform parent=null, CancellationToken ct = default);
    }
    
}