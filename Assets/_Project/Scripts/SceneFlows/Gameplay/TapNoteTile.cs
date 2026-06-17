using MagicTiles.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;
using MoreMountains.Feedbacks;

namespace MagicTiles.Gameplay
{
    /// <summary>
    /// A standard single-tap tile.
    /// </summary>
    public class TapNoteTile : NoteTile
    {
        [SerializeField] private SpriteRenderer _bodyRenderer;

        [field: SerializeField] public MMF_Player HitFeedback { get; private set; }

        private bool _canMove = true;
        private bool _tapped = false;

        protected override void OnSetup()
        {
            _canMove = true;
            _tapped = false;
            HitFeedback?.RestoreInitialValues();
        }

        public override void Tick(float deltaTime)
        {
            if (_canMove)
            {
                SyncPositionWithBeat();
                HandleAutoplay();
                var hitLineY = _gameData.Map.HitLine.transform.position.y;
                if (transform.position.y < hitLineY && !_bodyRenderer.isVisible)
                {
                    OnPoolable?.Invoke(this);
                }
            }
        }

        protected override async UniTask HandleOnTap()
        {
            if (_tapped) { return; }
            _tapped = true;

            var tapResult = _gameData.EvaluateTap(this);
            _gameData.RegisterHit(tapResult);

            IsConsumed = true;

            var hitFeedback = _gameData.Map.LaneGlows[Note.Lane].HitFeedback;
            hitFeedback.PlayFeedbacks();

            _canMove = false;
            await HitFeedback.PlayFeedbacksAsync(destroyCancellationToken);
            OnPoolable?.Invoke(this);
        }

        protected void HandleAutoplay()
        {
            if (!_gameData.SessionConfig.IsAutoplay) { return; }

            if (MatchesBeatOrPassed())
            {
                Tap();
            }
        }

    }
}