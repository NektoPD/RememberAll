using UnityEngine;

namespace Game.MiniGames.FifthGame
{
    [RequireComponent(typeof(Collider2D))]
    public class MissCatcher : MonoBehaviour
    {
        void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col) col.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var fl = other.GetComponent<FallingLetter>();
            if (fl != null)
            {
                fl.ReturnToPool();
            }
        }
    }
}