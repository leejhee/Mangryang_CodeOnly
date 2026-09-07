using Core.Scripts.Managers;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace GamePlay.Common.Scripts.Timeline.Marker
{
    public class SkillAudioMarker : SkillTimeLineMarker
    {
        [SerializeField] private AudioClip _audioClip;
        
        public override async UniTask BuildTaskAsync(CancellationToken ct)
        {
            SoundManager.Instance.Play(_audioClip);
        }

        protected override void SkillInitialize() { }
    }
}
