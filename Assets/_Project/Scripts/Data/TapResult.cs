using UnityEngine;

namespace MagicTiles.Data
{
    public enum HitRank { None, Cool, Great, Perfect }

    public readonly struct HitResult
    {
        public readonly HitRank Rank;
        public readonly float Delta; // |songTime - beatTime| in seconds

        /// <summary>
        /// True when this result represents the bonus awarded at hold-tail completion.
        /// IsHit is also true; combo must NOT be incremented for this result.
        /// </summary>

        public HitResult(HitRank rank, float delta)
        {
            Rank = rank;
            Delta = delta;
        }

        public static HitResult Hit(HitRank rank, float delta) => new(rank, delta);

        public override string ToString() => $"{Rank} (delta={Delta:F3}s)";
    }
}