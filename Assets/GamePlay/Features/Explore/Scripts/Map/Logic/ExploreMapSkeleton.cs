using Core.Scripts.Foundation.Define;
using System;
using System.Collections.Generic;

namespace GamePlay.Features.Explore.Scripts.Map.Logic
{
    /// <summary>
    /// 맵의 기본 구조를 나타내는 클래스
    /// </summary>
   [Serializable]
    public class ExploreMapSkeleton
    {
        public string DungeonName { get; }
        public int Floor { get; }
        public int Width { get; }
        public int Height { get; }
        public ulong Seed { get; }
        public int GenerationVersion { get; }
        public ulong ConfigSignature { get; }
        
        private readonly SystemEnum.MapCellType[] _cells;
        private readonly List<SkeletonSymbol> _symbols = new();
        private int _nextSymbolId = 1;
        
        private readonly Dictionary<int, List<int>> _indexToSymbolIds = new();
        private readonly Dictionary<SystemEnum.MapSymbolType, List<int>> _typeToSymbolIds = new();
        
        public ExploreMapSkeleton(
            string dungeonName,
            int floor,
            int width,
            int height,
            ulong seed,
            int generationVersion = 1,
            ulong configSignature = 0UL)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException("width/height must be positive");

            DungeonName = dungeonName ?? "Unknown";
            Floor = floor;
            Width = width;
            Height = height;
            Seed = seed;
            GenerationVersion = generationVersion;
            ConfigSignature = configSignature;

            _cells = new SystemEnum.MapCellType[width * height];
        }
        
