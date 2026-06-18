using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameDevKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MagicTiles.Data;
using MoreMountains.Feedbacks;
using GameDevKit.ObjectReferences;
using UnityEngine.SceneManagement;
using GameDevKit.UI;
using AYellowpaper.SerializedCollections;

namespace MagicTiles.UI
{
    public class GameplayUI : ScreenUI
    {
        [SerializeField] private SceneReference _mainMenuScene;

        [Header("UI References")]
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _comboText, _comboMultiplierText;

        [SerializeField] private GameObject _infoBox;
        [SerializeField] private TMP_Text _songTitle, _singer, _highScore;

        [Header("Ratings")]
        [SerializeField] private SerializedDictionary<HitRank, GameObject> _rankToObject;

        [Header("FX")]
        [SerializeField] private MMF_Player _stereoPulseFeedback;
        [SerializeField] private MMF_Player _tileTapFeedback;
        [SerializeField] private MMF_Player _scoreChangedFeedback;
        [SerializeField] private MMF_Player _comboChangedFeedback;
        [SerializeField] private MMF_Player _comboMultiplierChangedFeedback;


        [Header("Injected")]
        [SerializeField] private GameRuntimeDataSO _gameData;

        protected override void OnStart()
        {
            ScriptableObjectContainer.AssignIfNull(ref _gameData);

            _scoreText.text = "0";
            _comboText.text = "";
            _comboMultiplierText.text = "";
        }

        protected override void OnStartOrEnable()
        {
            _gameData.Score.OnValueChanged[this] += HandleOnScoreChanged;
            _gameData.Combo.OnValueChanged[this] += HandleOnComboChanged;

            _gameData.OnTileTapped[this] += HandleOnTileTapped;
            _gameData.OnGameResult[this] += HandleOnGameResult;

            ShowInfoBox();
            async UniTaskVoid ShowInfoBox()
            {
                _songTitle.text = _gameData.SessionConfig.Beatmap?.SongInfo.SongTitle;
                _singer.text = _gameData.SessionConfig.Beatmap?.SongInfo.ArtistName;
                _highScore.text = $"High Score: {_gameData.LoadHighScore()}";

                await UniTask.WaitForSeconds(0.5f);
                _infoBox.gameObject.SetActive(true);
            }

            _gameData.OnStartGame[this] += () => _infoBox.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _gameData?.AllEvents.UnsubscribeSource(this);
        }

        private void HandleOnGameResult(GameResult result)
        {
            _comboMultiplierText.text = "";
        }

        private void HandleOnScoreChanged(IntChangeInfo scoreInfo)
        {
            _scoreText.text = scoreInfo.current.ToString();
            if (scoreInfo.Diff != 0)
            {
                _scoreChangedFeedback?.PlayFeedbacks();
            }
        }

        private void HandleOnComboChanged(IntChangeInfo comboInfo)
        {
            var (prevCombo, combo) = (comboInfo.previous, comboInfo.current);
            if (combo == 0)
            {
                _comboMultiplierText.text = $"";
                _comboChangedFeedback.GetFeedbackOfType<MMF_Scale>().RemapCurveOne = 1;
                return;
            }

            if (combo > 1)
            {
                if (_gameData.Scoring.ComboMultipliers.TryGetValue(combo, out var multiplier))
                {
                    _comboChangedFeedback.GetFeedbackOfType<MMF_Scale>().RemapCurveOne = multiplier;
                    _comboMultiplierText.text = $"{multiplier}x";

                    _comboMultiplierChangedFeedback.GetFeedbackOfType<MMF_Scale>().RemapCurveOne = multiplier;
                    _comboMultiplierChangedFeedback?.PlayFeedbacks();
                }

                _comboText.text = $"x{combo}";
                _comboChangedFeedback?.PlayFeedbacks();
            }

        }

        private void HandleOnTileTapped(HitResult tapResult)
        {
            foreach (var (rank, obj) in _rankToObject)
            {
                obj.SetActive(tapResult.Rank == rank);
            }

            _tileTapFeedback?.PlayFeedbacks();

            if (tapResult.Rank is not HitRank.None)
            {
                _stereoPulseFeedback?.PlayFeedbacks();
                if (tapResult.Rank is HitRank.Perfect)
                {
                    _stereoPulseFeedback?.PlayFeedbacks();
                }
            }
        }

        public void ReturnToMainMenu()
        {
            Run();
            async UniTaskVoid Run()
            {
                var fadeInTask = UIManager.FadeTransition(FadeSetting.FadeIn());
                var mainMenuSceneHandle = await SceneFlow.LoadSceneWithoutActivation(_mainMenuScene, LoadSceneMode.Additive);
                await fadeInTask;

                var mainMenuSceneFlow = await mainMenuSceneHandle.Activate();
                await mainMenuSceneFlow.PrepareScene();
                var currentSceneFlow = SceneFlow.GetSceneFlowOfObject(this);
                await currentSceneFlow.UnloadSelf();
                await UIManager.FadeTransition(FadeSetting.FadeOut());
            }
        }

    }
}
