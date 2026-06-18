using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using GameDevKit;
using GameDevKit.ObjectReferences;
using IngameDebugConsole;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagicTiles
{
    [DefaultExecutionOrder(-10)]
    public class AppManager : SingletonBehaviour<AppManager>
    {
        [SerializeField] private SceneReference _sceneOnRestart;

#if DEV_BUILD || UNITY_EDITOR
        [SerializeField] private DebugLogManager _debugConsolePrefab;
#endif

        public static bool EnableCheat =>
#if ENABLE_CHEAT
        true;
#else
        IsDevBuild;
#endif

        // use this instead of Debug.isDebugBuild to debug on Android release builds
        public static bool IsDevBuild =>
#if DEV_BUILD || UNITY_EDITOR
        true;
#else
        false;
#endif

        public static event Action<bool> OnAppPaused;
        public static event Action OnAppQuit;
        public static event Action OnAppRestart;

        public static bool IsQuitting { get; private set; }

        private bool _isAppPaused;

        private async UniTaskVoid Start()
        {
            IsQuitting = false;

            PrimeTweenConfig.warnZeroDuration = false;
            PrimeTweenConfig.warnEndValueEqualsCurrent = false;
            PrimeTweenConfig.warnTweenOnDisabledTarget = false;

            Debug.unityLogger.logEnabled = IsDevBuild;

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Input.multiTouchEnabled = false;

#if DEV_BUILD || UNITY_EDITOR
            if (_debugConsolePrefab != null)
            {
                var console = Instantiate(_debugConsolePrefab);
                console.gameObject.SetActive(false);
            }
#endif

        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused != _isAppPaused)
            {
                OnAppPaused?.Invoke(isPaused);
            }

            _isAppPaused = isPaused;
        }

        private void OnApplicationFocus(bool isFocused)
        {
            if (isFocused == _isAppPaused)
            {
                OnAppPaused?.Invoke(!isFocused);
            }

            _isAppPaused = !isFocused;
        }

        private void OnApplicationQuit()
        {
            IsQuitting = true;
            OnAppQuit?.Invoke();

            OnAppPaused = null;
            OnAppQuit = null;
            OnAppRestart = null;
        }

        public static void QuitApp()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }

        public static void Restart()
        {
            DebugLogManager.Instance.DestroyGameObject();

            Tween.StopAll();

            OnAppRestart?.Invoke();
            SceneManager.LoadScene(_instance._sceneOnRestart);
        }


#if DEV_BUILD || UNITY_EDITOR
        private int _showDebugConsoleCount = 0;
        public void IncrementShowDebugConsole()
        {
            _showDebugConsoleCount++;
            if (_showDebugConsoleCount >= 3)
            {
                DebugLogManager.Instance.gameObject.SetActive(true);
            }
        }
#endif

    }

}