        public bool InBounds(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;
        
        /// <summary>
        /// Flatten해줌
        /// </summary>
        public int ToIndex(int x, int y)
        {
            if (!InBounds(x, y)) throw new ArgumentOutOfRangeException($"Out of bounds: ({x},{y})");
            return y * Width + x;
        }
        
        public IEnumerable<(int x, int y)> GetNeighbors4(int x, int y)
        {
            if (InBounds(x - 1, y)) yield return (x - 1, y);
            if (InBounds(x + 1, y)) yield return (x + 1, y);
            if (InBounds(x, y - 1)) yield return (x, y - 1);
            if (InBounds(x, y + 1)) yield return (x, y + 1);
        }
        
        public SystemEnum.MapCellType GetCellType(int x, int y) => _cells[ToIndex(x, y)];

        /// <summary>
        /// 역할: 셀 타입 설정
        /// 전제: Skeleton 단계에서만 호출(최종 데이터는 불변)
        /// </summary>
        public void SetCellType(int x, int y, SystemEnum.MapCellType type)
        {
            _cells[ToIndex(x, y)] = type;
        }
        
        public bool IsFloor(int x, int y) => GetCellType(x, y) == SystemEnum.MapCellType.Floor;
        public bool IsWall(int x, int y) => GetCellType(x, y) == SystemEnum.MapCellType.Wall;
        
        public IEnumerable<SkeletonSymbol> GetSymbolsAt(int x, int y)
        {
            int idx = ToIndex(x, y);
            if (_indexToSymbolIds.TryGetValue(idx, out var list))
            {
                foreach (int id in list) yield return _symbols[id - 1];
            }
        }
        
        public IEnumerable<SkeletonSymbol> GetSymbolsOfType(SystemEnum.MapSymbolType type)
        {
            if (_typeToSymbolIds.TryGetValue(type, out var list))
            {
                foreach (int id in list) yield return _symbols[id - 1];
            }
        }

        /// <summary>
        /// 칸당 1 심볼인지 체크
        /// </summary>
        public bool HasAnySymbolAt(int x, int y)
        {
            int idx = ToIndex(x, y);
            return _indexToSymbolIds.TryGetValue(idx, out var list) && list.Count > 0;
        }
        
        private bool CanPlaceSymbol(int x, int y, out string error)
        {
            if (!InBounds(x, y)) { error = "Out of bounds"; return false; }
            if (!IsFloor(x, y))  { error = "Symbol must be on Floor cell"; return false; }
            if (HasAnySymbolAt(x, y)) { error = "Symbol overlap not allowed"; return false; }
            error = null;
            return true;
        }
        
        public bool TryAddEventSymbol(int x, int y, SystemEnum.CellEventType eventType, out SkeletonSymbol symbol, out string error)
        {
            symbol = default;
            if (!CanPlaceSymbol(x, y, out error)) return false;

            // 타입별 필수 페이로드 강제: Event는 반드시 EventType 보유
            symbol = SkeletonSymbol.CreateEvent(_nextSymbolId++, x, y, eventType);
            AddSymbolInternal(symbol);
            return true;
        }
        
        public bool TryAddItemSymbol(int x, int y, long itemIndex, out SkeletonSymbol symbol, out string error)
        {
            symbol = default;
            if (!CanPlaceSymbol(x, y, out error)) return false;

            // 타입별 필수 페이로드 강제: Item은 반드시 ItemIndex 보유
            symbol = SkeletonSymbol.CreateItem(_nextSymbolId++, x, y, itemIndex);
            AddSymbolInternal(symbol);
            return true;
        }
        
        public bool TryAddSimpleSymbol(int x, int y, SystemEnum.MapSymbolType type, out SkeletonSymbol symbol, out string error)
        {
            symbol = default;
            if (type is SystemEnum.MapSymbolType.Item or SystemEnum.MapSymbolType.Event)
            {
                error = "Use TryAddItemSymbol or TryAddEventSymbol for typed payload";
                return false;
            }
            if (!CanPlaceSymbol(x, y, out error)) return false;

            symbol = SkeletonSymbol.CreateSimple(_nextSymbolId++, x, y, type);
            AddSymbolInternal(symbol);
            return true;
        }
        
        public bool TryRemoveAnySymbolAt(int x, int y)
        {
            int idx = ToIndex(x, y);
            if (!_indexToSymbolIds.TryGetValue(idx, out var list) || list.Count == 0) return false;

            int id = list[0];
            RemoveSymbolInternal(id, idx);
            return true;
        }
        
        private void AddSymbolInternal(SkeletonSymbol s)
        {
            _symbols.Add(s);

            int idx = ToIndex(s.X, s.Y);
            if (!_indexToSymbolIds.TryGetValue(idx, out var list))
            {
                list = new List<int>(1);
                _indexToSymbolIds[idx] = list;
            }
            list.Add(s.Id);

            if (!_typeToSymbolIds.TryGetValue(s.Type, out var tlist))
            {
                tlist = new List<int>(4);
                _typeToSymbolIds[s.Type] = tlist;
            }
            tlist.Add(s.Id);
        }
        
        private void RemoveSymbolInternal(int id, int idx)
        {
            // 리스트에선 id-1 위치에 저장된다고 보장(증분 ID)
            var s = _symbols[id - 1];

            // 자리 빈 칸으로 두지 않고 " tombstone " 없이 유지(간단화). 
            // 실제로는 제거 마크를 두고 압축하는 전략도 가능.
            _symbols[id - 1] = SkeletonSymbol.Deleted(id);

            // 좌표 인덱스에서 제거
            if (_indexToSymbolIds.TryGetValue(idx, out var list))
            {
                list.Remove(id);
                if (list.Count == 0) _indexToSymbolIds.Remove(idx);
            }
            // 타입 인덱스에서 제거
            if (_typeToSymbolIds.TryGetValue(s.Type, out var tlist))
            {
                tlist.Remove(id);
                if (tlist.Count == 0) _typeToSymbolIds.Remove(s.Type);
            }
        }
        
        public SystemEnum.MapCellType[] CloneCells() => (SystemEnum.MapCellType[])_cells.Clone();

        public List<SkeletonSymbol> CollectSymbols()
        {
            var result = new List<SkeletonSymbol>(_symbols.Count);
            foreach (var s in _symbols) if (!s.IsDeleted) result.Add(s);
            return result;
        }
        
        public void SetFloor(int x, int y) => SetCellType(x, y, SystemEnum.MapCellType.Floor);
        public void SetWall (int x, int y) => SetCellType(x, y, SystemEnum.MapCellType.Wall);
    }
}
