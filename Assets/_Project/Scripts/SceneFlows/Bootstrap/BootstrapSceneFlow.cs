using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameDevKit.ObjectReferences;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MagicTiles.UI;
using PrimeTween;
using IngameDebugConsole;
using GameDevKit.UI;

namespace MagicTiles
{
    public class BootstrapSceneFlow : SceneFlow
    {
        [SerializeField] private SplashUI _splashUI;

        [SerializeField] private SceneReference _servicesScene;
        [SerializeField] private SceneReference _nextScene;

#if UNITY_EDITOR
        private void Awake()
        {
            if (SceneManager.sceneCount > 1)
            {
                SceneManager.LoadScene(gameObject.scene.name);
                return;
            }
        }
#endif

        protected override async UniTaskVoid OnStart()
        {
            SceneManager.LoadScene(_servicesScene, LoadSceneMode.Additive);
            await UniTask.WaitUntil(() => UIManager.IsReady);

            UIManager.FadeTransition(FadeSetting.FadeIn(0));

            await UniTask.NextFrame();
            await UniTaskUtils.WaitUntilStableFps();

            await UIManager.FadeTransition(FadeSetting.FadeOut());

            await UniTask.WaitForSeconds(0.1f);
            _splashUI.SetInfo("Loading game systems...");
            _splashUI.RunProgress(0.1f, 0.2f);

            var totalProgress = 0.5f;
            var nextSceneFlow = await LoadScene(_nextScene, LoadSceneMode.Additive, HandleProgress);

            totalProgress = 0.8f;
            await nextSceneFlow.PrepareScene(HandleProgress);

            totalProgress = 1f;
            await _splashUI.RunProgress(totalProgress, 0.25f);

            _splashUI.SetInfo("Loading complete!");
            await UniTask.WaitForSeconds(0.2f);

            await UIManager.FadeTransition(FadeSetting.FadeIn());

            UIManager.HideUI(_splashUI);

            await UnloadSelf();
            await UIManager.FadeTransition(FadeSetting.FadeOut());

            void HandleProgress(ProgressInfo info)
            {
                _splashUI.RunProgress(info.TargetProgress * totalProgress, 0.25f);
                _splashUI.SetInfo(info.Message);
            }
        }

    }
}