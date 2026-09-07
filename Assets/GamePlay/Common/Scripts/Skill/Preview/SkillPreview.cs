// using Core.Scripts.Foundation.Define;
// using GamePlay.Common.Scripts.Entities.Skills;
// using GamePlay.Common.Scripts.Skill;
// using GamePlay.Features.Battle.Scripts.Unit;
// using System.Collections.Generic;
// using UnityEngine;
//
// namespace GamePlay.Features.Scripts.Skill.Preview
// {
//     // TODO : outliner 모듈화할 것
//     // TODO : capture 쪽 ppu 포함하여 자동화할 것
//     public class SkillPreview : MonoBehaviour
//     {
//         [SerializeField] private Camera     _textureCaptureCamera;
//         [SerializeField] private GameObject _targetPointer;
//         [SerializeField] private GameObject _rangeCircle;
//         [SerializeField] private SpriteRenderer _spriteRenderer;
//         [Header("Material References")]
//         [SerializeField] private Material outlineMaterial;
//         [SerializeField] private Material normalMaterial;
//         
//         private GameObject _previewFocusSnapshot;
//         private SkillModel _previewSkill;
//         
//         private CharBase _previewFocus;
//         private Vector3 _pivot;     // 무조건 시전자 중심
//         private int _range;         // 시전자로부터의 범위
//         private SystemEnum.ePivot _pivotType;
//         private int _pointerRange;  // 스킬 시전될 중심으로부터의 범위
//         
//         public void InitPreview(CharBase focus, SkillModel skillModel)
//         {
//             _previewFocus = focus;
//             _previewSkill = skillModel;
//             _range = 3;//_previewSkill.SkillRange;
//             _pivot = focus.CharTransform.position;
//             _rangeCircle.transform.localScale = new Vector3(_range * 2f, _range * 2f, 1);
//             //_pivotType = _previewSkill.SkillPivot;
//             //_pointerRange = _previewSkill.SkillHitRange;
//         }
//         
//         private void Update()
//         {
//             Vector2 curPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
//             Vector2 pivot2D = _pivot;
//             Vector2 dir = curPos - pivot2D;
//             
//             #region Restricting by Range
//             float pointerPivotDist = dir.magnitude;
//             if (pointerPivotDist > _range)
//             {
//                 curPos = dir.normalized * _range + pivot2D; 
//             }
//             _targetPointer.transform.position = curPos;
//             #endregion
//             
//             #region Raycast and outlining
//             RaycastHit2D hit = Physics2D.Raycast
//                 (curPos, Vector2.zero, 0f, LayerMask.GetMask("Character"));
//             Collider2D hitCollider = hit.collider;
//             if(hitCollider)
//             {
//                 if (hitCollider.TryGetComponent(out CharBase target))
//                 {
//                     if ((_pivotType == SystemEnum.ePivot.TARGET_SELF && target != _previewFocus) ||
//                         (_pivotType == SystemEnum.ePivot.TARGET_ENEMY && target == _previewFocus))
//                     {
//                         _spriteRenderer.sprite = null;
//                         _spriteRenderer.material = normalMaterial;
//                         return;
//                     }
//                     
//                     if(_previewFocusSnapshot && _previewFocusSnapshot != target.CharSnapShot)
//                         _previewFocusSnapshot.SetActive(false);
//                     _previewFocusSnapshot = target.CharSnapShot;
//                     _previewFocusSnapshot.SetActive(true);
//                     
//                     Capture(_previewFocusSnapshot);
//                     
//                     #region Target Decision
//                     if (Input.GetMouseButtonDown(0))
//                     {
//                         List<CharBase> targets = new();
//                         if (_pointerRange == 0)
//                         {
//                             targets.Add(target);
//                         }
//                         else if (_pointerRange > 0)
//                         {
//                             var center = _pivotType == SystemEnum.ePivot.TARGET_SELF ?
//                                 (Vector2)_pivot : new Vector2(target.CharTransform.position.x, target.CharTransform.position.y);
//                             
//                             Collider2D[] hits = Physics2D.OverlapBoxAll
//                                 (center, new Vector2(_pointerRange, 1), 0f, LayerMask.GetMask("Character"));
//                             foreach (Collider2D col in hits)
//                             {
//                                 if (col.TryGetComponent(out CharBase rangeTarget))
//                                 {
//                                     if (rangeTarget == _previewFocus)
//                                         continue;
//                                     targets.Add(rangeTarget);
//                                 }
//                             }
//                         }
//                         #region 스킬 사용부
//                         _previewFocus.SkillInfo.PlaySkill(_previewSkill.SkillIndex,
//                                 new SkillParameter(
//                                     _previewFocus, 
//                                     targets,
//                                     _previewSkill));
//                             Debug.Log($"Skill Used : {_previewSkill.SkillName}");
//                         
//                         gameObject.SetActive(false);
//                         #endregion
//                     }
//                     #endregion
//                 }
//             }
//             else
//             {
//                 _spriteRenderer.sprite = null;
//                 _spriteRenderer.material = normalMaterial;
//                 if (_previewFocusSnapshot)
//                 {
//                     _previewFocusSnapshot.SetActive(false);
//                     _previewFocusSnapshot = null;
//                 }
//             }
//             #endregion
//             if (Input.GetKeyDown(KeyCode.Escape))
//             {
//                 gameObject.SetActive(false);
//             }   
//         }
//
//         /// <summary>
//         /// 현재 포인팅 중인 타겟의 임시 sprite 생성 및 outlining 실시
//         /// </summary>
//         private void Capture(GameObject target)
//         {
//             Vector3 targetPos = target.transform.position;
//             _textureCaptureCamera.transform.position = 
//                 new Vector3(targetPos.x, targetPos.y, _textureCaptureCamera.transform.position.z);
//             
//             RenderTexture rt = new(512, 512, 16, RenderTextureFormat.ARGB32);
//             rt.filterMode = FilterMode.Point;
//             rt.Create();
//             _textureCaptureCamera.targetTexture = rt;
//
//             _textureCaptureCamera.enabled = true;
//             _textureCaptureCamera.Render();
//             
//             RenderTexture.active = rt;
//             Texture2D tex = new(rt.width, rt.height, TextureFormat.ARGB32, false);
//             tex.filterMode = FilterMode.Point;
//             tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
//             tex.Apply();
//             RenderTexture.active = null;
//             _textureCaptureCamera.targetTexture = null;
//             rt.Release();
//             
//             Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 48f);
//             _spriteRenderer.sprite = sprite;
//             _spriteRenderer.material = outlineMaterial;
//             _spriteRenderer.transform.position = targetPos;
//         }
//     }
// }