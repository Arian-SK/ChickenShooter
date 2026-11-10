using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cluck
{
    [RequireComponent(typeof(Canvas))]
    public sealed class MainMenuPanel : MonoBehaviour
    {
        [SerializeField] RectTransform frame;
        [SerializeField] Button play;
        [SerializeField] Button quit;
        [SerializeField] Slider musicVolume;
        [SerializeField] Slider effectsVolume;

        internal RectTransform Frame => frame;
        internal bool HasVolumeControls => musicVolume != null && effectsVolume != null;

        internal void Bind(GameManager state, EventSystem events, GameAudio sound)
        {
            play.onClick.AddListener(state.NewGame);
            quit.onClick.AddListener(Application.Quit);
            if (musicVolume != null)
            {
                musicVolume.SetValueWithoutNotify(sound.MusicVolume);
                musicVolume.onValueChanged.AddListener(sound.SetMusic);
            }
            if (effectsVolume != null)
            {
                effectsVolume.SetValueWithoutNotify(sound.EffectsVolume);
                effectsVolume.onValueChanged.AddListener(sound.SetEffects);
            }
            events.SetSelectedGameObject(play.gameObject);
        }

        internal static MainMenuPanel Create(Transform parent, CampaignDataSO campaign)
        {
            var menu = ArcadeView.CreateCanvas(parent, "Canvas").gameObject.AddComponent<MainMenuPanel>();
            menu.frame = ArcadeView.CreateFrame(menu.transform);
            var ink = new Color(.031f, .053f, .094f, .95f);
            var cyan = new Color(.40f, .83f, .94f);
            var muted = new Color(.71f, .79f, .88f);
            var panel = Image(menu.frame, "Menu", new Rect(76, 91, 638, 538), ink).rectTransform;
            Image(panel, "Accent", new Rect(0, 0, 638, 3), cyan);
            var icon = Image(panel, "Ship", new Rect(495, 37, 108, 88), Color.white);
            icon.sprite = campaign.ship;
            icon.preserveAspect = true;
            Label(panel, "Title", "Chicken Shooter", new Rect(33, 52, 461, 77), 52, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            Label(panel, "Tip", "Dodge the eggs. Break the rocks. Grab the gifts.",
                new Rect(36, 129, 560, 34), 21, FontStyle.Normal, muted, TextAnchor.MiddleLeft);
            menu.play = MenuButton(panel, "Play", new Rect(36, 193, 305, 59), cyan, new Color(.035f, .09f, .13f));
            menu.quit = MenuButton(panel, "Quit", new Rect(356, 193, 151, 59), new Color(.14f, .23f, .34f), Color.white);
            Label(panel, "Controls", "WASD / Arrows   Move       Space   Shoot\nX   Bomb                         Esc   Pause",
                new Rect(37, 274, 562, 66), 18, FontStyle.Normal, Color.white, TextAnchor.MiddleLeft);
            Image(panel, "Divider", new Rect(36, 358, 566, 1), new Color(.23f, .34f, .48f));
            Label(panel, "Music label", "Music", new Rect(36, 381, 115, 36), 19, FontStyle.Normal, muted, TextAnchor.MiddleLeft);
            Label(panel, "Effects label", "Sound effects", new Rect(36, 443, 135, 36), 19, FontStyle.Normal, muted, TextAnchor.MiddleLeft);
            menu.musicVolume = VolumeSlider(panel, "Music volume", new Rect(178, 383, 350, 32), cyan);
            menu.effectsVolume = VolumeSlider(panel, "Effects volume", new Rect(178, 445, 350, 32), cyan);

            var route = Image(menu.frame, "Level list", new Rect(746, 114, 455, 492), ink).rectTransform;
            Label(route, "Heading", "Flight route", new Rect(27, 21, 396, 55), 32, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            for (int i = 0; i < Mathf.Min(campaign.levels.Length, 3); i++)
            {
                var level = campaign.levels[i];
                var card = Image(route, $"Level {level.stage}", new Rect(26, 88 + i * 125, 403, 107),
                    new Color(.075f, .12f, .19f)).rectTransform;
                Image(card, "Level accent", new Rect(0, 0, 3, 107), level.accent);
                var portrait = Image(card, "Chicken", new Rect(23, 20, 66, 66), Color.white);
                portrait.sprite = level.rounds[0].enemyPrefab.GetComponent<SpriteRenderer>().sprite;
                portrait.preserveAspect = true;
                Label(card, "Level name", $"{level.stage}. {level.sector}", new Rect(106, 14, 279, 38),
                    22, FontStyle.Bold, level.accent, TextAnchor.MiddleLeft);
                int rocks = System.Array.FindAll(level.rounds, wave => wave.kind == WaveKind.Asteroids).Length;
                string asteroidWaves = rocks == 1 ? "asteroid wave" : "asteroid waves";
                Label(card, "Round count", $"{level.rounds.Length} rounds  /  {rocks} {asteroidWaves}",
                    new Rect(107, 54, 277, 35), 17, FontStyle.Normal, muted, TextAnchor.MiddleLeft);
            }
            return menu;
        }

        static Image Image(Transform parent, string name, Rect area, Color color)
        {
            var image = ArcadeView.Rect(parent, name, area).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Slider VolumeSlider(Transform parent, string name, Rect area, Color accent)
        {
            var hitArea = Image(parent, name, area, Color.clear);
            hitArea.raycastTarget = true;
            var slider = hitArea.gameObject.AddComponent<Slider>();
            var track = Image(hitArea.transform, "Track", new Rect(8, 12, area.width - 16, 8), new Color(.22f, .32f, .44f));
            var fill = Image(track.transform, "Fill", new Rect(0, 0, area.width - 16, 8), accent);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var handleArea = ArcadeView.Rect(hitArea.transform, "Handle Area", new Rect(8, 0, area.width - 16, area.height));
            var handle = Image(handleArea, "Handle", new Rect(0, 6, 16, 20), Color.white);
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.value = .5f;
            FitSliderHandle(slider);
            return slider;
        }

        static void FitSliderHandle(Slider slider)
        {
            var handle = slider.handleRect;
            // Slider stretches the handle vertically.
            handle.pivot = new Vector2(.5f, .5f);
            handle.sizeDelta = new Vector2(16, 20 - ((RectTransform)handle.parent).rect.height);
            handle.anchoredPosition = Vector2.zero;
        }

        internal void FitVolumeHandles()
        {
            FitSliderHandle(musicVolume);
            FitSliderHandle(effectsVolume);
        }

        static Button MenuButton(Transform parent, string title, Rect area, Color fill, Color textColor)
        {
            var image = Image(parent, title, area, fill);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(.93f, .96f, 1);
            colors.selectedColor = new Color(.83f, .91f, 1);
            colors.pressedColor = new Color(.69f, .77f, .88f);
            colors.fadeDuration = 0;
            button.colors = colors;
            Label(image.transform, "Text", title, new Rect(0, 0, area.width, area.height), 24, FontStyle.Bold, textColor);
            return button;
        }

        static void Label(Transform parent, string name, string value, Rect area, int size, FontStyle style, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var label = ArcadeView.Rect(parent, name, area).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.fontStyle = style;
            label.text = value;
            label.color = color;
            label.alignment = alignment;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
        }
    }
}
