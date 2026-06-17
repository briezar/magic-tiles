using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using MagicTiles.UI;
using MagicTiles.Gameplay;

namespace MagicTiles
{
    public class GameplaySceneFlow : SceneFlow
    {
        [SerializeField] private GameSession _gameSession;

        protected override async UniTaskVoid OnStart()
        {
            await UniTask.WaitForSeconds(0.5f);
            _gameSession.StartSession();
        }

        public override async UniTask PrepareScene(Action<ProgressInfo> progressCallback = null)
        {
            // Wait for game systems Start()
            await UniTask.NextFrame();
            await UniTaskUtils.WaitUntilStableFps();
        }

    }
}