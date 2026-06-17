using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameDevKit.ObjectReferences;
using UnityEngine;
using UnityEngine.SceneManagement;
using MagicTiles.UI;

namespace MagicTiles
{
    public record struct ProgressInfo(float TargetProgress, float EstimatedDuration, string Message);

    [DefaultExecutionOrder(-1000)]
    public abstract class SceneFlow : MonoBehaviour
    {
        [field: SerializeField] public bool SetActiveSceneOnStart { get; private set; } = true;

        protected static readonly List<SceneFlow> _activeSceneFlows = new();

        protected void Start()
        {
            if (SetActiveSceneOnStart) { SetActiveScene(); }
            OnStart();
        }

        protected virtual async UniTaskVoid OnStart() { }

        protected void OnEnable() => _activeSceneFlows.Add(this);
        protected void OnDisable() => _activeSceneFlows.Remove(this);

        public virtual UniTask PrepareScene(Action<ProgressInfo> progressCallback = null) => UniTask.CompletedTask;

        public void SetActiveScene() => SceneManager.SetActiveScene(gameObject.scene);

        public async UniTask UnloadSelf() => await SceneManager.UnloadSceneAsync(gameObject.scene);

        public static T GetSceneFlow<T>() where T : SceneFlow => _activeSceneFlows.Find(f => f is T) as T;

        public static SceneFlow GetSceneFlow(string sceneName) => _activeSceneFlows.Find(f => f.gameObject.scene.name == sceneName);
        public static SceneFlow GetSceneFlowOfObject(Component component) => GetSceneFlowOfObject(component.gameObject);
        public static SceneFlow GetSceneFlowOfObject(GameObject gObj) => _activeSceneFlows.Find(f => f.gameObject.scene == gObj.scene);

        public static async UniTask<SceneFlow> LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Additive, Action<ProgressInfo> progressCallback = null)
        {
            var sceneOp = SceneManager.LoadSceneAsync(sceneName, mode);
            while (!sceneOp.isDone)
            {
                progressCallback?.Invoke(new(sceneOp.progress, 1, "Loading scene..."));
                await UniTask.Yield();
            }
            // SceneManager.GetSceneByName does not work here
            var scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            return FindInScene(scene);
        }

        public readonly struct SceneActivationHandle
        {
            public readonly string SceneName;

            private readonly AsyncOperation _sceneOp;

            public SceneActivationHandle(string sceneName, AsyncOperation sceneOp)
            {
                SceneName = sceneName;
                _sceneOp = sceneOp;
            }

            public async UniTask<SceneFlow> Activate()
            {
                _sceneOp.allowSceneActivation = true;
                await _sceneOp;

                var scene = SceneManager.GetSceneByName(SceneName);
                return FindInScene(scene);
            }
        }

        /// <summary>
        /// Loads a scene without activating it.
        /// The returned AsyncOperation will complete when the scene is loaded and ready to be activated, but the scene will not be activated until allowSceneActivation is set to true.
        /// </summary>
        public static async UniTask<SceneActivationHandle> LoadSceneWithoutActivation(string sceneName, LoadSceneMode mode = LoadSceneMode.Additive, Action<ProgressInfo> progressCallback = null)
        {
            var sceneOp = SceneManager.LoadSceneAsync(sceneName, mode);
            sceneOp.allowSceneActivation = false;
            while (sceneOp.progress < 0.9f)
            {
                progressCallback?.Invoke(new(sceneOp.progress, 1, "Loading scene..."));
                await UniTask.Yield();
            }
            return new SceneActivationHandle(sceneName, sceneOp);
        }

        public static async UniTask UnloadScene(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
                await SceneManager.UnloadSceneAsync(scene);
            }
        }

        public static SceneFlow FindInScene(Scene scene)
        {
            foreach (var obj in scene.GetRootGameObjects())
            {
                if (obj.TryGetComponentInChildren(out SceneFlow sceneFlow))
                {
                    return sceneFlow;
                }
            }
            return null;
        }
    }
}