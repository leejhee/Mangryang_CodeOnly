using System.Threading;
using UnityEngine;

namespace GamePlay.Contracts.Interaction
{
    public interface IInteractor
    {
        Transform Transform { get; }
        CancellationToken LifeTimeToken { get; }
    }
}