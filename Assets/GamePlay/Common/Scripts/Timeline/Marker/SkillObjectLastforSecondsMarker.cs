using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace GamePlay.Common.Scripts.Timeline.Marker
{
    public class SkillObjectLastForSecondsMarker : SkillTimeLineMarker
    {
        [SerializeField] private float lastingSeconds;
        [SerializeField] private GameObject lastingObject;
        [SerializeField] private Vector2 offset;
        
        public override async UniTask BuildTaskAsync(CancellationToken ct)
        {
            float timer = 0f;
            if (!lastingObject) return;
            GameObject inst = Instantiate(lastingObject, InputParam.Caster.transform);
            inst.transform.position += new Vector3(offset.x, offset.y, 0f);
            while (timer < lastingSeconds)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                timer += Time.deltaTime;
            }
            Destroy(inst);
        }
    }
}