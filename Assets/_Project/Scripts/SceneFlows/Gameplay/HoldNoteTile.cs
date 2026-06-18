using UnityEngine;
using UnityEngine.EventSystems;
using MagicTiles.Data;
using Cysharp.Threading.Tasks;
using EditorAttributes;
using MoreMountains.Feedbacks;
using GameDevKit;
using TMPro;

namespace MagicTiles.Gameplay
{
    /// <summary>
    /// A standard hold note tile.
    ///
    public class HoldNoteTile : NoteTile, IPointerUpHandler, IPointerExitHandler
    {
        public enum TileState { None, Held, Released }

        [Tooltip("SpriteRenderer on the tile body. size.y is set to the hold height at Setup.")]
        [SerializeField] private SpriteRenderer _bodyRenderer;

        [Tooltip("SpriteRenderer on the fill bar child. Pivot at bottom-center. size.y grows upward.")]
        [SerializeField] private SpriteRenderer _fillRenderer;

        [SerializeField] private BoxCollider2D _collider;

        [SerializeField] private TextMeshPro _scoreText;

        [field: Header("Feedbacks")]
        [field: SerializeField] public MMF_Player HitFeedback { get; private set; }
        [field: SerializeField] public MMF_Player ReleaseEarlyFeedback { get; private set; }
        [field: SerializeField] public MMF_Player ReleaseFullFeedback { get; private set; }

        /// <summary>0 to 1 — how much of the hold window has been filled.</summary>
        public float FillAmount { get; private set; }

        public bool IsFilled => FillAmount >= 1f;

        private float _secondsHeld;
        private float _cellHeight;
        private float _tileHeight;
        private float _holdDurationSeconds;
        private TileState _tileState;

        protected override void OnSetup()
        {
            _tileState = TileState.None;
            _secondsHeld = 0f;
            FillAmount = 0f;

            _scoreText.text = $"+{_gameData.Scoring.FullHoldScore}";
            _scoreText.gameObject.SetActive(false);

            _cellHeight = _gameData.Map.CellSize.y;

            // Total height = one base cell (the tap head) + the hold body.
            // HoldBeats * SecondsPerBeat * TileSpeed converts beat-duration to world units.
            _holdDurationSeconds = Note.HoldBeats * _gameData.SessionConfig.Beatmap.SecondsPerBeat;
            _tileHeight = _cellHeight + _holdDurationSeconds * _gameData.TileSpeed;

            SetBodySize(_tileHeight);

            SetColliderHeight(_cellHeight);

            ResetFill();

            HitFeedback?.RestoreInitialValues();
            ReleaseEarlyFeedback?.RestoreInitialValues();
            ReleaseFullFeedback?.RestoreInitialValues();
        }

        public override void Tick(float deltaTime)
        {
            SyncPositionWithBeat();
            HandleAutoplay();

            if (_tileState is TileState.Held) { TickHolding(deltaTime); }

            var hitLineY = _gameData.Map.HitLine.transform.position.y;
            if (transform.position.y < hitLineY && !_bodyRenderer.isVisible) { OnPoolable?.Invoke(this); }
        }

        private void TickHolding(float deltaTime)
        {
            _secondsHeld += deltaTime;

            // Fill top tracks the hit line: start at cellHeight when tap registers,
            // grow by tileSpeed each second — identical to how far the tile has scrolled.
            var fillSizeY = Mathf.Clamp(_cellHeight + _secondsHeld * _gameData.TileSpeed, _cellHeight, _tileHeight);
            SetFillSize(fillSizeY);

            FillAmount = Mathf.Clamp01(_secondsHeld / _holdDurationSeconds);

            if (IsFilled)
            {
                Release();
            }
        }

        protected override async UniTask HandleOnTap()
        {
            if (_tileState is not TileState.None) { return; }

            var tapResult = _gameData.EvaluateTap(this);
            _gameData.RegisterHit(tapResult);

            _tileState = TileState.Held;
            HitFeedback?.PlayFeedbacks();

            SetColliderHeight(_tileHeight);
            SetFillSize(_cellHeight);
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            if (_tileState is not TileState.Held) { return; }
            Release();
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            if (_tileState is not TileState.Held) { return; }
            Release();
        }


        protected override async UniTask HandleOnRelease()
        {
            if (_tileState is not TileState.Held) { return; }
            _tileState = TileState.Released;

            IsConsumed = true;
            if (!IsFilled)
            {
                ReleaseEarlyFeedback.GetFeedbackOfType<MMF_ParticlesInstantiation>().TargetWorldPosition = _fillRenderer.transform.position.With(y: _fillRenderer.size.y);
                ReleaseEarlyFeedback?.PlayFeedbacks();
                return;
            }

            ReleaseFullFeedback.GetFeedbackOfType<MMF_ParticlesInstantiation>().TargetWorldPosition = _fillRenderer.transform.position.With(y: _fillRenderer.size.y);
            ReleaseFullFeedback?.PlayFeedbacks();

            _gameData.RegisterFullHold();
            _scoreText.gameObject.SetActive(true);
        }

        protected void HandleAutoplay()
        {
            if (!_gameData.SessionConfig.IsAutoplay) { return; }
            if (_tileState is not TileState.None) { return; }

            if (MatchesBeatOrPassed())
            {
                Tap();
            }
        }

        private void SetBodySize(float height)
        {
            _bodyRenderer.size = _fillRenderer.size.With(y: height);
            _scoreText.rectTransform.SetHeight(height);
        }

        private void SetFillSize(float height) => _fillRenderer.size = _fillRenderer.size.With(y: height);

        private void ResetFill() => SetFillSize(0f);

        private void SetColliderHeight(float height)
        {
            _collider.size = _collider.size.With(y: height);
            _collider.offset = _collider.offset.With(y: height / 2f);

        }
    }
}