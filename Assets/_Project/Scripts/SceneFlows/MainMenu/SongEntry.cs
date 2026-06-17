using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameDevKit;
using GameDevKit.ObjectReferences;
using GameDevKit.UI;
using MagicTiles.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MagicTiles.MainMenu
{
    public class SongEntry : AdvancedBehaviour
    {
        [SerializeField] private BeatmapSO _beatmap;
        [SerializeField] private GameRuntimeDataSO _gameData;
        [SerializeField] private TMP_Text _songTitleText, _artistText;
        [SerializeField] private SceneReference _gamePlayScene;

        [SerializeField] private Button _playBtn, _autoPlayBtn;

        private void Awake()
        {
            _playBtn.onClick.AddListener(OnPlayClicked);
            _autoPlayBtn.onClick.AddListener(OnAutoPlayClicked);
        }

        private void OnPlayClicked()
        {
            _gameData.SetupSessionConfig(new(_beatmap, false, 1));
            ChangeScene();
        }

        private void OnAutoPlayClicked()
        {
            _gameData.SetupSessionConfig(new(_beatmap, true, 1));
            ChangeScene();
        }

        protected override void OnStartOrEnable()
        {
            _songTitleText.text = _beatmap.SongInfo.SongTitle;
            _artistText.text = _beatmap.SongInfo.ArtistName;
        }

        private async UniTask ChangeScene()
        {
            var currentSceneFlow = SceneFlow.GetSceneFlowOfObject(this);

            var fadeInTask = UIManager.FadeTransition(FadeSetting.FadeIn());
            var gameplaySceneHandle = await SceneFlow.LoadSceneWithoutActivation(_gamePlayScene, LoadSceneMode.Additive);
            await fadeInTask;

            var gameplaySceneFlow = await gameplaySceneHandle.Activate();
            await gameplaySceneFlow.PrepareScene();
            await currentSceneFlow.UnloadSelf();
            await UIManager.FadeTransition(FadeSetting.FadeOut());
        }

    }
}
