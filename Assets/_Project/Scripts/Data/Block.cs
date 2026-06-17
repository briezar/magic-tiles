using System;
using MagicTiles.Data;

#if UNITY_EDITOR
using GameDevKit.Editor;
using UnityEditor;
#endif

namespace MagicTiles.Data
{
    [Serializable]
    public class Block
    {
        /// <summary>
        /// Subdivision row count for this measure (4 / 8 / 16 …).
        /// Determines beat resolution: beatStep = 4f / RowCount.
        /// </summary>
        public int RowCount;

        /// <summary>Sparse — only non-Empty notes are stored.</summary>
        public Note[] Notes = Array.Empty<Note>();
    }
}

#if UNITY_EDITOR
namespace MagicTiles.Editor
{
    [CustomPropertyDrawer(typeof(Block))]
    public class BlockDrawer : InvisibleLabelDrawer
    {
        protected override string GetLabelText(SerializedProperty property)
        {
            var block = (Block)property.boxedValue;
            return $"{block.RowCount} ({block.Notes.JoinToString(n => $"{n.BeatPosition:#.#}")})";
        }
    }
}
#endif
