// using UnityEngine;
// using UnityEngine.Playables;
//
// namespace GamePlay.Common.Scripts.Timeline.PlayableBehaviour
// {
//     public class AttackColliderGeneratorPlayableBehavior : SkillTimeLinePlayableBehaviour
//     {
//         public GameObject AttackCollider { get; set; }
//         public Vector3 OffSet { get; set; }
//         public Vector3 Size { get; set; }
//
//         private GameObject _collider = null;
//
//         // 클립이 시작될 때 호출
//         public override void OnBehaviourPlay(Playable playable, FrameData info)
//         {
//             Vector3 vec = OffSet;
//             if (AttackCollider == null)
//                 return;
//
//             _collider = Core.Scripts.Managers.ResourceManager.Instance.Instantiate(AttackCollider, charBase.transform);
//             if (_collider == null)
//                 return;
//
//             _collider.transform.localPosition = vec;
//             _collider.transform.localScale = Size;
//
//             // GetComponent 이래도 되려나....
//             //AttackCollider attackCollider = _collider.GetComponent<AttackCollider>();
//
//             //if (attackCollider == null)
//                 //return;
//
//             //if (!_collider.TryGetComponent(out AttackCollider attackCollider)) return;
//
//             //attackCollider.SetData(charBase,skillBase);
//         }
//
//         // 클립이 멈출 때 호출
//         public override void OnBehaviourPause(Playable playable, FrameData info)
//         {
//             if (_collider == null)
//                 return;
//             GameObject.Destroy(_collider);
//         }
//     }
// }