using System;
using Game.MainGame.DTO;
using UnityEngine;

namespace Game.SaveSystem
{
    public static class PlayerDataSaver
    {
        private const string Key = "playerDTO_json";

        public static PlayerDTO LoadOrDefault()
        {
            var json = PlayerPrefs.GetString(Key, string.Empty);
            if (string.IsNullOrEmpty(json))
                return new PlayerDTO { IsIntroCompleted = false };

            try
            {
                var dto = JsonUtility.FromJson<PlayerDTO>(json);
                return dto ?? new PlayerDTO { IsIntroCompleted = false };
            }
            catch (Exception e)
            {
                Debug.LogWarning($"PlayerDataSaver parse error: {e}");
                return new PlayerDTO { IsIntroCompleted = false };
            }
        }

        public static void Save(PlayerDTO dto)
        {
            var json = JsonUtility.ToJson(dto ?? new PlayerDTO { IsIntroCompleted = false });
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }
    }
}