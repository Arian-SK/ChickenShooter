using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cluck
{
    public sealed class GameManager : MonoBehaviour
    {
        public const int StartingLives = 3, StartingBombs = 2;
        internal const int MaxStartingLives = 10;
        const string LivesOption = "Cluck.StartingLives";
        const string EggsOption = "Cluck.ShootableEggs";
        internal static int ConfiguredLives => Mathf.Clamp(PlayerPrefs.GetInt(LivesOption, StartingLives), StartingLives, MaxStartingLives);
        internal static bool ShootableEggs => PlayerPrefs.GetInt(EggsOption, 0) != 0;

        internal static void CycleStartingLives()
        {
            int next = ConfiguredLives < 5 ? 5 : ConfiguredLives < MaxStartingLives ? MaxStartingLives : StartingLives;
            PlayerPrefs.SetInt(LivesOption, next);
            PlayerPrefs.Save();
        }

        internal static void ToggleShootableEggs()
        {
            PlayerPrefs.SetInt(EggsOption, ShootableEggs ? 0 : 1);
            PlayerPrefs.Save();
        }
        public const int FoodGoal = 6;
        public static GameManager Instance { get; private set; }
        public CampaignDataSO Campaign { get; private set; }
        public int Stage { get; private set; }
        public int Round { get; private set; }
        public int Lives { get; private set; }
        public int Score { get; private set; }
        public int Bombs { get; private set; }
        public int Power { get; private set; }
        public int Food { get; private set; }
        public event Action Changed;
        public event Action Damaged;
        public event Action<int> PowerChanged;
        public event Action<int> RoundChanged;
        public event Action<int> ScoreChanged;

        public static GameManager Get(CampaignDataSO campaign)
        {
            if (Instance != null) return Instance;
            var manager = new GameObject("Campaign Session").AddComponent<GameManager>();
            Instance = manager;
            DontDestroyOnLoad(manager.gameObject);
            manager.Campaign = campaign;
            return manager;
        }

        public void ResetRun(int stage)
        {
            Stage = stage;
            Round = 1;
            Lives = ConfiguredLives;
            Bombs = StartingBombs;
            Power = 1;
            Score = 0;
            Food = 0;
            Changed?.Invoke();
        }

        public void PrepareStage(int stage)
        {
            if (Stage != stage) ResetRun(stage);
        }

        public void NewGame() { ResetRun(1); Checkpoint(); LoadStage(1); }
        public void Retry() { ResetRun(Stage); Checkpoint(); LoadStage(Stage); }
        public void OpenMenu() { Stage = 0; SceneManager.LoadScene(Campaign.menuScene); }
        public void AdvanceStage() { Stage++; Round = 1; Checkpoint(); LoadStage(Stage); }
        void LoadStage(int stage) => SceneManager.LoadScene(Campaign.levels[stage - 1].sceneName);

        public void BeginRound(int round) { Round = round; RoundChanged?.Invoke(round); Changed?.Invoke(); }

        internal RunSave SavedRun() => SaveFile.Read(Campaign);

        internal void ContinueGame(RunSave save)
        {
            Stage = save.levelIndex;
            Round = save.roundIndex;
            Score = save.score;
            Lives = save.lives;
            Bombs = save.bombs;
            Power = save.weaponPower;
            Food = save.foodProgress;
            LoadStage(Stage);
        }

        internal void Checkpoint() => WriteCheckpoint(Stage, Round);
        internal void CheckpointNextStage() => WriteCheckpoint(Stage + 1, 1);

        void WriteCheckpoint(int stage, int round) => SaveFile.Write(new RunSave
        {
            version = 1, levelIndex = stage, roundIndex = round, score = Score,
            lives = Lives, bombs = Bombs, weaponPower = Power, foodProgress = Food
        });

        internal bool CollectFood()
        {
            bool earned = ++Food == FoodGoal;
            if (earned)
            {
                Food = 0;
                if (Bombs < int.MaxValue) Bombs++;
            }
            AddScore(50);
            return earned;
        }
        public void AddScore(int score)
        {
            Score = (int)Math.Min((long)Score + score, int.MaxValue);
            ScoreChanged?.Invoke(Score);
            Changed?.Invoke();
        }
        public void CollectGift()
        {
            if (Power < Campaign.weaponTiers.Length) { Power++; PowerChanged?.Invoke(Power); Changed?.Invoke(); }
            else AddScore(250);
        }

        public bool SpendBomb()
        {
            if (Bombs <= 0) return false;
            Bombs--;
            Changed?.Invoke();
            return true;
        }

        public void TakeHit()
        {
            if (Lives <= 0) return;
            Lives--;
            if (Power > 1) { Power--; PowerChanged?.Invoke(Power); }
            if (Lives == 0)
                SaveFile.Write(new RunSave
                {
                    version = 1, levelIndex = Stage, roundIndex = 1,
                    lives = ConfiguredLives, bombs = StartingBombs, weaponPower = 1
                });
            Damaged?.Invoke();
            Changed?.Invoke();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
