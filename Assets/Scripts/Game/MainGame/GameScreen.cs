using System.Collections.Generic;
using Game.Intro;
using UnityEngine;

namespace Game.MainGame
{
    public class GameScreen : MonoBehaviour
    {
        [SerializeField] private List<LevelButton> _levelButtons;
        [SerializeField] private IntroScreen _introScreen;
    }
}