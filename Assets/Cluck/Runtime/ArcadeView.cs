using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Cluck
{
    internal sealed class ArcadeView
    {
        static readonly Color Ink = new(.025f, .048f, .092f, .98f);
        static readonly Color Haze = new(.63f, .73f, .87f);
        static readonly Color Line = new(.24f, .37f, .54f, .58f);
        static readonly Color White = new(.91f, .96f, 1);
        readonly Font heading, body;
        readonly RectTransform frame;
        readonly EventSystem events;
        readonly Color accent;
        GameObject overlay;
        Text sector, score, stock, power, targets, dispatch, feedback;
        Image[] hull, track;
        readonly Image[] mattes = new Image[4];
        GameObject banner;
        string roundLabel;
        int remaining;

        public ArcadeView(Transform parent, Color accent, Font font, MainMenuPanel menu = null)
        {
            this.accent = accent;
            heading = font;
            body = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = menu == null ? CreateCanvas(parent, "HUD") : menu.GetComponent<Canvas>();
            for (int i = 0; i < mattes.Length; i++)
            {
                mattes[i] = Panel(canvas.transform, "Letterbox", UnityEngine.Rect.zero, new Color(.025f, .048f, .092f));
                mattes[i].transform.SetAsFirstSibling();
            }
            frame = menu == null ? CreateFrame(canvas.transform) : menu.Frame;
            var input = new GameObject("Input System UI", typeof(EventSystem), typeof(InputSystemUIInputModule));
            input.transform.SetParent(parent);
            events = input.GetComponent<EventSystem>();
        }

        internal static Canvas CreateCanvas(Transform parent, string name)
        {
            var canvas = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(parent, false);
            var component = canvas.GetComponent<Canvas>();
            component.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            return component;
        }

        internal static RectTransform CreateFrame(Transform canvas)
        {
            var frame = Rect(canvas, "16:9 safe frame", new Rect(0, 0, 1280, 720));
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f, .5f);
            frame.anchoredPosition = Vector2.zero;
            return frame;
        }

        public void FitViewport(Rect viewport)
        {
            Matte(0, Vector2.zero, new Vector2(viewport.xMin, 1));
            Matte(1, new Vector2(viewport.xMax, 0), Vector2.one);
            Matte(2, new Vector2(viewport.xMin, 0), new Vector2(viewport.xMax, viewport.yMin));
            Matte(3, new Vector2(viewport.xMin, viewport.yMax), new Vector2(viewport.xMax, 1));
        }

        void Matte(int index, Vector2 min, Vector2 max)
        {
            var rect = mattes[index].rectTransform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            mattes[index].enabled = max.x > min.x && max.y > min.y;
        }

        internal static RectTransform Rect(Transform root, string name, Rect bounds)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(root, false);
            r.anchorMin = r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(bounds.x, -bounds.y);
            r.sizeDelta = bounds.size;
            return r;
        }

        Image Panel(Transform root, string label, Rect bounds, Color fill)
        {
            var img = Rect(root, label, bounds).gameObject.AddComponent<Image>();
            img.color = fill;
            img.raycastTarget = false;
            return img;
        }

        Text Label(Transform root, string value, Rect bounds, int size, Color color, bool title = false, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var text = Rect(root, value, bounds).gameObject.AddComponent<Text>();
            text.font = title && heading != null ? heading : body;
            text.text = value;
            text.color = color;
            text.fontSize = size;
            text.alignment = alignment;
            if (title) text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        Button Action(Transform root, string text, Rect bounds, bool primary, UnityEngine.Events.UnityAction click)
        {
            var img = Panel(root, text, bounds, Color.white);
            img.raycastTarget = true;
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor = primary ? accent : Ink;
            colors.highlightedColor = primary ? Color.Lerp(accent, White, .24f) : new Color(.10f, .18f, .28f);
            colors.selectedColor = primary ? Color.Lerp(accent, White, .48f) : new Color(.17f, .28f, .40f);
            colors.pressedColor = primary ? Color.Lerp(accent, Ink, .16f) : new Color(.08f, .16f, .24f);
            colors.fadeDuration = 0;
            button.colors = colors;
            var border = primary ? new Color(1, 1, 1, .44f) : Line;
            Panel(img.transform, "Rule", new Rect(0, 0, bounds.width, 2), border);
            if (bounds.width < 90)
            {
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                Label(img.transform, text, new Rect(0, 0, bounds.width, bounds.height), 19, accent, true, TextAnchor.MiddleCenter);
            }
            else
            {
                Label(img.transform, text, new Rect(16, 0, bounds.width - 54, bounds.height), 17, primary ? Ink : White, true);
                Label(img.transform, ">", new Rect(bounds.width - 34, 0, 20, bounds.height), 19, primary ? Ink : accent, false, TextAnchor.MiddleCenter);
            }
            button.onClick.AddListener(click);
            return button;
        }

        public void BindMenu(MainMenuPanel menu, GameManager state, GameAudio sound) => menu.Bind(state, events, sound);

        public void Hud(LevelDataSO level, CampaignDataSO data)
        {
            Panel(frame, "Status plate", new Rect(0, 0, 1280, 60), Ink);
            Panel(frame, "Top accent", new Rect(0, 59, 1280, 1), accent);
            Panel(frame, "Stage pip", new Rect(20, 15, 3, 30), accent);
            sector = Label(frame, "", new Rect(31, 3, 385, 35), 19, White, true);
            track = new Image[level.rounds.Length];
            for (int i = 0; i < track.Length; i++) track[i] = Panel(frame, "Round marker", new Rect(32 + i * 15, 43, 11, 4), Line);
            targets = Label(frame, "", new Rect(200, 35, 235, 20), 15, Haze);
            Label(frame, "SCORE", new Rect(448, 8, 91, 39), 14, Haze);
            score = Label(frame, "", new Rect(506, 5, 180, 48), 21, White, true);
            Label(frame, "HULL", new Rect(663, 8, 55, 39), 14, Haze);
            hull = new Image[GameManager.StartingLives];
            for (int i = 0; i < hull.Length; i++)
            {
                hull[i] = Panel(frame, "Hull pip", new Rect(712 + i * 28, 17, 23, 23), accent);
                hull[i].sprite = data.ship;
                hull[i].preserveAspect = true;
            }
            stock = Label(frame, "", new Rect(836, 5, 169, 49), 17, accent, true);
            power = Label(frame, "", new Rect(1005, 5, 200, 49), 17, White, true);
            Action(frame, "II", new Rect(1210, 10, 50, 39), false, () => PauseClicked?.Invoke());
            Panel(frame, "Footer", new Rect(0, 695, 1280, 25), Ink);
            Panel(frame, "Bottom accent", new Rect(0, 695, 1280, 1), Line);
            Label(frame, "FLIGHT CONTROL  //  WASD / ARROWS", new Rect(22, 696, 410, 24), 14, Haze);
            Label(frame, "SPACE  FIRE     /     X  SCREEN PULSE     /     ESC  PAUSE", new Rect(757, 696, 500, 24), 14, Haze, false, TextAnchor.MiddleRight);
            feedback = Label(frame, "", new Rect(430, 696, 310, 24), 14, accent, false, TextAnchor.MiddleCenter);
            banner = Panel(frame, "Dispatch plate", new Rect(340, 86, 600, 66), Ink).gameObject;
            Panel(banner.transform, "Signal strip", new Rect(0, 0, 600, 2), accent);
            dispatch = Label(banner.transform, "", new Rect(10, 3, 580, 60), 24, White, true, TextAnchor.MiddleCenter);
            banner.SetActive(false);
        }

        public event System.Action PauseClicked;
        public void Status(GameManager state, LevelDataSO level)
        {
            sector.text = level.stage.ToString("00") + "   " + level.sector;
            score.text = state.Score.ToString("D6");
            stock.text = "PULSE  " + state.Bombs.ToString("00");
            power.text = "POWER  " + state.Power + " / " + state.Campaign.weaponTiers.Length;
            for (int i = 0; i < hull.Length; i++) hull[i].color = i < state.Lives ? White : new Color(.45f, .52f, .67f, .28f);
            for (int i = 0; i < track.Length; i++) track[i].color = i < state.Round ? accent : Line;
            roundLabel = $"ROUND {state.Round:00}/{track.Length:00}";
            Targets(remaining);
        }

        public void Targets(int count)
        {
            remaining = count;
            targets.text = count > 0 ? roundLabel + "   /   " + count + " CONTACTS" : roundLabel;
        }
        public void Dispatch(string message)
        {
            dispatch.text = message;
            banner.SetActive(message.Length > 0);
        }

        public void Feedback(string message) => feedback.text = message;

        public void Modal(string title, string subtitle, string primary, UnityEngine.Events.UnityAction proceed, UnityEngine.Events.UnityAction menu)
        {
            CloseModal();
            var dim = Panel(frame, "Modal backdrop", new Rect(0, 0, 1280, 720), new Color(.01f, .025f, .06f, .78f));
            overlay = dim.gameObject;
            dim.raycastTarget = true;
            var card = Panel(overlay.transform, "Panel", new Rect(354, 212, 572, 291), Ink).transform;
            Panel(card, "Panel edge", new Rect(0, 0, 572, 3), accent);
            Label(card, title, new Rect(34, 28, 504, 65), 40, White, true);
            Label(card, subtitle, new Rect(36, 104, 504, 57), 19, Haze);
            var primaryButton = Action(card, primary, new Rect(36, 196, 235, 55), true, proceed);
            Action(card, "MAIN MENU", new Rect(290, 196, 246, 55), false, menu);
            events.SetSelectedGameObject(primaryButton.gameObject);
        }

        public void CloseModal()
        {
            if (overlay == null) return;
            events.SetSelectedGameObject(null);
            overlay.SetActive(false);
            Object.Destroy(overlay);
            overlay = null;
        }
    }
}
