using System;

namespace ThreeDoorsOfFate.Game.V140
{
    [Serializable]
    public sealed class RunRandomSnapshot
    {
        public int Seed;
        public uint State;
        public int Cursor;
    }

    public sealed class SeededRunRandom
    {
        private const uint NonZeroFallbackState = 0x6D2B79F5u;
        private const float UnitFloatScale = 1f / 16777216f;

        private readonly int seed;
        private uint state;
        private int cursor;

        public SeededRunRandom(int seed)
        {
            this.seed = seed;
            state = unchecked((uint)seed);
            if (state == 0u)
            {
                state = NonZeroFallbackState;
            }
        }

        public SeededRunRandom(RunRandomSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (snapshot.State == 0u)
            {
                throw new ArgumentException("Random state cannot be zero.", nameof(snapshot));
            }

            if (snapshot.Cursor < 0)
            {
                throw new ArgumentException("Random cursor cannot be negative.", nameof(snapshot));
            }

            seed = snapshot.Seed;
            state = snapshot.State;
            cursor = snapshot.Cursor;
        }

        public uint NextUInt()
        {
            uint value = state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            state = value == 0u ? NonZeroFallbackState : value;
            cursor += 1;
            return state;
        }

        public int Range(int minimumInclusive, int maximumExclusive)
        {
            if (maximumExclusive <= minimumInclusive)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumExclusive),
                    "Maximum must be greater than minimum.");
            }

            uint span = unchecked((uint)((long)maximumExclusive - minimumInclusive));
            uint threshold = unchecked((uint)(0u - span)) % span;
            uint sample;
            do
            {
                sample = NextUInt();
            }
            while (sample < threshold);

            long offset = sample % span;
            return unchecked((int)(minimumInclusive + offset));
        }

        public float Value()
        {
            return (NextUInt() >> 8) * UnitFloatScale;
        }

        public RunRandomSnapshot Capture()
        {
            return new RunRandomSnapshot
            {
                Seed = seed,
                State = state,
                Cursor = cursor
            };
        }
    }
}
