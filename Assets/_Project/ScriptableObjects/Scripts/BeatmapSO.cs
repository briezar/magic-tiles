using System;
using System.Collections.Generic;
using UnityEngine;
using GameDevKit;
using System.Linq;
using EditorAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicTiles.Data
{
    [CreateAssetMenu(menuName = "MagicTiles/Beatmap")]
    public class BeatmapSO : ScriptableObject
    {
        [Header("Song")]
        public SongInfoSO SongInfo;

        [Header("Timing")]
        public float Bpm = 120f;

        /// <summary>
        /// Seconds before beat 0 the audio starts.
        /// Derived from #OFFSET in the .sm file with sign flipped (stored positive).
        /// e.g. #OFFSET:-0.319070 → OffsetSeconds = 0.319070
        /// </summary>
        public float OffsetSeconds;

        [Header("Playback Range")]
        /// <summary>
        /// Normalized [0..1] fraction of clip length.
        /// Music fades in at min and fades out at max.
        /// Use AutoSetPlayRange to populate from note data, then adjust manually.
        /// </summary>
        public FloatRange PlayRange = new(0f, 1f);

        [Min(0f)] public float FadeInDuration = 0.5f;
        [Min(0f)] public float FadeOutDuration = 0.5f;

        public Block[] Blocks = Array.Empty<Block>();

        public float ClipLength => SongInfo?.MusicClip != null ? SongInfo.MusicClip.length : 0f;

        /// <summary>Absolute seconds into the clip where fade-in begins.</summary>
        public float StartTime => ClipLength * PlayRange.min;

        /// <summary>Absolute seconds into the clip where fade-out begins.</summary>
        public float EndTime => ClipLength * PlayRange.max;

        /// <summary>Duration of a single beat in seconds, derived from BPM.</summary>
        public float SecondsPerBeat => 60f / Bpm;

        public float BeatsPerSecond => Bpm / 60f;

        [NonSerialized] private Note[] _flatNotes;

        /// <summary>
        /// All non-Empty notes across all blocks, sorted by BeatTime.
        /// Lazily built and cached; call InvalidateFlatNotes() if Blocks change at runtime.
        /// </summary>
        public Note[] FlatNotes
        {
            get
            {
                if (_flatNotes == null)
                {
                    var list = new List<Note>();
                    foreach (var block in Blocks)
                    {
                        if (block?.Notes == null) { continue; }
                        foreach (var note in block.Notes)
                        {
                            if (note != null) { list.Add(note); }
                        }
                    }
                    list.Sort((a, b) => a.BeatPosition.CompareTo(b.BeatPosition));
                    _flatNotes = list.ToArray();
                }
                return _flatNotes;
            }
        }

        public void InvalidateFlatNotes() => _flatNotes = null;

        /// <summary>
        /// Returns the tile speed where one tile height equals the gap between two consecutive notes
        /// at the densest subdivision in the beatmap (e.g. 8 rows per measure = eighth notes = 0.5 beat apart).
        /// </summary>
        public float GetTileSpeed(float tileHeight)
        {
            const int RowsPerBeat = 4;
            // var maxRows = Blocks.Max(block => block.RowCount);
            var maxRows = 0;
            for (int i = 0; i < Blocks.Length; i++)
            {
                var block = Blocks[i];
                if (block.RowCount > maxRows) { maxRows = block.RowCount; }
            }
            var subdivision = (float)maxRows / RowsPerBeat;
            return tileHeight * subdivision * BeatsPerSecond;
        }

        /// <summary> Convert an absolute beat to seconds, accounting for offset. </summary>
        public float BeatToSeconds(float beat) => beat * SecondsPerBeat + OffsetSeconds;

        public float GetNormalizedTime(float time) => Mathf.InverseLerp(StartTime, EndTime, time);


#if UNITY_EDITOR
        [Button]
        private void PrintInfo()
        {
            InvalidateFlatNotes();

            var tileHeight = 1.96f;
            var tileSpeed = GetTileSpeed(tileHeight);

            const int RowsPerBeat = 4;
            var maxRows = Blocks.Max(block => block.RowCount);
            var subdivision = (float)maxRows / RowsPerBeat;

            Debug.Log($"Beatmap Info:\n" +
                      $"- Song: {SongInfo?.SongTitle ?? "None"}\n" +
                      $"- BPM: {Bpm}\n" +
                      $"- Offset: {OffsetSeconds:F3}s\n" +
                      $"- Play Range: {PlayRange.min} to {PlayRange.max}\n" +
                      $"- Clip Length: {ClipLength:F2}s\n" +
                      $"- Start Time: {StartTime:F2}s\n" +
                      $"- End Time: {EndTime:F2}s\n" +
                      $"- Beat Duration: {SecondsPerBeat:F3}s\n" +
                      $"- Beat Per Second: {BeatsPerSecond:F3}s\n" +
                      $"- Tile Speed (for tile height {tileHeight}): {tileSpeed:F3} units/s\n" +
                      $"- Max Rows: {maxRows} - Subdivision: {subdivision}\n" +
                      $"- Total Notes: {FlatNotes.Length}", this);
        }

        [Button]
        private void AutoSetPlayRange()
        {
            InvalidateFlatNotes();

            var clip = SongInfo?.MusicClip;
            if (clip == null)
            {
                Debug.LogWarning("AutoSetPlayRange: SongInfo.MusicClip is not assigned.", this);
                return;
            }

            var flat = FlatNotes;
            if (flat == null || flat.Length == 0)
            {
                Debug.LogWarning("AutoSetPlayRange: No notes found in this beatmap.", this);
                return;
            }

            var firstNoteSeconds = BeatToSeconds(flat[0].BeatPosition);
            var lastNoteSeconds = BeatToSeconds(flat[^1].BeatPosition);
            var clipLen = clip.length;

            PlayRange = new FloatRange(
                Mathf.Clamp01(firstNoteSeconds / clipLen),
                Mathf.Clamp01(lastNoteSeconds / clipLen)
            );

            EditorUtility.SetDirty(this);
            Debug.Log($"AutoSetPlayRange: min={PlayRange.min:F4} ({firstNoteSeconds:F2}s) max={PlayRange.max:F4} ({lastNoteSeconds:F2}s)", this);
        }
#endif
    }
}