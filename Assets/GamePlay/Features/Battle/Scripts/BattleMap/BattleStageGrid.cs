using GamePlay.Features.Battle.Scripts.Unit;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GamePlay.Features.Battle.Scripts.BattleMap
{
    [RequireComponent(typeof(StageField))]
    public class BattleStageGrid : MonoBehaviour
    {
        private StageField _stage;

        private Vector2Int _gridSize;
        private Vector2 _cellSize;
        
        private Dictionary<Vector2Int, CharBase> _characters;
        private Dictionary<CharBase, Vector2Int> _unitToCell;
        
        private Dictionary<Vector2Int, FieldCover> _cellToCover;
        private Dictionary<FieldCover, Vector2Int> _coverToCell;

        private Dictionary<Vector2Int, FieldObstacle> _cellToObstacle;
        private Dictionary<FieldObstacle, Vector2Int> _obstacleToCell;
        
        private HashSet<Vector2Int> _walkable;  //플랫폼이 있는 자리
        private HashSet<Vector2Int> _obstacles; //장애물이 있는 자리
        private HashSet<Vector2Int> _coverages; //엄폐물이 있는 자리
        
        #region Public Properties
        
        public Vector2Int GridSize => _gridSize;
        public Vector2 CellSize => _cellSize;
        
        /// <summary> 여기서 필요한 cell position 가져다 쓸 것 </summary>
        public IReadOnlyDictionary<Vector2Int, CharBase> CharacterPositions => _characters;
        public IReadOnlyCollection<Vector2Int> WalkablePositions => _walkable;
        public IReadOnlyCollection<Vector2Int> ObstaclePositions => _obstacles;
        public IReadOnlyCollection<Vector2Int> CoveragePositions => _coverages;
        #endregion
        
        #region Initialization
        public void InitGrid(StageField staticField)
        {
            _stage = staticField;

            _gridSize = staticField.GridSize;
            _cellSize = staticField.Grid.cellSize;
                
            _walkable = staticField.PlatformGridCells.ToHashSet();
            _obstacles = new HashSet<Vector2Int>();
            _coverages = new HashSet<Vector2Int>();
            
            _characters = new Dictionary<Vector2Int, CharBase>();
            _unitToCell = new Dictionary<CharBase, Vector2Int>();
            
            _cellToCover = new Dictionary<Vector2Int, FieldCover>();
            _coverToCell = new Dictionary<FieldCover, Vector2Int>();
            
            _cellToObstacle = new Dictionary<Vector2Int, FieldObstacle>();
            _obstacleToCell = new Dictionary<FieldObstacle, Vector2Int>();

            foreach (ObstacleEntry oe in staticField.ObstacleGridCells)
                RegisterObstacle(oe.obstacle, oe.cell);
            foreach (CoverEntry ce in staticField.CoverageGridCells)
                RegisterCover(ce.cover, ce.cell);
        }
        
        /// <summary>
        /// 캐릭터 초기화 시 사용. BattleCharManager에 모든 유닛이 등록된 후에 사용
        /// </summary>
        public void RebuildCharacterPositions()
        {
            _characters.Clear();
            _unitToCell.Clear();
            foreach (CharBase unit in BattleCharManager.Instance.GetBattleMembers())
            {
                Debug.Log(unit.CharInfo.Name);
                Debug.Log(unit.CharTransform);
                Vector2Int cell = _stage.WorldToCell(unit.CharTransform.position);
                _characters[cell] = unit;
                _unitToCell[unit] = cell;
            }
            
            
        }
        #endregion
        
        #region Cell Point Validation
        public bool IsInBounds(Vector2Int cell) => _stage.InBounds(cell);
        public bool IsOccupied(Vector2Int cell) => _characters.ContainsKey(cell);

        public bool IsWalkable(Vector2Int cell) =>
            _walkable.Contains(cell) && !_obstacles.Contains(cell) && !IsOccupied(cell);
        public bool IsPlatform(Vector2Int cell) => _walkable.Contains(cell);
        public bool IsObstacle(Vector2Int cell) => _obstacles.Contains(cell) && !IsOccupied(cell);
        public bool IsCoverage(Vector2Int cell) => _coverages.Contains(cell);
        public bool IsMaskable(Vector2Int cell) => !IsInBounds(cell) || !IsPlatform(cell);
        
        #endregion
        
        #region StageField API - Cell Point
        
        public Vector2Int WorldToCell(Vector2 w) => _stage.WorldToCell(w);
        public Vector2 CellToWorldCenter(Vector2Int c) => _stage.CellToWorldCenter(c);
        
        #endregion
        
        #region Grid Helper
        
        public bool TryFindLandingFloor(Vector2Int from, out Vector2Int landingCell, out int fallFloors)
        {
            landingCell = default; fallFloors = 0;
            if (!IsInBounds(from)) return false;
            int y = from.y - 1; // from가 공중이라면 아래부터 탐색
            while (y >= 0){
                var c = new Vector2Int(from.x, y);
                if (IsPlatform(c)){ landingCell = c; fallFloors = from.y - y; return true; }
                y--;
            }
            return false; 
        }
        
        public CharBase GetUnitAt(Vector2Int cell) => _characters.GetValueOrDefault(cell);
        
        /// <summary>
        /// 등록자가 Cell을 점유하게 함
        /// </summary>
        /// <param name="cell">좌표 포인트</param>
        /// <param name="registrant">등록자</param>
        public bool OccupyCell(Vector2Int cell, CharBase registrant)
        {
            if (!IsInBounds(cell) || !IsWalkable(cell)) return false;
            if(_unitToCell.TryGetValue(registrant, out Vector2Int c)) _characters.Remove(c);
            _characters[cell] = registrant; _unitToCell[registrant] = cell;

            return true;
        }
        
        /// <summary>
        /// Cell의 점유 및 등록 해제
        /// </summary>
        /// <param name="unit">해제하는 유닛</param>
        public bool RemoveUnit(CharBase unit){
            if (!_unitToCell.Remove(unit, out Vector2Int c)) return false;
            _characters.Remove(c);
            return true;
        }
        
        /// <summary>
        /// 유닛의 위치 이동 관리. 오로지 위치 정보만 이동(구체 이동 관리 X)
        /// </summary>
        /// <param name="unit">이동할 유닛</param>
        /// <param name="to">목적지</param>
        /// <returns>결과</returns>
        public bool MoveUnit(CharBase unit, Vector2Int to)
        {
            if(!_unitToCell.TryGetValue(unit, out Vector2Int c)) return OccupyCell(to, unit); // 자기자리 찾기
            if (to != c && !IsWalkable(to)) return false; // 자기자리가 아닌 다른 곳으로 가는데, 갈 수 있는 곳이 아니면 false.
            _characters.Remove(c);
            _characters[to] = unit; 
            _unitToCell[unit] = to;
            return true;
        }

        #endregion
        
        public bool RegisterObstacle(FieldObstacle obs, Vector2Int cell)
        {
            if (!obs) return false;

            _cellToObstacle[cell] = obs;
            _obstacleToCell[obs] = cell;
            _obstacles.Add(cell);
            
            //SnapToCenter(obs.transform, cell);
            obs.BindGrid(this, cell);
            obs.Broken -= OnObstacleBroken;
            obs.Broken += OnObstacleBroken;
            return true;
        }

        public bool RegisterCover(FieldCover cover, Vector2Int cell)
        {
            if (!cover) return false;
            _cellToCover[cell] = cover;
            _coverToCell[cover] = cell;
            _coverages.Add(cell);
            
            cover.BindGrid(this, cell);
            cover.Broken -= OnCoverBroken;
            cover.Broken += OnCoverBroken;
            return true;
        }

        public bool UnregisterObstacle(FieldObstacle obs)
        {
            if (!obs) return false;
            if (_obstacleToCell.Remove(obs, out Vector2Int cell))
            {
                _cellToObstacle.Remove(cell);
                _obstacles.Remove(cell);
                obs.Broken -= OnObstacleBroken;
                return true;
            }
            return false;
        }

        public bool UnregisterCover(FieldCover cover)
        {
            if (!cover) return false;
            if (_coverToCell.Remove(cover, out Vector2Int cell))
            {
                _cellToCover.Remove(cell);
                _coverages.Remove(cell);
                cover.Broken -= OnCoverBroken;
                return true;
            }
            return false;
        }

        public FieldObstacle GetObstacleAt(Vector2Int cell) => _cellToObstacle.GetValueOrDefault(cell);
        public FieldCover    GetCoverAt(Vector2Int cell)    => _cellToCover.GetValueOrDefault(cell);

        // 외부에서 셀 기준 제거하고 싶을 때(예: 스킬 효과)
        public bool TryRemoveObstacleAt(Vector2Int cell)
        {
            FieldObstacle obs = GetObstacleAt(cell);
            if (!obs) return false;
            // 파괴 → OnDestroy/이벤트 통해 정리
            Object.Destroy(obs.gameObject);
            return true;
        }

        public bool TryRemoveCoverAt(Vector2Int cell)
        {
            FieldCover cov = GetCoverAt(cell);
            if (!cov) return false;
            Object.Destroy(cov.gameObject);
            return true;
        }

        private void OnObstacleBroken(FieldObstacle obs) => UnregisterObstacle(obs);
        private void OnCoverBroken(FieldCover cov)       => UnregisterCover(cov);

        private void SnapToCenter(Transform t, Vector2Int cell)
        {
            Vector2 want = _stage.CellToWorldCenter(cell);
            if ((Vector2)t.position != want) t.position = want;
        }

        public Vector2 CalibratedPivot(Vector2Int cell, CharBase unit)
        {
            Vector2 toWorld = _stage.CellToWorldCenter(cell);
            float yCalibration = _stage.Grid.cellSize.y / 2;
            float yColliderHalf = unit.BattleCollider.size.y / 2;
            Vector2 finalPos = new(toWorld.x, toWorld.y - (yCalibration - yColliderHalf));
            return finalPos;
        }
    }
}