using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using MagicTiles.UI;
using MagicTiles.Gameplay;
using GameDevKit.ObjectReferences;

namespace MagicTiles
{
    public class MainMenuSceneFlow : SceneFlow
    {
        [SerializeField] private SceneReference _gameplayScene;

        public override async UniTask PrepareScene(Action<ProgressInfo> progressCallback = null)
        {
            // Wait for game systems Start()
            await UniTask.NextFrame();
            await UniTaskUtils.WaitUntilStableFps();
        }

    }
}