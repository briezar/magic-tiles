using UnityEngine;
using MagicTiles.Data;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using EditorAttributes;
using GameDevKit;
using Cysharp.Threading.Tasks;

namespace MagicTiles.Gameplay
{
    /// <summary>
    /// Abstract base for all scrolling note tiles.
    ///
    /// <b>Contract with TileController:</b>
    /// <list type="bullet">
    ///   <item>Controller subscribes to <see cref="OnTap"/> at spawn; the tile fires it on pointer-down.</item>
    /// </list>
    /// </summary>
    public abstract class NoteTile : AdvancedBehaviour, IPointerDownHandler
    {
        [field: SerializeField, ReadOnly]
        public Note Note { get; private set; }

        public bool IsConsumed { get; protected set; }

        /// <summary> Fired on pointer-down/up </summary>
        public Action<NoteTile> OnTap, OnRelease;

        /// <summary> Fired when the tile is ready to be release back to the pool. </summary>
        public Action<NoteTile> OnPoolable;

        protected GameRuntimeDataSO _gameData;

        public void Setup(Note note, GameRuntimeDataSO gameData)
        {
            Note = note;
            _gameData = gameData;
            IsConsumed = false;
            OnSetup();
        }

        protected virtual void OnSetup() { }

        /// <summary>
        /// Called every frame by TileController.
        /// </summary>
        public abstract void Tick(float deltaTime);

        protected void SyncPositionWithBeat()
        {
            var bpm = _gameData.SessionConfig.Beatmap.Bpm;
            var currentBeat = _gameData.CurrentBeat;
            var hitLineY = _gameData.Map.HitLine.transform.position.y;
            var tileSpeed = _gameData.TileSpeed;
            SyncPositionWithBeat(bpm, currentBeat, tileSpeed, hitLineY);
        }
        protected void SyncPositionWithBeat(float bpm, float currentBeat, float tileSpeed, float hitLineY)
        {
            var beatsRemaining = Note.BeatPosition - currentBeat;
            var timeRemainingSeconds = beatsRemaining * 60f / bpm;
            var pos = transform.position;
            pos.y = hitLineY + timeRemainingSeconds * tileSpeed;
            transform.position = pos;
        }

        public void Tap()
        {
            OnTap?.Invoke(this);
            HandleOnTap();
        }

        public void Release()
        {
            OnRelease?.Invoke(this);
            HandleOnRelease();
        }

        public bool MatchesBeatOrPassed()
        {
            var (currentTime, desiredTime) = _gameData.GetBeatTiming(this);
            return currentTime >= desiredTime - 0.01f;
        }

        protected virtual async UniTask HandleOnTap() { }
        protected virtual async UniTask HandleOnRelease() { }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData) => Tap();
    }
}