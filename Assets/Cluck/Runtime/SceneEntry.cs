using UnityEngine;
using UnityEngine.InputSystem;

namespace Cluck
{
    public sealed class SceneEntry : MonoBehaviour
    {
        [SerializeField] CampaignDataSO campaign;
        [SerializeField] LevelDataSO level;
        [SerializeField] Font displayFont;
        [SerializeField] SpriteRenderer background;
        [SerializeField] MainMenuPanel mainMenu;
        enum Flow { Briefing, Combat, StageClear, Defeated, Complete }
        GameManager state;
        Arena arena;
        ArcadeView view;
        GameAudio sound;
        Backdrop backdrop;
        Camera flightCamera;
        InputActionAsset controls;
        InputAction move, fire, bomb, pause;
        bool paused, bombQueued, fireQueued;
        float timer, starTime, feedbackTime;
        int viewportWidth, viewportHeight;
        Flow flow;

        public void Configure(CampaignDataSO data, LevelDataSO sector, Font font)
        {
            campaign = data; level = sector; displayFont = font;
        }

        internal void ConfigurePresentation(SpriteRenderer sceneBackground, MainMenuPanel menu)
        {
            background = sceneBackground;
            mainMenu = menu;
        }

        void Start()
        {
            state = GameManager.Get(campaign);
            var theme = level != null ? level : campaign.levels[0];
            flightCamera = GetComponentInChildren<Camera>();
            if (FindAnyObjectByType<AudioListener>() == null) flightCamera.gameObject.AddComponent<AudioListener>();
            sound = new GameAudio(transform, level != null ? level.music : campaign.menuMusic);
            backdrop = new Backdrop(transform, theme, campaign.spark.GetComponent<SpriteRenderer>().sprite, campaign.spriteMaterial, background);
            if (level == null && mainMenu == null) mainMenu = MainMenuPanel.Create(transform, campaign);
            view = new ArcadeView(transform, theme.accent, displayFont, mainMenu);
            FitCamera();
            if (level == null) { view.BindMenu(mainMenu, state, sound); return; }
            state.PrepareStage(level.stage);
            controls = Instantiate(campaign.controls);
            var map = controls.FindActionMap("Flight", true);
            move = map.FindAction("Move", true);
            fire = map.FindAction("Fire", true);
            bomb = map.FindAction("Bomb", true);
            pause = map.FindAction("Pause", true);
            fire.performed += QueueFire;
            bomb.performed += QueueBomb;
            pause.performed += PauseAction;
            map.Enable();

            arena = new GameObject("Combat simulation").AddComponent<Arena>();
            arena.transform.SetParent(transform);
            arena.Setup(state, level, sound);
            view.Hud(level, campaign);
            view.PauseClicked += TogglePause;
            state.Changed += Refresh;
            state.Damaged += HullDamaged;
            arena.GiftCollected += GiftCollected;
            arena.TargetsChanged += view.Targets;
            Briefing();
        }

        void FitCamera()
        {
            int width = Mathf.Max(1, Screen.width), height = Mathf.Max(1, Screen.height);
            if (width == viewportWidth && height == viewportHeight) return;
            viewportWidth = width;
            viewportHeight = height;
            float ratio = (float)width / height;
            float desired = Arena.HalfWidth / Arena.HalfHeight;
            if (ratio > desired)
            {
                float w = desired / ratio;
                flightCamera.rect = new Rect((1 - w) * .5f, 0, w, 1);
            }
            else
            {
                float h = ratio / desired;
                flightCamera.rect = new Rect(0, (1 - h) * .5f, 1, h);
            }
            flightCamera.aspect = desired;
            view.FitViewport(flightCamera.rect);
        }

        void QueueFire(InputAction.CallbackContext _) { if (!paused && flow == Flow.Combat) fireQueued = true; }
        void QueueBomb(InputAction.CallbackContext _) { if (!paused && flow == Flow.Combat) bombQueued = true; }
        void PauseAction(InputAction.CallbackContext _) => TogglePause();

