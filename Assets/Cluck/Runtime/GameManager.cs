using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cluck
{
    public sealed class GameManager : MonoBehaviour
    {
        public const int StartingLives = 3, StartingBombs = 2;
        public static GameManager Instance { get; private set; }
        public CampaignDataSO Campaign { get; private set; }
        public int Stage { get; private set; }
        public int Round { get; private set; }
        public int Lives { get; private set; }
        public int Score { get; private set; }
        public int Bombs { get; private set; }
        public int Power { get; private set; }
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
            Lives = StartingLives;
            Bombs = StartingBombs;
            Power = 1;
            Score = 0;
            Changed?.Invoke();
        }

        public void PrepareStage(int stage)
        {
            if (Stage != stage) ResetRun(stage);
        }

        public void NewGame() { ResetRun(1); LoadStage(1); }
        public void Retry() { var stage = Stage; ResetRun(stage); LoadStage(stage); }
        public void OpenMenu() { Stage = 0; SceneManager.LoadScene(Campaign.menuScene); }
        public void AdvanceStage() { Stage++; Round = 1; Changed?.Invoke(); LoadStage(Stage); }
        void LoadStage(int stage) => SceneManager.LoadScene(Campaign.levels[stage - 1].sceneName);

        public void BeginRound(int round) { Round = round; RoundChanged?.Invoke(round); Changed?.Invoke(); }
        public void AddScore(int score) { Score += score; ScoreChanged?.Invoke(Score); Changed?.Invoke(); }
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
            Damaged?.Invoke();
            Changed?.Invoke();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
