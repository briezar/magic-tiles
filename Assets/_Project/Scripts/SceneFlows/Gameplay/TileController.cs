using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MagicTiles.Data;
using GameDevKit.Pool;
using Cysharp.Threading.Tasks;
using GameDevKit;
using MoreMountains.Feedbacks;
using System.Linq;
using EditorAttributes;

namespace MagicTiles.Gameplay
{
    public class TileController : AdvancedBehaviour
    {
        [SerializeField] private SerializableComponentPool<TapNoteTile> _tapNotesPool;
        [SerializeField] private SerializableComponentPool<HoldNoteTile> _holdNotesPool;
        [SerializeField] private StartNoteTile _startTile;

        [Header("Injected")]
        [SerializeField] private GameRuntimeDataSO _gameData;

        private BeatmapSO _beatmap;
        private float _spawnAheadBeats;

        private Note[] _notes;
        private readonly List<NoteTile> _activeTiles = new();
        private Coroutine _spawnCoroutine;
        private Coroutine _moveCoroutine;
        private int _nextNoteIndex;

        protected override void OnStartOrEnable()
        {
            ScriptableObjectContainer.AssignIfNull(ref _gameData);
        }

        public void Initialize()
        {
            _beatmap = _gameData.SessionConfig.Beatmap;

            var gridMap = _gameData.Map;

            _gameData.MusicPlayer.Pitch = _gameData.SessionConfig.SpeedMultiplier;

            var distanceToSpawn = gridMap.GetSpawnDistanceFromHitLine();
            _spawnAheadBeats = (distanceToSpawn / _gameData.TileSpeed) * _beatmap.BeatsPerSecond;

            _notes = _beatmap.FlatNotes;
            _nextNoteIndex = 0;
            _activeTiles.Clear();

            _tapNotesPool.ReleaseAll();
            _tapNotesPool.Prepare(12);

            _holdNotesPool.ReleaseAll();
            _holdNotesPool.Prepare(4);

            var startTileLane = Random.Range(0, Constants.MaxLane);
            _startTile.Note.Lane = startTileLane;
            _startTile.transform.position = new Vector3(gridMap.GetSpawnPosition(startTileLane).x, gridMap.HitLine.transform.position.y, 0f);
            _startTile.gameObject.SetActive(true);

            _activeTiles.Add(_startTile);
        }

        public async UniTask WaitForTapStartTile()
        {
            var tapped = false;
            _startTile.OnTap = _ => tapped = true;
            await UniTask.WaitUntil(() => tapped, cancellationToken: destroyCancellationToken);
            _startTile.OnTap = null;
            _activeTiles.Remove(_startTile);
            _startTile.HitFeedback.PlayFeedbacksAsync().ContinueWith(() =>
            {
                _startTile.gameObject.SetActive(false);
                _startTile.HitFeedback?.RestoreInitialValues();
            });
        }

        public void StartSpawning()
        {
            _spawnCoroutine = StartCoroutine(SpawnRoutine());
            _moveCoroutine = StartCoroutine(MoveRoutine());
        }

        public void StopSpawning()
        {
            _spawnCoroutine.Stop(this);
            _moveCoroutine.Stop(this);
        }


        // ── Spawn loop ───────────────────────────────────────────────────────────

        private IEnumerator SpawnRoutine()
        {
            while (_nextNoteIndex < _notes.Length)
            {
                if (_gameData.CurrentBeat >= _notes[_nextNoteIndex].BeatPosition - _spawnAheadBeats)
                {
                    SpawnTile(_notes[_nextNoteIndex]);
                    _nextNoteIndex++;
                }
                yield return null;
            }
        }

        // ── Main loop ────────────────────────────────────────────────────────────

        private IEnumerator MoveRoutine()
        {
            while (true)
            {
                for (var i = _activeTiles.Count - 1; i >= 0; i--)
                {
                    var tile = _activeTiles[i];
                    tile.Tick(Time.deltaTime);
                }

                yield return null;
            }
        }

        private readonly Dictionary<int, NoteTile> _tappedTilesByLane = new();
        public void TapLowestInLane(int lane)
        {
            var tile = GetLowestTileInLane(lane);
            if (tile != null)
            {
                _tappedTilesByLane[lane] = tile;
                tile.Tap();
            }
        }
        public void ReleaseLowestInLane(int lane)
        {
            if (_tappedTilesByLane.Remove(lane, out var tile))
            {
                tile.Release();
            }
        }

        private NoteTile GetLowestTileInLane(int lane)
        {
            NoteTile lowest = null;
            for (var i = 0; i < _activeTiles.Count; i++)
            {
                var tile = _activeTiles[i];
                if (tile.Note.Lane != lane) { continue; }
                if (tile.IsConsumed) { continue; }
                if (lowest == null || tile.transform.position.y < lowest.transform.position.y) { lowest = tile; }
            }
            return lowest;
        }

        // ── Spawn ────────────────────────────────────────────────────────────────

        private void SpawnTile(Note note)
        {
            var spawnPos = _gameData.Map.GetSpawnPosition(note.Lane);
            var tile = GetTileFromPool(note);

            tile.transform.position = spawnPos;
            tile.Setup(note, _gameData);

            tile.OnPoolable = ReturnTileToPool;
            _activeTiles.Add(tile);
        }

        private NoteTile GetTileFromPool(Note note) => note.Type switch
        {
            NoteType.Tap => _tapNotesPool.Get(),
            NoteType.Hold => _holdNotesPool.Get(),
            _ => null,
        };

        private void ReturnTileToPool(NoteTile tile)
        {
            _activeTiles.Remove(tile);
            switch (tile)
            {
                case TapNoteTile tapNoteTile:
                    _tapNotesPool.Release(tapNoteTile);
                    break;
                case HoldNoteTile holdNoteTile:
                    _holdNotesPool.Release(holdNoteTile);
                    break;
            }
        }

    }
}