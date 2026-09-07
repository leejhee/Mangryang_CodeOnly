using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Interaction;
using GamePlay.Contracts.Interaction;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GamePlay.Common.Scripts.Contracts.Interaction
{
    public interface IInteractable
    {
        IReadOnlyList<string> Prompt { get; }
        Transform ParentTransform { get; }
        int Priority { get; } // 겹칠 경우
        bool Interactable(Interactor targetActor);
        UniTask Interact (Interactor targetActor, CancellationToken ct);

        void OnFocusEnter(IInteractor actor);
        void OnFocusExit(IInteractor actor);
    }
}