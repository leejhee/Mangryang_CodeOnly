using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Contracts.Interaction;
using GamePlay.Contracts.Interaction;
using System.Threading;
using UnityEngine;

namespace GamePlay.Common.Scripts.Interaction
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Interactor : MonoBehaviour, IInteractor
    {
        public Transform Transform => transform;
        public CancellationToken LifeTimeToken => this.GetCancellationTokenOnDestroy();

        public async UniTask TryInteract(IInteractable target, CancellationToken ext)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(LifeTimeToken, ext);
            await target.Interact(this, linked.Token);
        }
        
    }
}