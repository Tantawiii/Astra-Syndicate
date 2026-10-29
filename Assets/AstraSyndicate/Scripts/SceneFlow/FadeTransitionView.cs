using UnityEngine;
using UnityEngine.UI;

namespace AstraSyndicate.SceneFlow
{
    /// <summary>
    /// Default transition: a full-screen fade with a thin progress bar. Built in code, so it needs no
    /// prefab. Uses unscaled time, so it still animates while the game is paused.
    /// </summary>
    public sealed class FadeTransitionView : MonoBehaviour, ISceneTransitionView
    {
        // Above any gameplay or menu canvas.
        private const int SortingOrder = 30000;

        [SerializeField, Min(0f)] private float fadeDuration = 0.35f;
        [SerializeField] private Color overlayColor = Color.black;
        [SerializeField] private Color progressColor = new Color(0.35f, 0.75f, 1f, 1f);

        private CanvasGroup _group;
        private RectTransform _progressFill;

        public float FadeDuration
        {
            get => fadeDuration;
            set => fadeDuration = Mathf.Max(0f, value);
        }

        public Awaitable ShowAsync()
        {
            return FadeTo(1f);
        }

        public Awaitable HideAsync()
        {
            return FadeTo(0f);
        }

        public void SetProgress(float progress01)
        {
            BuildIfNeeded();
            _progressFill.anchorMax = new Vector2(Mathf.Clamp01(progress01), 1f);
        }

        private void Awake()
        {
            BuildIfNeeded();
        }

        private async Awaitable FadeTo(float targetAlpha)
        {
            BuildIfNeeded();
            _group.blocksRaycasts = true;

            float startAlpha = _group.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }

            _group.alpha = targetAlpha;
            _group.blocksRaycasts = targetAlpha > 0f;
        }

        private void BuildIfNeeded()
        {
            if (_group != null) return;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Lets the overlay swallow clicks on any uGUI menu underneath while it is visible.
            gameObject.AddComponent<GraphicRaycaster>();

            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            RectTransform overlay = CreateRect("Overlay", transform);
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            overlay.gameObject.AddComponent<Image>().color = overlayColor;

            RectTransform track = CreateRect("ProgressTrack", transform);
            track.anchorMin = new Vector2(0.2f, 0.08f);
            track.anchorMax = new Vector2(0.8f, 0.08f);
            track.sizeDelta = new Vector2(0f, 4f);
            track.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

            _progressFill = CreateRect("ProgressFill", track);
            _progressFill.anchorMin = Vector2.zero;
            _progressFill.anchorMax = new Vector2(0f, 1f);
            _progressFill.offsetMin = Vector2.zero;
            _progressFill.offsetMax = Vector2.zero;
            _progressFill.gameObject.AddComponent<Image>().color = progressColor;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }
    }
}
