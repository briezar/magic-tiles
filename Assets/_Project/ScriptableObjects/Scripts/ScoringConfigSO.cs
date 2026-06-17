using UnityEngine;
using AYellowpaper.SerializedCollections;
using MagicTiles.Gameplay;
using System.Collections.Generic;

namespace MagicTiles.Data
{
    [CreateAssetMenu(menuName = "MagicTiles/Scoring Config")]
    public class ScoringConfigSO : ScriptableObject
    {
        [Header("Hit Windows (seconds from beat time)")]
        [SerializeField]
        private SerializedDictionary<HitRank, float> _hitWindows = new()
        {
            { HitRank.Perfect, 0.04f },
            { HitRank.Great, 0.08f },
            { HitRank.Cool, 0.15f }
        };

        [Header("Base Points per Rank")]
        [SerializeField]
        private SerializedDictionary<HitRank, int> _basePoints = new()
        {
            { HitRank.Perfect, 150 },
            { HitRank.Great, 100 },
            { HitRank.Cool, 50 },
        };

        [Header("Combo Multiplier Thresholds")]
        [SerializeField]
        private SerializedDictionary<int, float> _comboMultipliers = new()
        {
            { 10, 1.2f },
            { 20, 1.5f },
            { 50, 2.0f },
        };

        [field: SerializeField] public int FullHoldScore { get; private set; }

        public IReadOnlyDictionary<int, float> ComboMultipliers => _comboMultipliers;

        // private void OnEnable()
        // {
        //     Debug.Log($"Hit windows: {_hitWindows.JoinToString(h => $"[{h.Key}:{h.Value}]")}");
        //     Debug.Log($"Base points: {_basePoints.JoinToString(h => $"[{h.Key}:{h.Value}]")}");
        //     Debug.Log($"Combo multipliers: {_comboMultipliers.JoinToString(h => $"[{h.Key}:{h.Value}]")}");
        // }

        public HitResult EvaluateTap(float currentTime, float desiredTime)
        {
            var delta = Mathf.Abs(currentTime - desiredTime);
            foreach (var (rank, window) in _hitWindows)
            {
                if (delta < window) { return HitResult.Hit(rank, delta); }
            }
            return HitResult.Hit(HitRank.None, delta);
        }

        public int GetBasePoints(HitRank rank)
        {
            if (_basePoints.TryGetValue(rank, out var points)) { return points; }
            return 0;
        }

        public float GetComboMultiplier(int combo)
        {
            float multiplier = 1.0f;
            foreach (var (threshold, value) in _comboMultipliers)
            {
                if (combo >= threshold) { multiplier = value; }
            }
            return multiplier;
        }
    }
}
