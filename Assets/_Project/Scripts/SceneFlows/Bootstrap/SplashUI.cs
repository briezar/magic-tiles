using Cysharp.Threading.Tasks;
using GameDevKit.UI;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MagicTiles.UI
{
    public class SplashUI : ScreenUI
    {
        [SerializeField] private Slider _loadingBar;
        [SerializeField] private TMP_Text _progressText, _infoText;

        private Tween _runProgressTween;

        private void Awake()
        {
            SetProgress(0);
        }

        public void SetInfo(string text = null)
        {
            // _infoText.text = text;
        }

        public void SetProgress(float normalizedValue)
        {
            normalizedValue = Mathf.Clamp01(normalizedValue);
            _loadingBar.value = normalizedValue;
            _progressText.text = $"{normalizedValue:P2}";
        }

        public async UniTask RunProgress(float from, float to, float duration)
        {
            _runProgressTween.Stop();
            _runProgressTween = Tween.Custom(from, to, duration, value => SetProgress(value), Ease.Linear);
            await _runProgressTween;
        }

        public async UniTask RunProgress(float to, float duration)
        {
            var from = _loadingBar.value;
            await RunProgress(from, to, duration);
        }
    }
}
