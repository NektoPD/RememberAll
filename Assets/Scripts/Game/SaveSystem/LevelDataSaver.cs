using System;
using System.Collections.Generic;
using System.Linq;
using Game.MainGame.DTO;
using UnityEngine;

namespace Game.SaveSystem
{
    public static class LevelDataSaver
    {
        private const string Key = "levelDTO_json";

        [Serializable]
        private class LevelDataCollection
        {
            public List<LevelData> Items = new();
        }

        public static bool TryLoad(out List<LevelData> data)
        {
            data = null;
            var json = PlayerPrefs.GetString(Key, string.Empty);
            if (string.IsNullOrEmpty(json)) return false;

            try
            {
                var wrapper = JsonUtility.FromJson<LevelDataCollection>(json);
                data = wrapper?.Items ?? new List<LevelData>();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"LevelDataSaver.TryLoad parse error: {e}");
                return false;
            }
        }

        public static void Save(List<LevelData> data)
        {
            var wrapper = new LevelDataCollection { Items = data ?? new List<LevelData>() };
            var json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Гарантирует полный набор LevelData по количеству enum LevelType.
        /// Если сохранений нет или набор неполный — пересобирает дефолт.
        /// </summary>
        public static List<LevelData> LoadOrCreateCompleteSet()
        {
            List<LevelData> loaded;
            if (!TryLoad(out loaded)) loaded = new List<LevelData>();

            var allTypes = (LevelType[])Enum.GetValues(typeof(LevelType));
            var dict = loaded.ToDictionary(l => l.LevelType, l => l);

            bool changed = false;

            foreach (var t in allTypes)
            {
                if (!dict.ContainsKey(t))
                {
                    dict[t] = new LevelData
                    {
                        LevelType = t,
                        Progress = 0f,
                        IsUnlocked = false // дефолт: закрыт
                    };
                    changed = true;
                }
            }

            // Если в сохранении были «лишние» типы (неизвестные) — чистим
            var final = dict
                .Where(p => allTypes.Contains(p.Key))
                .Select(p => p.Value)
                .OrderBy(v => Array.IndexOf(allTypes, v.LevelType))
                .ToList();

            if (changed || final.Count != loaded.Count)
                Save(final);

            return final;
        }
    }
}
