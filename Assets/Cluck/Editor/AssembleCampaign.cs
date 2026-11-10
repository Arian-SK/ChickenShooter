using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Cluck.Editor
{
    public static class AssembleCampaign
    {
        const string Base = "Assets/Cluck/";
        const string Data = Base + "Data/";
        const string Art = Base + "Art/";
        const string Audio = Base + "Audio/";
        const string Scenes = "Assets/Scenes/";
        const string Unlit = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        [MenuItem("Cluck Defender/Difficulty/Level 1")]
        static void SelectLevelOne() => SelectLevel(1);
        [MenuItem("Cluck Defender/Difficulty/Level 2")]
        static void SelectLevelTwo() => SelectLevel(2);
        [MenuItem("Cluck Defender/Difficulty/Level 3")]
        static void SelectLevelThree() => SelectLevel(3);

        static void SelectLevel(int stage)
        {
            var asset = AssetDatabase.LoadAssetAtPath<LevelDataSO>(Data + $"Level{stage}.asset");
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        public static void UpdatePresentationScenes()
        {
            var campaign = AssetDatabase.LoadAssetAtPath<CampaignDataSO>(Data + "Campaign.asset");
            WireAsteroidSprites();
            ConfigureAsteroidWaves(campaign);
            WireAudio(campaign);
            UpdateScene(campaign.menuScene, campaign, null);
            foreach (var level in campaign.levels) UpdateScene(level.sceneName, campaign, level);
            EditorSceneManager.OpenScene(Scenes + campaign.menuScene + ".unity", OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
        }

        static void UpdateScene(string name, CampaignDataSO campaign, LevelDataSO level)
        {
            var scene = EditorSceneManager.OpenScene(Scenes + name + ".unity", OpenSceneMode.Single);
            var entry = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SceneEntry>(true)).Single();
            AddSceneObjects(entry, campaign, level);
            EditorSceneManager.SaveScene(scene);
        }

        static void AddSceneObjects(SceneEntry entry, CampaignDataSO campaign, LevelDataSO level)
        {
            entry.name = level == null ? "Main Menu Controller" : "Level Controller";
            var camera = entry.GetComponentInChildren<Camera>();
            camera.name = "Main Camera";
            if (!camera.TryGetComponent<AudioListener>(out _)) camera.gameObject.AddComponent<AudioListener>();
            var theme = level != null ? level : campaign.levels[0];
            var background = entry.transform.Find("Background")?.GetComponent<SpriteRenderer>();
            if (background == null) background = Backdrop.CreateBackground(entry.transform, theme.backdrop, campaign.spriteMaterial);
            var menu = entry.GetComponentInChildren<MainMenuPanel>();
            if (level == null && (menu == null || !menu.HasVolumeControls))
            {
                if (menu != null) Object.DestroyImmediate(menu.gameObject);
                menu = MainMenuPanel.Create(entry.transform, campaign);
            }
            if (menu != null) menu.FitVolumeHandles();
            entry.ConfigurePresentation(background, menu);
            EditorUtility.SetDirty(entry);
        }

        static void ConfigureAsteroidWaves(CampaignDataSO campaign)
        {
            var asteroid = AssetDatabase.LoadAssetAtPath<PoolBody>(Data + "Prefabs/Asteroid.prefab");
            foreach (var level in campaign.levels)
            {
                if (level.stage is 2 or 3)
                {
                    var extra = level.rounds[4];
                    extra.kind = WaveKind.Asteroids;
                    extra.enemyPrefab = asteroid;
                    extra.title = level.stage == 2 ? "ROCK SHOWER" : "SHATTERED BELT";
                    extra.count = level.stage == 2 ? 14 : 18;
                    extra.travelSpeed = 3.7f + level.stage * .63f;
                    extra.entryDelay = .52f - level.stage * .035f;
                }
                foreach (var round in level.rounds.Where(round => round.kind == WaveKind.Asteroids))
                {
                    round.health = level.stage;
                    EditorUtility.SetDirty(round);
                }
            }
        }

        static void WireAsteroidSprites()
        {
            var cracked = Sprite("asteroid_cracked", 91);
            var split = Sprite("asteroid_split", 91);
            string path = Data + "Prefabs/Asteroid.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                prefab.GetComponent<PoolBody>().SetDamageSprites(cracked, split, null);
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        static void WireAudio(CampaignDataSO campaign)
        {
            campaign.menuMusic = Clip("Menu");
            campaign.laserSound = Clip("Laser");
            campaign.eggSound = Clip("Egg");
            campaign.impactSound = Clip("Impact");
            campaign.asteroidBreakSound = Clip("Asteroid Break");
            campaign.chickenDownSound = Clip("Chicken Down");
            campaign.damageSound = Clip("Player Hit");
            campaign.giftSound = Clip("Gift");
            campaign.bombSound = Clip("Bomb");
            campaign.roundClearSound = Clip("Round Clear");
            var soundtrack = new[] { "Earth Orbit", "Jovian Belt", "The Deep" };
            for (int i = 0; i < campaign.levels.Length && i < soundtrack.Length; i++)
            {
                campaign.levels[i].music = Clip(soundtrack[i]);
                EditorUtility.SetDirty(campaign.levels[i]);
            }
            EditorUtility.SetDirty(campaign);
        }

        static AudioClip Clip(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + name + ".wav");
            if (clip == null) throw new System.IO.FileNotFoundException("Missing game audio: " + name);
            return clip;
        }

        static void WireChickenSprites()
        {
            var names = new[] { "Orbit Hen", "Armored Hen", "Void Raider" };
            var pixelsPerUnit = new[] { 119f, 115f, 108f };
            for (int index = 0; index < names.Length; index++)
            {
                string name = "chicken" + (index + 1);
                float scale = pixelsPerUnit[index];
                var mild = Sprite(name + "_mild", scale);
                var hurt = Sprite(name + "_hurt", scale);
                var bad = Sprite(name + "_bad", scale);
                string path = Data + "Prefabs/" + names[index] + ".prefab";
                var prefab = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    prefab.GetComponent<PoolBody>().SetDamageSprites(mild, hurt, bad);
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefab);
                }
            }
        }

        [MenuItem("Cluck Defender/Build Phase 1 for Windows")]
        public static void BuildPhaseOne()
        {
            const string folder = "Builds/Phase1-Windows";
            Directory.CreateDirectory(folder);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = Path.Combine(folder, "Cluck Defender.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CompressWithLz4HC
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("The Phase 1 build failed. See the Unity build log.");
            Debug.Log("Phase 1 Windows build: " + Path.GetFullPath(report.summary.outputPath));
        }

        [MenuItem("Cluck Defender/Rebuild authored campaign")]
        public static void Generate()
        {
            Directory.CreateDirectory(Data + "Waves");
            Directory.CreateDirectory(Data + "Prefabs");
            AssetDatabase.Refresh();
            var ship = Sprite("ship", 101);
            var shield = Sprite("shield", 96);
            var flare = Sprite("flare", 94);
            var ring = Sprite("ring", 128);
            var spark = Prefab("Spark", BodyKind.Spark, Sprite("spark", 85), 10, Vector2.zero);
            var gift = Prefab("Weapon Gift", BodyKind.Gift, Sprite("gift", 78), 7, new Vector2(.56f, .48f));
            var egg = Prefab("Egg", BodyKind.Egg, Sprite("egg", 93), 6, new Vector2(.30f, .49f));
            var laserArt = Sprite("laser", 99);
            var laser = Prefab("Laser", BodyKind.Laser, laserArt, 7, new Vector2(.14f, .46f));
            var rock = Prefab("Asteroid", BodyKind.Rock, Sprite("asteroid", 91), 4, new Vector2(.86f, .80f));
            var birds = new[]
            {
                Prefab("Orbit Hen", BodyKind.Chicken, Sprite("chicken1", 119), 5, new Vector2(.88f, .74f)),
                Prefab("Armored Hen", BodyKind.Chicken, Sprite("chicken2", 115), 5, new Vector2(.91f, .77f)),
                Prefab("Void Raider", BodyKind.Chicken, Sprite("chicken3", 108), 5, new Vector2(.96f, .82f))
            };

            var campaign = DataAsset<CampaignDataSO>(Data + "Campaign.asset");
            campaign.ship = ship;
            campaign.shield = shield;
            campaign.flare = flare;
            campaign.ring = ring;
            campaign.spriteMaterial = SpriteMaterial();
            campaign.egg = egg;
            campaign.gift = gift;
            campaign.spark = spark;
            campaign.controls = Input();
            campaign.weaponTiers = new[]
            {
                Weapon("Needle", laser, laserArt, 6.7f, new ShotLane(0, 0)),
                Weapon("Twin Beam", laser, laserArt, 6.25f, new ShotLane(-.21f, 0), new ShotLane(.21f, 0)),
                Weapon("Trident", laser, laserArt, 6.1f, new ShotLane(-.15f, 12), new ShotLane(0, 0), new ShotLane(.15f, -12))
            };
            campaign.levels = new[]
            {
                Level(1, "EARTH ORBIT", "Low orbit patrol", new Color(.34f, .89f, 1), birds[0], rock,
                    new[] { "FIRST CONTACT", "ORBITAL PATROL", "* DEBRIS BELT", "CROSSFIRE", "LAST LINE" },
                    new[] { 8, 10, 13, 12, 14 }),
                Level(2, "JOVIAN BELT", "Armored squadrons", new Color(1, .68f, .40f), birds[1], rock,
                    new[] { "IRON FEATHERS", "ANVIL FLIGHT", "* BROKEN MOON", "STEEL CHEVRON", "* ROCK SHOWER", "JOVIAN GUARD", "THE BREACH" },
                    new[] { 9, 11, 15, 13, 14, 15, 16 }),
                Level(3, "THE DEEP", "Cosmic raiders", new Color(.77f, .59f, 1), birds[2], rock,
                    new[] { "INTO THE DEEP", "VOID LANCERS", "* GRAVITY WELL", "NEBULA RAID", "* SHATTERED BELT", "ECLIPSE", "* COMET STORM", "THE SWARM", "NIGHTFALL", "FINAL CONTACT" },
                    new[] { 10, 12, 16, 13, 18, 15, 20, 16, 17, 18 })
            };
            WireChickenSprites();
            WireAsteroidSprites();
            WireAudio(campaign);
            EditorUtility.SetDirty(campaign);
            AssetDatabase.SaveAssets();

            var font = AssetDatabase.LoadAssetAtPath<Font>(Art + "display.ttf");
            var paths = new string[4];
            paths[0] = MakeScene(campaign.menuScene, campaign, null, font);
            for (int i = 0; i < campaign.levels.Length; i++)
                paths[i + 1] = MakeScene(campaign.levels[i].sceneName, campaign, campaign.levels[i], font);
            EditorBuildSettings.scenes = System.Array.ConvertAll(paths, p => new EditorBuildSettingsScene(p, true));
            EditorSceneManager.OpenScene(paths[0], OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            Debug.Log("Cluck Defender: four scenes, 22 waves and three weapon tiers assembled.");
        }

        static LevelDataSO Level(int stage, string sector, string description, Color accent, PoolBody bird, PoolBody rock, string[] titles, int[] counts)
        {
            var level = DataAsset<LevelDataSO>(Data + $"Level{stage}.asset");
            level.stage = stage;
            level.sceneName = "Level" + stage;
            level.sector = sector;
            level.subtitle = description;
            level.useLevelDifficulty = true;
            level.chickenHits = stage + 1;
            level.eggInterval = stage switch { 1 => 2.1f, 2 => 1.65f, _ => 1.18f };
            level.accent = accent;
            level.backdrop = Sprite($"sector{stage}", 1280 / 17.6f, "jpg");
            level.rounds = new WaveDataSO[titles.Length];
            for (int i = 0; i < titles.Length; i++)
            {
                var wave = DataAsset<WaveDataSO>(Data + $"Waves/S{stage}R{i + 1:00}.asset");
                bool belt = titles[i].StartsWith("* ");
                wave.title = belt ? titles[i].Substring(2) : titles[i];
                wave.kind = belt ? WaveKind.Asteroids : WaveKind.Flock;
                wave.formation = (Formation)(stage - 1);
                wave.enemyPrefab = belt ? rock : bird;
                wave.count = counts[i];
                wave.columns = stage == 1 ? 5 : 6;
                wave.spacing = stage == 1 ? 1.65f : stage == 2 ? 1.52f : 1.47f;
                wave.health = belt ? stage : stage + (i == titles.Length - 1 ? 1 : 0);
                wave.travelSpeed = belt ? 3.5f + stage * .63f + i * .05f : 6.3f + stage * 1.1f + i * .17f;
                wave.entryDelay = belt ? .52f - stage * .035f : .20f - stage * .027f;
                wave.entryBend = stage == 1 ? new Vector2(7.8f, 3.1f) : stage == 2 ? new Vector2(7.7f, .85f) : new Vector2(4.3f, .28f);
                wave.eggInterval = Mathf.Max(.61f, 2.26f - stage * .43f - i * .065f);
                wave.eggSpeed = 3.75f + stage * .63f + i * .11f;
                wave.aim = .15f + stage * .10f + i * .014f;
                wave.diveInterval = Mathf.Max(2.55f, 7.1f - stage * .93f - i * .20f);
                wave.diveSpeed = 6.3f + stage * 1.15f + i * .19f;
                wave.giftAfterKills = i == 0 ? 4 : Mathf.Clamp(5 + i / 3, 5, 8);
                level.rounds[i] = wave;
                EditorUtility.SetDirty(wave);
            }
            EditorUtility.SetDirty(level);
            return level;
        }

        static WeaponDataSO Weapon(string name, PoolBody laser, Sprite icon, float rate, params ShotLane[] lanes)
        {
            var weapon = DataAsset<WeaponDataSO>(Data + name + ".asset");
            weapon.weaponName = name;
            weapon.icon = icon;
            weapon.projectile = laser;
            weapon.damage = 1;
            weapon.speed = 19.5f;
            weapon.shotsPerSecond = rate;
            weapon.lanes = lanes;
            EditorUtility.SetDirty(weapon);
            return weapon;
        }

        static string MakeScene(string name, CampaignDataSO campaign, LevelDataSO level, Font font)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var entry = new GameObject(level == null ? "Mission Control" : level.sector).AddComponent<SceneEntry>();
            entry.Configure(campaign, level, font);
            var camera = new GameObject("Flight Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.SetParent(entry.transform);
            camera.transform.localPosition = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.007f, .012f, .029f);
            camera.orthographic = true;
            camera.orthographicSize = Arena.HalfHeight;
            AddSceneObjects(entry, campaign, level);
            var path = Scenes + name + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        static Sprite Sprite(string name, float ppu, string extension = "png")
        {
            var path = Art + name + "." + extension;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static PoolBody Prefab(string name, BodyKind kind, Sprite sprite, int order, Vector2 collision)
        {
            var obj = new GameObject(name);
            var art = obj.AddComponent<SpriteRenderer>();
            art.sprite = sprite;
            art.sharedMaterial = SpriteMaterial();
            art.sortingOrder = order;
            if (collision != Vector2.zero)
            {
                var box = obj.AddComponent<BoxCollider2D>();
                box.size = collision;
                box.isTrigger = true;
                if (kind == BodyKind.Laser)
                {
                    var rb = obj.AddComponent<Rigidbody2D>();
                    rb.gravityScale = 0;
                    rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                }
            }
            obj.AddComponent<PoolBody>().kind = kind;
            var saved = PrefabUtility.SaveAsPrefabAsset(obj, Data + "Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(obj);
            return saved.GetComponent<PoolBody>();
        }

        static Material SpriteMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Unlit);
            if (material == null) throw new System.InvalidOperationException("The URP 2D unlit material is missing.");
            return material;
        }

        static T DataAsset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static InputActionAsset Input()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = new InputActionMap("Flight");
            asset.AddActionMap(map);
            var move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            map.AddAction("Fire", InputActionType.Button, "<Keyboard>/space").AddBinding("<Gamepad>/rightTrigger");
            map.AddAction("Bomb", InputActionType.Button, "<Keyboard>/x").AddBinding("<Gamepad>/leftShoulder");
            map.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape").AddBinding("<Gamepad>/start");
            const string path = Data + "Flight.inputactions";
            File.WriteAllText(path, asset.ToJson());
            Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }
    }
}
