using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using MagicTiles.Data;

namespace MagicTiles.Editor
{
    /// <summary>
    /// EditorWindow: parse a .sm file and write a BeatmapSO asset.
    /// Menu: MagicTiles > StepMania Parser
    /// </summary>
    public class SmFileParser : EditorWindow
    {
        private string _smFilePath = "";
        private SongInfoSO _targetSongInfo;
        private BeatmapSO _targetBeatmap;

        [MenuItem("MagicTiles/StepMania Parser")]
        public static void ShowWindow() => GetWindow<SmFileParser>("SM Parser");

        private void OnGUI()
        {
            GUILayout.Label("StepMania → BeatmapSO", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // .sm file picker
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("SM File", GUILayout.Width(80));
            EditorGUILayout.LabelField(string.IsNullOrEmpty(_smFilePath) ? "None" : Path.GetFileName(_smFilePath));
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFilePanel("Select .sm file", "", "sm");
                if (!string.IsNullOrEmpty(path)) { _smFilePath = path; }
            }
            EditorGUILayout.EndHorizontal();

            _targetSongInfo = (SongInfoSO)EditorGUILayout.ObjectField("Song Info", _targetSongInfo, typeof(SongInfoSO), false);
            _targetBeatmap = (BeatmapSO)EditorGUILayout.ObjectField("Target Beatmap", _targetBeatmap, typeof(BeatmapSO), false);

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_smFilePath));

