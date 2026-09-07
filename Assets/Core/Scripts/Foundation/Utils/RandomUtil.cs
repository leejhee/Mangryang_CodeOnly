namespace Core.Scripts.Foundation.Utils
{
    public static class RandomUtil
    {
        public static ulong SplitMix64(ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            return x ^ (x >> 31);
        }

        public static ulong StringHash64(string s)
        {
            // FNV-1a 64 (GameRandom에도 FNV 기반 유사 구현 존재)
            unchecked
            {
                ulong h = 14695981039346656037UL;
                foreach (var ch in s) { h ^= ch; h *= 1099511628211UL; }
                return h;
            }
        }

        public static ulong Mix3(ulong a, ulong b, ulong c)
        {
            a ^= b; a = SplitMix64(a);
            a ^= c; a = SplitMix64(a);
            return a;
        }

        public static ulong DeriveSlotSeed(ulong masterSeed, string slotName, ulong generation)
        {
            if (string.IsNullOrWhiteSpace(slotName))
                throw new System.ArgumentException("Slot name must not be empty.", nameof(slotName));
            if (generation == 0)
                throw new System.ArgumentOutOfRangeException(nameof(generation), "Slot generation must start at 1.");

            ulong seed = Mix3(masterSeed, StringHash64(slotName), generation);
            if (seed != 0) return seed;

            seed = SplitMix64(masterSeed ^ generation);
            return seed != 0 ? seed : 1UL;
        }
    }
}
