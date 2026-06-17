using System;
using GameDevKit;
using MagicTiles.Gameplay;
using EditorAttributes;
using UnityEngine;

namespace MagicTiles.Data
{
    /// <summary>
    /// Shared runtime state stored in a ScriptableObject so any system
    /// (UI, audio, effects) can read it without a direct GameManager reference.
    /// All mutations happen through the named methods; raw setters are internal.
    ///
    /// Events fire on the SO itself, so listeners survive scene reloads as long
    /// as they subscribe in OnEnable / unsubscribe in OnDisable.
    /// </summary>
    [ResetOnExitPlayMode]
    [RegisterToGlobalContainer]
    [CreateAssetMenu(menuName = "MagicTiles/Game Runtime Data")]
    public class GameRuntimeDataSO : ScriptableObject
    {
        [field: SerializeField] public ObservableInt Score { get; private set; }
        [field: SerializeField] public ObservableInt Combo { get; private set; }
        [field: SerializeField] public int BestCombo { get; private set; }
        [field: SerializeField] public ScoringConfigSO Scoring { get; set; }

        [Header("Game Session Config")]
        [SerializeField] private SessionConfig _sessionConfig;
        public ref readonly SessionConfig SessionConfig => ref _sessionConfig;

        [field: Header("Scene Objects")]
        [field: SerializeField] public GridMap Map { get; private set; }
        [field: SerializeField] public MusicPlayer MusicPlayer { get; private set; }

        [ShowInInspector]
        public float CurrentBeat => (MusicPlayer && SessionConfig.Beatmap) ? (MusicPlayer.Time - SessionConfig.Beatmap.OffsetSeconds) * SessionConfig.Beatmap.BeatsPerSecond : 0;

        private float? _tileSpeedCached;

        [ShowInInspector]
        public float TileSpeed
        {
            get
            {
                if (!SessionConfig.Beatmap || !Map)
                {
                    _tileSpeedCached = null;
                    return 0;
                }
                _tileSpeedCached ??= SessionConfig.Beatmap.GetTileSpeed(Map.CellSize.y) * SessionConfig.SpeedMultiplier;
                return _tileSpeedCached.Value;
            }
        }

        public GameResult GameResult { get; private set; }

        public readonly SourcedAction OnStartGame = new();
        public readonly SourcedAction<HitResult> OnTileTapped = new();
        public readonly SourcedAction<GameResult> OnGameResult = new();

        public readonly SourcedDelegateBag AllEvents = new();

        private void OnEnable()
        {
            AllEvents.Add(Score.OnValueChanged);
            AllEvents.Add(Combo.OnValueChanged);

            AllEvents.Add(OnStartGame);
            AllEvents.Add(OnTileTapped);
            AllEvents.Add(OnGameResult);
        }

        public void SetSceneObjects(GridMap gridMap, MusicPlayer musicPlayer)
        {
            Map = gridMap;
            MusicPlayer = musicPlayer;
        }

        public void SetupSessionConfig(SessionConfig sessionConfig)
        {
            _sessionConfig = sessionConfig;

            // re-cache
            _tileSpeedCached = null;
        }

        public void RegisterFullHold()
        {
            var pts = Mathf.RoundToInt(Scoring.FullHoldScore * Scoring.GetComboMultiplier(Combo));
            Score.Value += pts;
        }

        public void RegisterMiss()
        {
            Combo.Value = 0;
        }

        public void RegisterHit(HitResult result)
        {
            var pts = Mathf.RoundToInt(Scoring.GetBasePoints(result.Rank) * Scoring.GetComboMultiplier(Combo));
            Score.Value += pts;

            if (result.Rank is HitRank.Perfect) { Combo.Value++; }
            else { Combo.Value = 0; }

            if (Combo > BestCombo) { BestCombo = Combo; }

            OnTileTapped?.Invoke(result);
        }

        public void ResetSession()
        {
            Score.Value = 0;
            Combo.Value = 0;
            BestCombo = 0;
            GameResult = null;

            SetupSessionConfig(_sessionConfig);
        }

        public void RaiseGameOver(GameResult gameResult)
        {
            GameResult = gameResult;
            if (!SessionConfig.IsAutoplay)
            {
                SaveHighScore();
            }

            OnGameResult?.Invoke(gameResult);
        }

        public int LoadHighScore() => PlayerPrefs.GetInt($"HighScore_{SessionConfig.Beatmap?.SongInfo?.SongTitle}", 0);

        public void SaveHighScore()
        {
            if (SessionConfig.Beatmap?.SongInfo == null) { return; }
            var key = $"HighScore_{SessionConfig.Beatmap.SongInfo.SongTitle}";
            if (Score > PlayerPrefs.GetInt(key, 0))
            {
                PlayerPrefs.SetInt(key, Score);
                PlayerPrefs.Save();
            }
        }
    }

    public abstract record GameResult;
    public record WinResult : GameResult;
    public record LoseResult : GameResult;

    [Serializable]
    public struct SessionConfig
    {
        public BeatmapSO Beatmap;
        public bool IsAutoplay;

        /// <summary>
        /// Scales both AudioSource.pitch and tile speed simultaneously.
        /// 1.0 = normal, 1.2 = 20% faster music and tiles.
        /// </summary>
        public float SpeedMultiplier;

        public SessionConfig(BeatmapSO beatmap, bool isAutoplay, float speedMultiplier = 1)
        {
            Beatmap = beatmap;
            IsAutoplay = isAutoplay;
            SpeedMultiplier = speedMultiplier;
        }
    }

    public static class GameRuntimeDataExtensions
    {
        public static (float currentTime, float desiredTime) GetBeatTiming(this GameRuntimeDataSO gameData, NoteTile tile)
        {
            var currentTime = gameData.MusicPlayer.Time;
            var desiredTime = gameData.SessionConfig.Beatmap.BeatToSeconds(tile.Note.BeatPosition);
            return (currentTime, desiredTime);
        }

        public static HitResult EvaluateTap(this GameRuntimeDataSO gameData, NoteTile tile)
        {
            var (currentTime, desiredTime) = GetBeatTiming(gameData, tile);
            return gameData.Scoring.EvaluateTap(currentTime, desiredTime);
        }

    }
}