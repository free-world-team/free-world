using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// One programmatic placeholder Canvas shared by every M7 page and presentation
    /// overlay. Localization keys are resolved through Unity Localization.
    /// </summary>
    public sealed class RuntimeUiRoot : MonoBehaviour, IGameFlowView
    {
        private readonly StringBuilder builder = new StringBuilder(256);
        private Canvas canvas;
        private TMP_Text pageText;
        private TMP_FontAsset runtimeFont;
        private ILocalizationService localization;

        public Canvas SharedCanvas => canvas;
        public UiPageId CurrentPage { get; private set; }
        public int RenderedOptionCount { get; private set; }
        public int RenderedSelectedIndex { get; private set; }

        public string RenderedText => pageText == null ? string.Empty : pageText.text;

        public void Initialize(ILocalizationService localizationService = null)
        {
            if (canvas != null) return;
            localization = localizationService;
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();

            var panelObject = new GameObject("M7PlaceholderPanel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(transform, false);
            var rect = (RectTransform)panelObject.transform;
            rect.anchorMin = new Vector2(0.05f, 0.05f);
            rect.anchorMax = new Vector2(0.48f, 0.55f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panelObject.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.1f, 0.88f);

            var textObject = new GameObject("LocalizedKeyPreview", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panelObject.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 24f);
            textRect.offsetMax = new Vector2(-24f, -24f);
            pageText = textObject.GetComponent<TMP_Text>();
            runtimeFont = TMP_Settings.defaultFontAsset;
            if (runtimeFont == null)
                throw new System.InvalidOperationException("The governed TMP default font asset is unavailable.");
            pageText.font = runtimeFont;
            pageText.fontSize = 20;
            pageText.alignment = TextAlignmentOptions.TopLeft;
            pageText.color = Color.white;
            pageText.enableWordWrapping = true;
            pageText.overflowMode = TextOverflowModes.Overflow;
        }

        /// <summary>Reports whether the runtime fallback font can render a localized character.</summary>
        public bool SupportsCharacter(char character)
        {
            if (runtimeFont == null) return false;
            return runtimeFont.HasCharacter(character, true, true);
        }

        public void Show(UiPageViewModel model)
        {
            if (canvas == null) Initialize();
            CurrentPage = model.Page;
            RenderedOptionCount = model.OptionCount;
            RenderedSelectedIndex = model.SelectedIndex;
            builder.Clear();
            builder.Append(Resolve(model.TitleKey));
            for (var index = 0; index < model.OptionCount; index++)
            {
                builder.Append('\n');
                builder.Append(index == model.SelectedIndex ? "> " : "  ");
                builder.Append(Resolve(model.GetOptionKey(index)));
            }

            pageText.text = builder.ToString();
        }

        private string Resolve(string key) => localization == null ? key : localization.Resolve(key);

        private void OnDestroy()
        {
            runtimeFont = null;
        }
    }
}
