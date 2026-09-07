using Core.Scripts.Foundation.Define;
using GamePlay.Common.Scripts.Contracts;
using GamePlay.Common.Scripts.Entities.Skills;
using GamePlay.Features.Battle.Scripts.Unit;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GamePlay.Features.Battle.Scripts.BattleAction
{
    /// <summary>
    /// - 셀 위에 커서가 올라가기만 하면, 셀 영역 안의 '유효 타겟' 캐릭터의 hoverSR(오버레이 SR)을 켜서 외곽선 표시
    /// - 포인터가 빠지면 외곽선 끔
    /// - 캐릭터에 Collider 없어도 Renderer.bounds로 폴백 탐색
    /// - hoverSR은 미리 붙어있다고 하자
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BattleActionIndicator : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerMoveHandler
    {
        #region Target Detecting
        [Header("Target Detection")]
        [Tooltip("셀 영역 탐색 크기(월드). autoProbeFromScale=true면 자동 계산")]
        [SerializeField] private Vector2 probeSize = new(0.98f, 0.98f);
        [SerializeField] private bool autoProbeFromScale = true;
        [Tooltip("콜라이더 기반 오버랩 먼저 시도, 실패시 렌더러 바운즈 폴백")]
        [SerializeField] private bool preferPhysicsOverlap = true;
        [Tooltip("캐릭터 탐색 레이어(비우면 전 레이어)")]
        [SerializeField] private LayerMask characterMask = 0;
        [Tooltip("막힌 셀이어도 '보여주기용'으로 외곽선 노출할지")]
        [SerializeField] private bool outlineEvenIfBlocked = true;
        
        #endregion
        
        #region Overlay
        [Header("Existing Overlay (hoverSR)")]
        [Tooltip("UnitRoot 하위에서 hoverSR을 찾을 때 우선 사용할 이름(없어도 됨)")]
        [SerializeField] private string hoverSrChildName = "hoverSR";
        [Tooltip("hoverSR의 머티리얼(아웃라인). null이면 현재 할당된 걸 그대로 사용")]
        [SerializeField] private Material outlineMaterialOverride;
        [Tooltip("Outline 셰이더 파라미터명")]
        [SerializeField] private string outlineColorProp = "_OutlineColor";
        [SerializeField] private string outlineSizeProp  = "_OutlineSize";
        [SerializeField] private Color  outlineColor     = new(1, 0, 1, 1);
        [SerializeField] private float  outlineSize      = 5f;
        [Tooltip("기본 SR보다 몇 단계 위/아래에 그릴지")]
        [SerializeField] private int outlineSortingOffset = +1;
        [Tooltip("애니메이션 스프라이트를 따라가도록 호버 중 매 프레임 동기화")]
        [SerializeField] private bool followBaseSpriteWhileHover = true;
        
        #endregion
        
        #region Tint - Hover
        [Header("Hover Tint (Indicator cell tint)")]
        [SerializeField] private float hoverBrightnessMul = 0.90f;
        [SerializeField] private float hoverAlphaAdd      = 0.05f;
        
        #endregion
        
        #region Runtime Field - Injected
        private CharBase     _caster;
        private SkillModel   _skill;
        private bool         _isBlocked;
        private SystemEnum.ePivot _pivotType = SystemEnum.ePivot.TARGET_ENEMY;
        private int          _pointerRange = 0; // 0=단일
        private Vector2Int _cell;
        private Action<CharBase, SkillModel, List<IDamageable>, Vector2Int> _confirmAction;
        private Action<Vector2Int> _onClickCell;
        
        #endregion
        
        #region Runtime Field - Indicator State
        private CharBase       _hoverTarget;
        private SpriteRenderer _baseSR;        // UnitRoot의 기본 SR
        private SpriteRenderer _hoverSR;       // UnitRoot의 자식 hoverSR(오버레이 SR)
        [SerializeField]private SpriteRenderer _cellMarkSR;        // 인디케이터 칸 SR
        private Color          _baseCellColor;
        private bool           _hovered;
        private Sprite         _lastBaseSprite; // 동기화용 캐시
        
        #endregion
        
        public SpriteRenderer CellMarkSR => _cellMarkSR;
        public Vector2 Cell => _cell;
        
        #region Initialization
        private bool _initialized = false;
        
        //TODO : pointerRange는 어떻게 처리되어야하는지 확인 필요함.
        //TODO : 굉장히 더러워서 정리한번 하면 좋을듯하다...
        public void Init(
            CharBase caster,
            SkillModel skill,
            bool isBlocked,
            Vector2Int cell,
            SystemEnum.ePivot pivotType = SystemEnum.ePivot.TARGET_ENEMY,
            int pointerRange = 0,
            Action<CharBase, SkillModel, List<IDamageable>, Vector2Int> confirmAction = null
        )
        {
            _caster = caster;
            _skill = skill;
            _isBlocked = isBlocked;
            _cell = cell;
            _pivotType = pivotType;
            _pointerRange = Mathf.Max(0, pointerRange);
            _confirmAction = confirmAction;
            
            //_cellSR ??= GetComponent<SpriteRenderer>();
            if(_cellMarkSR) 
            {
                _baseCellColor = _cellMarkSR.color;
                _hovered = false;
            }
            _initialized = true;
        }
        
        public void InitForSimpleCell(bool isBlocked, Vector2Int cell, Action<Vector2Int> onClickCell)
        {
            _isBlocked = isBlocked;
            _cell = cell;
            _onClickCell = onClickCell;
            
            _cellMarkSR ??= GetComponent<SpriteRenderer>();
            if(_cellMarkSR) 
            {
                _baseCellColor = _cellMarkSR.color;
                _hovered = false;
            }
            _initialized = true;
        }
        
        #endregion
        
        #region Unity Events
        private void Awake()
        {
            //_cellSR = GetComponent<SpriteRenderer>();
            if (_cellMarkSR != null) _baseCellColor = _cellMarkSR.color;

            if (autoProbeFromScale)
            {
                var s = transform.lossyScale;
                probeSize = new Vector2(Mathf.Abs(s.x), Mathf.Abs(s.y)) * 0.98f;
            }

            if (characterMask == 0)
            {
                int ch = LayerMask.NameToLayer("Character");
                if (ch >= 0) characterMask = 1 << ch; // 없으면 전 레이어 검색
            }
        }

        private void OnEnable()
        {
            if (!_initialized) return;
            if (IsPointerCurrentlyOverThis())
            {
                ApplyCellHoverTint(true);
                TryOutlineTargetInCell();
            }
        }

        private void Update()
        {
            // 호버 중에만 기본 SR의 스프라이트 변화(애니메이션)를 따라감
            if (followBaseSpriteWhileHover && _hoverSR != null && _baseSR != null && _hoverSR.enabled)
            {
                var cur = _baseSR.sprite;
                if (cur != _lastBaseSprite)
                {
                    _hoverSR.sprite = cur;
                    _hoverSR.flipX  = _baseSR.flipX;
                    _hoverSR.flipY  = _baseSR.flipY;
                    _lastBaseSprite = cur;
                }
            }
        }

        private void OnDisable() { ClearOutline(); ApplyCellHoverTint(false); }
        private void OnDestroy() { ClearOutline(); }
        
        #endregion
        
        #region Event System Implementation
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            ApplyCellHoverTint(true);
            if (_isBlocked && !outlineEvenIfBlocked) return;
            TryOutlineTargetInCell();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (_isBlocked && !outlineEvenIfBlocked) return;
            TryOutlineTargetInCell();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ClearOutline();
            ApplyCellHoverTint(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isBlocked) return;
            
            #region NON-SKILL
            if (_skill == null && _onClickCell != null)
            {
                _onClickCell(_cell);
                return;
            }
            #endregion
            
            #region SKILL
            
            if (!TryOutlineTargetInCell()) return;
            if (!IsValidTarget(_hoverTarget)) return;
            
            // 타겟 담기
            List<IDamageable> targets = new() { _hoverTarget };
            if (_pointerRange > 0)
            {
                targets.Clear();
                Vector2 center = (_pivotType == SystemEnum.ePivot.TARGET_SELF)
                    ? _caster.CharTransform.position
                    : transform.position;

                Vector2 box = new(_pointerRange, 1f);
                //TODO : BattleStageGrid를 참조하도록 할 수 있는지 확인할 것.
                
                
                Collider2D[] cols = Physics2D.OverlapBoxAll(center, box, 0f, characterMask);
                foreach (var c in cols)
                    if (c && c.TryGetComponent(out CharBase cb) && cb != null && IsValidTarget(cb))
                        targets.Add(cb);
            }
            
            if (targets.Count == 0 || !_caster) return; //타겟도 없고 심지어 캐스터 없으면 괘씸해서 return
            
            // controller callback
            _confirmAction?.Invoke(_caster, _skill, targets, _cell);
            
            #endregion
        }
        
        #endregion
        
        #region Core Implementation
        private bool TryOutlineTargetInCell()
        {
            ClearOutline();

            CharBase best = FindBestTargetInCell();
            if (best == null || !IsValidTarget(best)) return false;

            _hoverTarget = best;

            // UnitRoot의 기본 SR, 그리고 그 자식 hoverSR(오버레이) 찾기
            var unitRoot = _hoverTarget.CharUnitRoot ? _hoverTarget.CharUnitRoot : _hoverTarget.transform;
            _baseSR = unitRoot.GetComponent<SpriteRenderer>();
            if (_baseSR == null) return false;

            _hoverSR = FindHoverSR(unitRoot, _baseSR);
            if (_hoverSR == null) return false;

            // 머티리얼 오버라이드(선택)
            if (outlineMaterialOverride != null && _hoverSR.sharedMaterial != outlineMaterialOverride)
                _hoverSR.material = outlineMaterialOverride;

            // 셰이더 파라미터 적용(있을 때만)
            if (_hoverSR.material != null)
            {
                if (_hoverSR.material.HasProperty(outlineColorProp)) _hoverSR.material.SetColor(outlineColorProp, outlineColor);
                if (_hoverSR.material.HasProperty(outlineSizeProp))  _hoverSR.material.SetFloat(outlineSizeProp, outlineSize);
            }

            // 정렬을 기본 SR 기준으로 보정
            _hoverSR.sortingLayerID = _baseSR.sortingLayerID;
            _hoverSR.sortingOrder   = _baseSR.sortingOrder + outlineSortingOffset;

            // 스프라이트/플립 동기화 후 켜기
            _hoverSR.sprite = _baseSR.sprite;
            _hoverSR.flipX  = _baseSR.flipX;
            _hoverSR.flipY  = _baseSR.flipY;
            _hoverSR.enabled = true;
            _lastBaseSprite = _baseSR.sprite;

            return true;
        }

        private void ClearOutline()
        {
            if (_hoverSR != null)
            {
                _hoverSR.enabled = false;
                _hoverSR.sprite  = null;      // 깔끔히 끊기(선택)
            }
            _hoverTarget = null;
            _baseSR = null;
            _hoverSR = null;
            _lastBaseSprite = null;
        }

        private CharBase FindBestTargetInCell()
        {
            Bounds cell = new Bounds(transform.position, new Vector3(probeSize.x, probeSize.y, 10f));
            CharBase best = null; int bestOrder = int.MinValue;

            if (preferPhysicsOverlap)
            {
                int mask = (characterMask.value == 0) ? ~0 : characterMask.value;
                var cols = Physics2D.OverlapBoxAll(cell.center, new Vector2(cell.size.x, cell.size.y), 0f, mask);
                foreach (var c in cols)
                {
                    if (!c) continue;
                    var cb = c.GetComponentInParent<CharBase>();
                    if (cb == null) continue;
                    var sr = cb.GetComponentInChildren<SpriteRenderer>();
                    int so = sr ? sr.sortingOrder : 0;
                    if (best == null || so > bestOrder) { best = cb; bestOrder = so; }
                }
                if (best != null) return best;
            }

            // 폴백: 씬의 CharBase 전수 스캔 + Renderer.bounds 교차
            var all = FindObjectsOfType<CharBase>();
            foreach (var cb in all)
            {
                var sr = cb.GetComponentInChildren<SpriteRenderer>();
                if (sr == null) continue;
                if (!sr.bounds.Intersects(cell)) continue;
                int so = sr.sortingOrder;
                if (best == null || so > bestOrder) { best = cb; bestOrder = so; }
            }
            return best;
        }

        private bool IsValidTarget(CharBase target)
        {
            if (target == null) return false;
            switch (_pivotType)
            {
                case SystemEnum.ePivot.TARGET_SELF:   return target == _caster;
                case SystemEnum.ePivot.TARGET_ENEMY:  return target != _caster; // 필요시 팀판정으로 교체
                default:                              return true;
            }
        }

        private SpriteRenderer FindHoverSR(Transform unitRoot, SpriteRenderer baseSr)
        {
            // 1) 이름 우선
            if (!string.IsNullOrEmpty(hoverSrChildName))
            {
                var t = unitRoot.Find(hoverSrChildName);
                if (t && t.TryGetComponent(out SpriteRenderer srByName) && srByName != baseSr)
                    return srByName;
            }

            // 2) 머티리얼(Outline)로 추정
            var all = unitRoot.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in all)
            {
                if (sr == baseSr) continue;
                if (outlineMaterialOverride != null)
                {
                    if (sr.sharedMaterial == outlineMaterialOverride || sr.material == outlineMaterialOverride)
                        return sr;
                    if (sr.sharedMaterial != null && sr.sharedMaterial.shader == outlineMaterialOverride.shader)
                        return sr;
                }
                else
                {
                    // 이름이 hoverSR이거나, 스프라이트가 None이고 머티에 Outline 키워드가 있으면 후보
                    if (sr.name.ToLower().Contains("hover")) return sr;
                    if (sr.sprite == null && sr.sharedMaterial != null &&
                        sr.sharedMaterial.shader != null &&
                        sr.sharedMaterial.shader.name.ToLower().Contains("outline"))
                        return sr;
                }
            }

            return null; 
        }

        #endregion
        
        #region Util
        private void ApplyCellHoverTint(bool on)
        {
            if (_cellMarkSR == null) return;
            if (on)
            {
                if (!_hovered) { _baseCellColor = _cellMarkSR.color; _hovered = true; }
                var c = _baseCellColor;
                c.r = Mathf.Clamp01(c.r * hoverBrightnessMul);
                c.g = Mathf.Clamp01(c.g * hoverBrightnessMul);
                c.b = Mathf.Clamp01(c.b * hoverBrightnessMul);
                c.a = Mathf.Clamp01(c.a + hoverAlphaAdd);
                _cellMarkSR.color = c;
            }
            else
            {
                if (_hovered) { _cellMarkSR.color = _baseCellColor; _hovered = false; }
            }
        }

        private bool IsPointerCurrentlyOverThis()
        {
            if (EventSystem.current == null) return false;
            Vector2 pos = Mouse.current != null ? Mouse.current.position.ReadValue()
                                                : (Vector2)Input.mousePosition;
            var ed = new PointerEventData(EventSystem.current) { position = pos };
            var results = new List<RaycastResult>(8);
            EventSystem.current.RaycastAll(ed, results);
            foreach (var r in results) if (r.gameObject == gameObject) return true;
            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(probeSize.x, probeSize.y, 0.1f));
        }
#endif
        #endregion
    }
}
