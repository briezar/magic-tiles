using System.Collections;
using UnityEngine;
using MagicTiles.Data;
using GameDevKit;
using Cysharp.Threading.Tasks;
using EditorAttributes;
using MoreMountains.Feedbacks;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using GameDevKit.ObjectReferences;
using GameDevKit.UI;

namespace MagicTiles.Gameplay
{
    public class GameSession : AdvancedBehaviour
    {
        // [Expandable]
        [SerializeField] private GameRuntimeDataSO _gameData;

        [Header("Configs")]
        [Tooltip("Seconds of silence after the start tile is tapped before music and tiles begin.")]
        [SerializeField] private float _prerollSeconds = 2f;

        [Header("Scene Objects")]
        [SerializeField] private TileController _tileController;
        [SerializeField] private MusicPlayer _musicPlayer;
        [SerializeField] private GridMap _gridMap;

        [Header("Feedbacks")]
        [SerializeField] private MMF_Player _changeBgFeedback;
        [SerializeField] private MMF_Player _startFeedback;
        [SerializeField] private MMF_Player _loseFeedback;
        [SerializeField] private MMF_Player _winFeedback;

        [SerializeField] private SceneReference _mainMenuScene;

        private void Awake()
        {
            _gameData.SetSceneObjects(_gridMap, _musicPlayer);
        }

        private void OnDestroy()
        {
            _gameData.ResetSession();
        }

        protected override void OnStartOrEnable()
        {
            _changeBgFeedback.PlayFeedbacks();

            _gameData.OnGameResult[this] = (result) => HandleGameResult(result);

            async UniTaskVoid HandleGameResult(GameResult result)
            {
                if (result is WinResult)
                {
                    _winFeedback?.PlayFeedbacks();
                }
                else
                {
                    _loseFeedback?.PlayFeedbacks();
                }

                await UniTask.WaitForSeconds(2f);

                await UIManager.FadeTransition(FadeSetting.FadeIn());

                var nextSceneFlow = await SceneFlow.LoadScene(_mainMenuScene, LoadSceneMode.Additive);
                var sceneFlow = SceneFlow.GetSceneFlowOfObject(this);
                await sceneFlow.UnloadSelf();

                await UIManager.FadeTransition(FadeSetting.FadeOut());
            }
        }

        private void OnDisable()
        {
            _gameData.OnGameResult.UnsubscribeSource(this);
        }

        // private void Update()
        // {
        //     if (Pointer.current.press.wasPressedThisFrame)
        //     {
        //         var raycast = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Pointer.current.position.ReadValue()), Vector2.zero);
        //         if (raycast.rigidbody != null)
        //         {
        //             Debug.Log($"Pointer pressed over {raycast.rigidbody.name}", raycast.rigidbody);
        //         }
        //     }
        // }

        [Button]
        public void Restart()
        {
            StopAllCoroutines();
            _tileController.StopSpawning();
            _musicPlayer.Stop();
            _gameData.ResetSession();
            StartSession();
        }

        public void StartSession()
        {
            StartCoroutine(RunSessionRoutine());
            IEnumerator RunSessionRoutine()
            {
                var beatmap = _gameData.SessionConfig.Beatmap;
                _musicPlayer.Clip = beatmap.SongInfo.MusicClip;
                yield return _musicPlayer.LoadAudioData().ToCoroutine();
                _tileController.Initialize();

                // Position playhead at the start of the play range before any tap
                _musicPlayer.SetTime(beatmap.StartTime);

                yield return _tileController.WaitForTapStartTile().ToCoroutine();
                _gameData.OnStartGame?.Invoke();
                _startFeedback?.PlayFeedbacks();

                yield return YieldCollection.WaitForSeconds(_prerollSeconds);

                _musicPlayer.FadeIn(beatmap.FadeInDuration).Forget();

                _tileController.StartSpawning();

                var changeAt = new Queue<float>();
                changeAt.Enqueue(Random.Range(0.3f, 0.36f));
                changeAt.Enqueue(Random.Range(0.63f, 0.69f));

                // Wait until we reach EndTime or an external game-over fires
                while (true)
                {
                    if (_gameData.GameResult != null)
                    {
                        break;
                    }

                    var normalizedTime = beatmap.GetNormalizedTime(_musicPlayer.Time);
                    while (changeAt.Count > 0 && normalizedTime >= changeAt.Peek())
                    {
                        _changeBgFeedback.PlayFeedbacks();
                        changeAt.Dequeue();
                    }

                    if (_musicPlayer.Time >= beatmap.EndTime)
                    {
                        yield return _musicPlayer.FadeOut(beatmap.FadeOutDuration, stopOnComplete: false).ToCoroutine();
                        _gameData.RaiseGameOver(new WinResult());
                        break;
                    }

                    yield return null;
                }

            }
        }

    }
}