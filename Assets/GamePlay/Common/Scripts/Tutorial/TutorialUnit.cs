using Core.Scripts.GameSave;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UIs.Runtime;
using UnityEngine;

namespace GamePlay.Common.Scripts.Tutorial
{
    [CreateAssetMenu(menuName = "ScriptableObject/TutorialUnit")]
    public class TutorialUnit : ScriptableObject
    {
        public SlotProgressData.TutorialFlag includingTutorial;
        public List<ViewID> tutorialUIList = new();
        
        private Queue<ViewID> _internalQueue = new();
        private void OnEnable()
        {
            foreach (var viewID in tutorialUIList)
            {
                _internalQueue.Enqueue(viewID);
            }
        }

        public async UniTask ProvideTutorialView()
        {
            await UIManager.Instance.ShowViewAsync(_internalQueue.Dequeue());
        }
    }
}