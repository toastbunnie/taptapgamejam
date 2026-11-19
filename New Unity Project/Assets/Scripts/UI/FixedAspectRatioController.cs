using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Keeps the game camera and screen UI inside a centered 16:9 viewport.
    /// Any unused display area remains black.
    /// </summary>
    public class FixedAspectRatioController : MonoBehaviour
    {
        private const float TargetAspect = 16f / 9f;
        private const int ScreenCanvasSortingOrder = 5000;
        private static readonly Vector2 ReferenceResolution =
            new Vector2(1920f, 1080f);
        private static FixedAspectRatioController _instance;

        private Camera _gameCamera;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private Coroutine _sceneRefreshRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null)
                return;

            GameObject controllerObject =
                new GameObject(nameof(FixedAspectRatioController));
            DontDestroyOnLoad(controllerObject);
            controllerObject.AddComponent<FixedAspectRatioController>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void Start()
        {
            RefreshDisplayLayout();
        }

        private void LateUpdate()
        {
            Camera activeCamera = FindActiveSceneCamera();
            bool cameraChanged = activeCamera != _gameCamera;
            bool screenChanged =
                Screen.width != _lastScreenWidth ||
                Screen.height != _lastScreenHeight;

            if (cameraChanged || screenChanged)
                RefreshDisplayLayout();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _gameCamera = null;
            if (_sceneRefreshRoutine != null)
                StopCoroutine(_sceneRefreshRoutine);
            _sceneRefreshRoutine = StartCoroutine(RefreshAfterSceneLoad());
        }

        private IEnumerator RefreshAfterSceneLoad()
        {
            // Allow the newly loaded scene's cameras and canvases to finish Awake.
            yield return null;
            RefreshDisplayLayout();
            _sceneRefreshRoutine = null;
        }

        private void RefreshDisplayLayout()
        {
            _gameCamera = FindActiveSceneCamera();
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            if (_gameCamera == null || _lastScreenHeight <= 0)
                return;

            float screenAspect = (float)_lastScreenWidth / _lastScreenHeight;
            Rect viewport = new Rect(0f, 0f, 1f, 1f);

            if (screenAspect > TargetAspect)
            {
                viewport.width = TargetAspect / screenAspect;
                viewport.x = (1f - viewport.width) * 0.5f;
            }
            else if (screenAspect < TargetAspect)
            {
                viewport.height = screenAspect / TargetAspect;
                viewport.y = (1f - viewport.height) * 0.5f;
            }

            _gameCamera.rect = viewport;
            _gameCamera.aspect = TargetAspect;
            _gameCamera.clearFlags = CameraClearFlags.SolidColor;
            _gameCamera.backgroundColor = Color.black;

            BindScreenCanvases();
        }

        private Camera FindActiveSceneCamera()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            Camera taggedCamera = Camera.main;
            if (taggedCamera != null &&
                taggedCamera.gameObject.scene == activeScene)
                return taggedCamera;

            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (Camera candidate in cameras)
            {
                if (candidate != null &&
                    candidate.isActiveAndEnabled &&
                    candidate.gameObject.scene == activeScene &&
                    candidate.targetDisplay == 0)
                    return candidate;
            }

            return null;
        }

        private void BindScreenCanvases()
        {
            if (_gameCamera == null)
                return;

            List<Canvas> canvases = new List<Canvas>();
            GameObject[] sceneRoots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject root in sceneRoots)
                canvases.AddRange(root.GetComponentsInChildren<Canvas>(true));

            foreach (Canvas canvas in canvases)
            {
                if (canvas == null)
                    continue;
                if (canvas.renderMode == RenderMode.WorldSpace)
                    continue;

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _gameCamera;
                canvas.overrideSorting = true;
                canvas.sortingOrder = ScreenCanvasSortingOrder;
                canvas.planeDistance = Mathf.Clamp(
                    1f,
                    _gameCamera.nearClipPlane + 0.01f,
                    _gameCamera.farClipPlane - 0.01f);

                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null)
                    continue;

                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = ReferenceResolution;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f;
            }
        }
    }
}
