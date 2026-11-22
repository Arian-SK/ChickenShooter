using System;
using System.IO;
using UnityEngine;

namespace Cluck
{
    [Serializable]
    internal sealed class RunSave
    {
        public int version;
        public int levelIndex, roundIndex, score, lives, bombs, weaponPower, foodProgress;
    }

    internal static class SaveFile
    {
        static string PathName => Path.Combine(Application.persistentDataPath, "savegame.json");

        internal static RunSave Read(CampaignDataSO campaign)
        {
            try
            {
                if (!File.Exists(PathName)) return null;
                var save = JsonUtility.FromJson<RunSave>(File.ReadAllText(PathName));
                if (save == null || save.version != 1 || save.levelIndex < 1 ||
                    save.levelIndex > campaign.levels.Length) return null;
                var level = campaign.levels[save.levelIndex - 1];
                return save.roundIndex >= 1 && save.roundIndex <= level.rounds.Length &&
                    save.lives >= 1 && save.lives <= GameManager.MaxStartingLives &&
                    save.score >= 0 && save.bombs >= 0 && save.weaponPower >= 1 &&
                    save.weaponPower <= campaign.weaponTiers.Length &&
                    save.foodProgress >= 0 && save.foodProgress < GameManager.FoodGoal ? save : null;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
            {
                Debug.LogWarning("Could not read progress: " + e.Message);
                return null;
            }
        }

        internal static void Write(RunSave save)
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string path = PathName;
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(save, true));
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning("Could not save progress: " + e.Message);
            }
        }

        internal static void Clear()
        {
            try { File.Delete(PathName); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            { Debug.LogWarning("Could not clear progress: " + e.Message); }
        }
    }
}
