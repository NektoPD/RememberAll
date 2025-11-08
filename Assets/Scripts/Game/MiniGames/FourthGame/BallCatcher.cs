using System;
using UnityEngine;

namespace Game.MiniGames.FourthGame
{
    public class BallCatcher : MonoBehaviour
    {
        [SerializeField] private FourthGame _game;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Ball ball))
            {
                _game?.OnBallCaught(ball);
            }
        }
    }
}