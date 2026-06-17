using System;
using MagicTiles.Gameplay;

namespace MagicTiles.Data
{
    public enum NoteType { Tap, Hold }

    [Serializable]
    public class Note
    {
        public NoteType Type;

        /// <summary>Row index within its measure (0-based).</summary>
        public int Row;

        /// <summary>Lane index, 0–3 left-to-right.</summary>
        public int Lane;

        /// <summary>
        /// Absolute beat position, pre-computed at parse time.
        /// e.g. measure 2, row 2 of 8 → beat = 2*4 + (2/8)*4 = 9.0
        /// </summary>
        public float BeatPosition;

        /// <summary>
        /// Duration in beats. 0 for Tap. Populated on `HoldHead` only after its matching tail is found.
        /// </summary>
        public float HoldBeats;
    }
}