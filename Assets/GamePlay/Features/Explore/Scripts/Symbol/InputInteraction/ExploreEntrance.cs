using Cysharp.Threading.Tasks;
using GamePlay.Common.Scripts.Contracts.Interaction;
using GamePlay.Common.Scripts.Interaction;
using GamePlay.Contracts.Interaction;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GamePlay.Features.Explore.Scripts.Symbol.InputInteraction
{
    /// <summary>
    /// 탐사 진입로 오브젝트
    /// </summary>
    public class ExploreEntrance : MonoBehaviour, IInteractable
    {
        public IReadOnlyList<string> Prompt { get; }
        public Transform ParentTransform { get; }
        public int Priority { get; }
        
        // Village 기준 keymap에서는 항상 허용해도 무방.
        public bool Interactable(Interactor targetActor) => true;

        public UniTask Interact(Interactor targetActor, CancellationToken ct)
        {
            throw new System.NotImplementedException();
        }

        public void OnFocusEnter(IInteractor actor)
        {
            //ui 띄워야 한다.
            
        }

        public void OnFocusExit(IInteractor actor)
        {
            throw new System.NotImplementedException();
        }
        
        
    }
}