            var buttonTitle = _targetBeatmap == null ? "Parse & Create" : "Update Beatmap";
            if (GUILayout.Button(buttonTitle)) { ParseAndCreate(); }
            EditorGUI.EndDisabledGroup();
        }

        // ── Parsing ────────────────────────────────────────────────────────────

        private void ParseAndCreate()
        {
            var rawText = File.ReadAllText(_smFilePath);

            // ── 1. Header fields ───────────────────────────────────────────────
            var bpm = ParseHeaderFloat(rawText, "BPMS");        // "0.000000=135.000000"
            if (bpm <= 0f)
            {
                EditorUtility.DisplayDialog("Error", "Could not parse BPM from #BPMS tag.", "OK");
                return;
            }

            // #OFFSET is negative when audio leads the grid → flip to positive "seconds before beat 0"
            var rawOffset = ParseHeaderFloat(rawText, "OFFSET");
            var offsetSeconds = -rawOffset;

            // Parse song metadata
            var songTitle = ParseHeaderString(rawText, "TITLE");
            var artistName = ParseHeaderString(rawText, "ARTIST");

            // ── 2. Notes section ───────────────────────────────────────────────
            var notesStart = rawText.IndexOf("#NOTES:");
            if (notesStart == -1)
            {
                EditorUtility.DisplayDialog("Error", "No #NOTES block found.", "OK");
                return;
            }

            // Skip the 5 metadata lines inside #NOTES (type, desc, difficulty, meter, radar)
            var notesSection = rawText.Substring(notesStart + "#NOTES:".Length);
            var cleanNotes = StripNotesHeader(notesSection);

            // Split into measures by comma
            var rawMeasures = cleanNotes.Split(',');
            var blocks = new List<Block>();
            // track open hold heads across measures: column → Note
            var openHolds = new Dictionary<int, Note>();

            for (var measureIndex = 0; measureIndex < rawMeasures.Length; measureIndex++)
            {
                var measureText = rawMeasures[measureIndex].Replace(";", "").Trim();
                if (string.IsNullOrEmpty(measureText)) { continue; }

                var rows = measureText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                var rowCount = rows.Length;
                if (rowCount == 0) { continue; }

                var baseBeat = measureIndex * 4f;
                var beatStep = 4f / rowCount;

                var sparseNotes = new List<Note>();

                const char Tap = '1';
                const char HoldHead = '2';
                const char HoldTail = '3';

                for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
                {
                    var row = rows[rowIndex].Trim();
                    if (row.Length < 4) { continue; }

                    var beat = baseBeat + rowIndex * beatStep;

                    for (var lane = 0; lane < 4; lane++)
                    {
                        var ch = row[lane];
                        if (ch == '0') { continue; }

                        switch (ch)
                        {
                            case Tap:
                                {
                                    var tap = new Note
                                    {
                                        Type = NoteType.Tap,
                                        Lane = lane,
                                        Row = rowIndex,
                                        BeatPosition = beat,
                                    };
                                    sparseNotes.Add(tap);
                                    break;
                                }
                            case HoldHead:
                                {
                                    var headNote = new Note
                                    {
                                        Type = NoteType.Hold,
                                        Lane = lane,
                                        Row = rowIndex,
                                        BeatPosition = beat,
                                    };
                                    openHolds[lane] = headNote;
                                    sparseNotes.Add(headNote);
                                    break;
                                }
                            case HoldTail:
                                {
                                    if (openHolds.TryGetValue(lane, out var head))
                                    {
                                        head.HoldBeats = beat - head.BeatPosition;
                                        openHolds.Remove(lane);
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"Hold tail found at beat {beat} without matching head!");
                                    }
                                    break;
                                }
                        }
                    }
                }

                blocks.Add(new Block
                {
                    RowCount = rowCount,
                    Notes = sparseNotes.ToArray(),
                });
            }

            // ── 3. Handle SongInfoSO (create or edit) ─────────────────────────────
            SongInfoSO songInfo = _targetSongInfo;
            if (songInfo == null)
            {
                songInfo = CreateInstance<SongInfoSO>();
                songInfo.SongTitle = songTitle;
                songInfo.ArtistName = artistName;

                // Determine save directory (same as beatmap or a default)
                string songInfoDir;
                if (_targetBeatmap != null)
                {
                    songInfoDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(_targetBeatmap));
                }
                else
                {
                    songInfoDir = "Assets/_Project/Music"; // fallback directory
                    if (!AssetDatabase.IsValidFolder(songInfoDir))
                    {
                        songInfoDir = "Assets";
                    }
                }

                string songInfoPath = AssetDatabase.GenerateUniqueAssetPath($"{songInfoDir}/SongInfo_{songTitle}.asset");
                AssetDatabase.CreateAsset(songInfo, songInfoPath);
            }
            else
            {
                // Update existing SongInfoSO with parsed data
                songInfo.SongTitle = songTitle;
                songInfo.ArtistName = artistName;
                EditorUtility.SetDirty(songInfo);
            }

            // ── 4. Create or update beatmap ────────────────────────────────────
            var beatmap = _targetBeatmap != null ? _targetBeatmap : CreateInstance<BeatmapSO>();
            beatmap.SongInfo = songInfo;
            beatmap.Bpm = bpm;
            beatmap.OffsetSeconds = offsetSeconds;

            // Use reflection-free approach: set via SerializedObject so undo works
            var so = new SerializedObject(beatmap);
            so.FindProperty("Blocks").arraySize = 0; // clear default or previous notes
            so.ApplyModifiedPropertiesWithoutUndo();

            // Directly assign — field is public
            beatmap.Blocks = blocks.ToArray();
            EditorUtility.SetDirty(beatmap);

            string assetPath;
            if (_targetBeatmap != null)
            {
                assetPath = AssetDatabase.GetAssetPath(_targetBeatmap);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                var dir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(songInfo));
                if (string.IsNullOrEmpty(dir)) { dir = "Assets"; }
                assetPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/Beatmap_{songInfo.SongTitle}.asset");
                AssetDatabase.CreateAsset(beatmap, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog(
                "Done",
                $"BPM: {bpm}\nOffset: {offsetSeconds:F6}s\nMeasures: {blocks.Count}\nNotes: {beatmap.FlatNotes.Length}\n\nSaved to: {assetPath}",
                "OK"
            );

            Selection.activeObject = beatmap;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>
        /// Parse a string from a header tag. Extracts text between "#KEY:" and ";".
        /// </summary>
        private static string ParseHeaderString(string text, string key)
        {
            var tag = $"#{key}:";
            var start = text.IndexOf(tag);
            if (start == -1) { return "Unknown"; }

            start += tag.Length;
            var end = text.IndexOf(';', start);
            if (end == -1) { end = text.Length; }

            var value = text.Substring(start, end - start).Trim();
            return string.IsNullOrEmpty(value) ? "Unknown" : value;
        }

        /// <summary>
        /// Parse a float from a header tag. Handles both "#KEY:VALUE;" and "#KEY:seed=VALUE,…;"
        /// For BPMS it returns the first BPM value after the '=' sign.
        /// </summary>
        private static float ParseHeaderFloat(string text, string key)
        {
            var tag = $"#{key}:";
            var start = text.IndexOf(tag);
            if (start == -1) { return 0f; }

            start += tag.Length;
            var end = text.IndexOf(';', start);
            if (end == -1) { end = text.Length; }

            var value = text.Substring(start, end - start).Trim();

            // BPMS format: "0.000000=135.000000\n,79.312500=135.000000"
            // grab everything after the first '='
            var eqIdx = value.IndexOf('=');
            if (eqIdx != -1) { value = value.Substring(eqIdx + 1).Trim(); }

            // Truncate at comma or newline (multiple BPM segments — we take the first)
            foreach (var stop in new[] { ',', '\n', '\r' })
            {
                var idx = value.IndexOf(stop);
                if (idx != -1) { value = value.Substring(0, idx); }
            }

            return float.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0f;
        }

        /// <summary>
        /// Strip the 5-line metadata header inside a #NOTES block, returning just the measure data.
        /// Lines ending with ':' or containing dance-single/difficulty strings are skipped.
        /// </summary>
        private static string StripNotesHeader(string notesSection)
        {
            using var reader = new StringReader(notesSection);
            var output = new System.Text.StringBuilder();
            var headerLinesSkipped = 0;

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//")) { continue; }

                // Skip the 5 metadata lines: they all end with ':'
                if (headerLinesSkipped < 5 && trimmed.EndsWith(":"))
                {
                    headerLinesSkipped++;
                    continue;
                }

                output.AppendLine(trimmed);
            }

            return output.ToString();
        }
    }
}