        void Update()
        {
            FitCamera();
            if (level == null) { backdrop.Step(starTime += Time.deltaTime); return; }
            if (paused) return;
            backdrop.Step(starTime += Time.deltaTime);
            var shake = arena.Shake;
            flightCamera.transform.localPosition = new Vector3(
                Mathf.Sin(Time.time * 89) * shake * .18f, Mathf.Sin(Time.time * 113) * shake * .12f, -10);
        }

        void FixedUpdate()
        {
            if (arena == null || paused || flow is Flow.Defeated or Flow.Complete) return;
            float dt = Time.fixedDeltaTime;
            if (feedbackTime > 0 && (feedbackTime -= dt) <= 0) view.Feedback("");
            if (bombQueued) TryPulse();
            arena.Step(move.ReadValue<Vector2>(), fire.IsPressed(), fireQueued, dt);
            bombQueued = fireQueued = false;
            if (state.Lives == 0)
            {
                flow = Flow.Defeated;
                arena.Lose();
                view.Dispatch("");
                view.Modal("SIGNAL LOST", "Hull lost in sector " + level.stage + ". Regroup at its first round.", "RETRY SECTOR", state.Retry, state.OpenMenu);
                return;
            }

            switch (flow)
            {
                case Flow.Briefing:
                    if ((timer -= dt) > 0) break;
                    arena.Begin(level.rounds[state.Round - 1]);
                    view.Dispatch("");
                    flow = Flow.Combat;
                    break;
                case Flow.Combat:
                    if (!arena.Finished) break;
                    arena.EndWave();
                    sound.Play(campaign.roundClearSound, .72f);
                    state.AddScore(100 + state.Round * 15);
                    if (state.Round < level.rounds.Length)
                    {
                        state.BeginRound(state.Round + 1);
                        Briefing();
                    }
                    else
                    {
                        flow = Flow.StageClear;
                        timer = 3.7f;
                        view.Dispatch("SECTOR " + level.stage.ToString("00") + " SECURED");
                    }
                    break;
                case Flow.StageClear:
                    if ((timer -= dt) > 0) break;
                    if (level.stage < campaign.levels.Length)
                    {
                        flow = Flow.Complete;
                        state.AdvanceStage();
                    }
                    else
                    {
                        flow = Flow.Complete;
                        view.Dispatch("");
                        view.Modal("ORBIT SECURED", "All sectors clear. Final score: " + state.Score.ToString("N0"), "FLY AGAIN", state.NewGame, state.OpenMenu);
                    }
                    break;
            }
        }

        void Briefing()
        {
            flow = Flow.Briefing;
            timer = state.Round == 1 ? 1.65f : 1.45f;
            Refresh();
            view.Dispatch("ROUND " + state.Round.ToString("00") + "  /  " + level.rounds[state.Round - 1].title);
        }

        void Refresh() => view.Status(state, level);

        void TryPulse()
        {
            var result = arena.Bomb();
            ShowFeedback(result switch
            {
                Arena.PulseResult.Fired => "SCREEN PULSE DEPLOYED",
                Arena.PulseResult.Empty => "NO PULSES LEFT",
                _ => "NO TARGETS FOR PULSE"
            });
        }

        void HullDamaged() => ShowFeedback("HULL HIT");
        void GiftCollected(bool upgraded) => ShowFeedback(upgraded ? "WEAPON UPGRADED" : "+250  BONUS SCORE");
        void ShowFeedback(string message)
        {
            view.Feedback(message);
            feedbackTime = 1.4f;
        }

        void TogglePause()
        {
            if (arena == null || flow is Flow.Defeated or Flow.Complete) return;
            paused = !paused;
            arena.Paused = paused;
            sound.SetPaused(paused);
            bombQueued = fireQueued = false;
            arena.CancelBufferedFire();
            if (paused)
                view.Modal("ON HOLD", "Flight systems paused.", "RESUME FLIGHT", TogglePause, state.OpenMenu);
            else view.CloseModal();
        }

        void OnDestroy()
        {
            if (state != null && level != null)
            {
                state.Changed -= Refresh;
                state.Damaged -= HullDamaged;
            }
            if (controls == null) return;
            fire.performed -= QueueFire;
            bomb.performed -= QueueBomb;
            pause.performed -= PauseAction;
            controls.Disable();
            Destroy(controls);
        }
    }
}
