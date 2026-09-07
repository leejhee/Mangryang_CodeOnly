using System.Collections.Generic;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.BattleMap
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class BattleGridProvider : MonoBehaviour
    {
        // ===== 셰이더 Reference와 동일해야 함 =====
        private static readonly int CellsID     = Shader.PropertyToID("_Cells");
        private static readonly int CellSizeID  = Shader.PropertyToID("_CellSize");
        private static readonly int OriginID    = Shader.PropertyToID("_Origin");
        private static readonly int CellMaskID  = Shader.PropertyToID("_CellMask");
        private static readonly int HoverCellID = Shader.PropertyToID("_HoverCell");
        private static readonly int ThicknessID = Shader.PropertyToID("_Thickness");

        const byte S_NONE     = 0;
        const byte S_POSSIBLE = 85;   
        const byte S_BLOCKED  = 170;  
        const byte S_SELECTED = 255;
        
        [Header("Grid Appearance")]
        [SerializeField, Range(0.005f, 0.02f)]
        private float thickness = 0.005f;   // 셀 크기 대비 선 두께 비율
        
        SpriteRenderer _sr;
        MaterialPropertyBlock _mpb;

        Texture2D _mask;      // R8
        byte[] _buf;          // W*H
        Vector2Int _cells;    // (W,H)

        void Awake()
        {
            _sr  = GetComponent<SpriteRenderer>();
            _mpb = new MaterialPropertyBlock();
        }

        /// <summary>스프라이트 크기/위치와 셰이더 기본 파라미터 세팅</summary>
        public void ApplySpec(Vector2Int cells, Vector2 cellSizeWorld, Vector2 originWorld)
        {
            EnsureComponents();
            _cells = cells;

            // 이전 프리팹의 수동 Scale override를 제거하고 SpriteRenderer 크기를 단일 기준으로 사용한다.
            var worldSize = new Vector2(cells.x * cellSizeWorld.x, cells.y * cellSizeWorld.y);
            Vector3 localScale = transform.localScale;
            localScale.x = 1f;
            localScale.y = 1f;
            transform.localScale = localScale;

            Vector3 parentScale = transform.parent ? transform.parent.lossyScale : Vector3.one;
            Vector2 localSize = new(
                worldSize.x / Mathf.Max(Mathf.Abs(parentScale.x), Mathf.Epsilon),
                worldSize.y / Mathf.Max(Mathf.Abs(parentScale.y), Mathf.Epsilon));
            _sr.drawMode = SpriteDrawMode.Tiled;
            _sr.size     = localSize;
            var center   = originWorld + worldSize * 0.5f;
            transform.position = new Vector3(center.x, center.y, transform.position.z);

            _sr.GetPropertyBlock(_mpb);
            _mpb.SetVector(CellsID,    new Vector4(cells.x, cells.y, 0, 0));
            _mpb.SetVector(CellSizeID, new Vector4(cellSizeWorld.x, cellSizeWorld.y, 0, 0));
            _mpb.SetVector(OriginID,   new Vector4(originWorld.x, originWorld.y, 0, 0));
            if (_sr.sharedMaterial && _sr.sharedMaterial.HasFloat(ThicknessID))
            {
                float clamped = Mathf.Clamp(thickness, 0.001f, 1.0f);
                _mpb.SetFloat(ThicknessID, clamped);
            }
                
            _mpb.SetVector(HoverCellID, new Vector4(-999, -999, 0, 0)); // 초기 호버 off
            _sr.SetPropertyBlock(_mpb);
        }

        private void EnsureComponents()
        {
            if (!_sr)
                _sr = GetComponent<SpriteRenderer>();
            _mpb ??= new MaterialPropertyBlock();
        }

        /// <summary>하이라이트 마스크 텍스처 준비(R8, Point/Clamp)</summary>
        public void InitMask()
        {
            ReleaseMask();

            _buf  = new byte[_cells.x * _cells.y];
            _mask = new Texture2D(_cells.x, _cells.y, TextureFormat.R8, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode   = TextureWrapMode.Clamp
            };
            _mask.SetPixelData(_buf, 0);
            _mask.Apply(false, false);

            _sr.GetPropertyBlock(_mpb);
            _mpb.SetTexture(CellMaskID, _mask);
            _sr.SetPropertyBlock(_mpb);
        }

        public void SetHighlights(IEnumerable<Vector2Int> possible, IEnumerable<Vector2Int> blocked, Vector2Int? selected = null)
        {
            if (_mask == null || _buf == null) return;

            System.Array.Clear(_buf, 0, _buf.Length);

            if (possible != null) foreach (var c in possible) Paint(c, S_POSSIBLE);
            if (blocked  != null) foreach (var c in blocked)  Paint(c, S_BLOCKED);
            if (selected.HasValue) Paint(selected.Value, S_SELECTED);

            _mask.SetPixelData(_buf, 0);
            _mask.Apply(false, false);
        }

        public void SetHoverCell(Vector2Int cell)
        {
            _sr.GetPropertyBlock(_mpb);
            _mpb.SetVector(HoverCellID, new Vector4(cell.x, cell.y, 0, 0));
            _sr.SetPropertyBlock(_mpb);
        }

        public void ClearHover()
        {
            _sr.GetPropertyBlock(_mpb);
            _mpb.SetVector(HoverCellID, new Vector4(-999, -999, 0, 0));
            _sr.SetPropertyBlock(_mpb);
        }

        public void ClearHighlights()
        {
            if (_mask == null || _buf == null) return;
            System.Array.Clear(_buf, 0, _buf.Length);
            _mask.SetPixelData(_buf, 0);
            _mask.Apply(false, false);
        }

        public void Show(bool on) => gameObject.SetActive(on);

        void Paint(Vector2Int c, byte v)
        {
            if ((uint)c.x >= (uint)_cells.x || (uint)c.y >= (uint)_cells.y) return;
            _buf[c.y * _cells.x + c.x] = v;
        }

        void OnDestroy() => ReleaseMask();

        void ReleaseMask()
        {
            if (_mask != null)
            {
#if UNITY_EDITOR
                Object.DestroyImmediate(_mask);
#else
            Object.Destroy(_mask);
#endif
                _mask = null;
            }
            _buf = null;
        }
    }
}
