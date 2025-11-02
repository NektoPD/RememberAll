using System;
using Unity.VisualScripting;

namespace Game.MainGame.DTO
{
    [Serializable]
    public class LevelData
    {
        public LevelType LevelType;
        public float Progress;
        public bool IsUnlocked;
    }

    public enum LevelType
    {
        А,
        Б,
        В,
        Г,
        Д,
        Е,
        Ё,
        Ж,
        З,
        И,
        Й,
        К,
        Л,
        М,
        Н,
        О,
        П,
        Р,
        С,
        Т,
        У,
        Ф,
        Х,
        Ц,
        Ч,
        Ш,
        Щ,
        Ъ,
        Ы,
        Ь,
        Э,
        Ю,
        Я
    }
}