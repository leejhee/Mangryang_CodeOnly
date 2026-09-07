using Core.Scripts.Foundation.Define;

namespace GamePlay.Features.Explore.Scripts.Map.Data
{
    [System.Serializable]
    public struct ExploreMapKey : System.IEquatable<ExploreMapKey>
    {
        public SystemEnum.Dungeon dungeon;
        public int floor;

        public ExploreMapKey(SystemEnum.Dungeon dungeon, int floor)
        {
            this.dungeon = dungeon;
            this.floor = floor;
        }

        public bool Equals(ExploreMapKey other) => dungeon == other.dungeon && floor == other.floor;
        public override bool Equals(object obj) => obj is ExploreMapKey other && Equals(other);
        public override int GetHashCode() => (int)dungeon * 100 + floor; //이건 그냥 무지성으로 함.
        public override string ToString() => $"{dungeon} - Floor {floor}";
    }
}