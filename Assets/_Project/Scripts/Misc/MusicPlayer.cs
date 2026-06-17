using System;
using Cysharp.Threading.Tasks;
using GameDevKit;
using PrimeTween;
using UnityEngine;

namespace MagicTiles
{
    /// <summary>
    /// Handles music playback with common controls.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;

        private Tween _volumeTween;

        private bool IsClipReady => _audioSource.clip != null && _audioSource.clip.loadState == AudioDataLoadState.Loaded;

        public float Time => IsClipReady ? _audioSource.time : 0f;

        public float NormalizedTime => IsClipReady && _audioSource.clip.length > 0f ? _audioSource.time / _audioSource.clip.length : 0f;

        public AudioClip Clip
        {
            get => _audioSource.clip;
            set => _audioSource.clip = value;
        }

        public bool IsPlaying => _audioSource.isPlaying;

        /// <summary>
        /// Playback speed multiplier.
        /// 1.0 = normal speed.
        /// </summary>
        public float Pitch
        {
            get => _audioSource.pitch;
            set => _audioSource.pitch = value;
        }

        private void OnDestroy() => _volumeTween.Stop();

        /// <summary>
        /// Asynchronously load audio data for the assigned clip. Must be called before playback to ensure the clip is ready, especially on WebGL.
        /// </summary>
        public async UniTask LoadAudioData()
        {
            if (_audioSource.clip == null)
            {
                Debug.LogWarning($"Clip is null, cannot load audio data!", this);
                return;
            }

            var clip = _audioSource.clip;

            if (clip.loadState == AudioDataLoadState.Loaded)
            {
                return;
            }

            clip.LoadAudioData();

            while (true)
            {
                switch (clip.loadState)
                {
                    case AudioDataLoadState.Loaded:
                        {
                            return;
                        }

                    case AudioDataLoadState.Failed:
                        {
                            throw new Exception($"Failed to load audio data for clip [{clip.name}]");
                        }
                }

                await UniTask.Yield(destroyCancellationToken);
            }
        }

        /// <summary>
        /// Set playback position (seconds) without starting playback.
        /// </summary>
        public void SetTime(float seconds) => SeekToSeconds(seconds);

        public void UnPause() => _audioSource.UnPause();

        /// <summary>
        /// Play the audio. If delay > 0 schedules playback after that many seconds.
        /// </summary>
        public void Play(float delay = 0f)
        {
            if (delay > 0f)
            {
                _audioSource.PlayDelayed(delay);
            }
            else
            {
                _audioSource.Play();
            }
        }

        /// <summary>
        /// Set playback position and play immediately.
        /// </summary>
        public void PlayAt(TimeSpan position)
        {
            if (!ValidateClipReady()) { return; }

            SeekToSeconds((float)position.TotalSeconds);
            Play();
        }

        /// <summary>
        /// Set playback position using normalized time [0..1] and play immediately.
        /// </summary>
        public void PlayAtNormalized(float normalized)
        {
            if (!ValidateClipReady()) { return; }

            SeekToSeconds(Mathf.Clamp01(normalized) * _audioSource.clip.length);
            Play();
        }

        public void Pause() => _audioSource.Pause();

        public void Stop() => _audioSource.Stop();

        /// <summary>
        /// Fade audio volume from current volume to target volume.
        /// </summary>
        public async UniTask FadeIn(float duration = 1f, float targetVolume = 1f)
        {
            if (!ValidateClipReady()) { return; }

            _volumeTween.Stop();

            if (!_audioSource.isPlaying)
            {
                _audioSource.volume = 0f;
                _audioSource.Play();
            }

            _volumeTween = Tween.Custom(
                _audioSource.volume,
                targetVolume,
                duration,
                value => _audioSource.volume = value,
                Ease.Linear
            );
            await _volumeTween;
        }

        /// <summary>
        /// Fade audio volume down to target volume.
        /// </summary>
        public async UniTask FadeOut(float duration = 1f, float targetVolume = 0f, bool stopOnComplete = true)
        {
            if (!_audioSource.isPlaying) { return; }

            _volumeTween.Stop();

            _volumeTween = Tween.Custom(
                _audioSource.volume,
                targetVolume,
                duration,
                value => _audioSource.volume = value,
                Ease.Linear
            );
            await _volumeTween;

            if (stopOnComplete && Mathf.Approximately(targetVolume, 0f))
            {
                _audioSource.Stop();
            }
        }

        /// <summary>
        /// Play a clip segment defined by a normalized range [0..1].
        /// Completes when playback reaches the end of the range.
        /// </summary>
        public async UniTask PlayRange(FloatRange range)
        {
            if (!ValidateClipReady())
            {
                return;
            }

            var startTime = Mathf.Clamp01(range.min) * _audioSource.clip.length;
            var endTime = Mathf.Clamp01(range.max) * _audioSource.clip.length;

            if (endTime <= startTime)
            {
                _audioSource.time = startTime;
                Play();
                return;
            }

            _audioSource.time = startTime;
            Play();

            while (true)
            {
                if (_audioSource.clip == null)
                {
                    break;
                }

                if (!_audioSource.isPlaying)
                {
                    await UniTask.Yield(destroyCancellationToken);
                    continue;
                }

                if (_audioSource.time >= endTime)
                {
                    break;
                }

                await UniTask.Yield(destroyCancellationToken);
            }

            _audioSource.Stop();
        }

        private void SeekToSeconds(float seconds)
        {
            if (!ValidateClipReady())
            {
                return;
            }

            _audioSource.time = Mathf.Clamp(seconds, 0f, _audioSource.clip.length);
        }

        private bool ValidateClipReady()
        {
            if (_audioSource == null)
            {
                Debug.LogWarning("AudioSource is not assigned!", this);
                return false;
            }

            if (_audioSource.clip == null)
            {
                Debug.LogWarning("AudioClip is not assigned!", this);
                return false;
            }

            if (!IsClipReady)
            {
                Debug.LogWarning($"AudioClip [{_audioSource.clip.name}] is not ready!", this);
                return false;
            }

            return true;
        }
    }
}