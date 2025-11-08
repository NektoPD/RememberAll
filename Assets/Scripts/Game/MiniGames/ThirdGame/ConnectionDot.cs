using UnityEngine;
using DG.Tweening;

namespace Game.MiniGames.ThirdGame
{
    [RequireComponent(typeof(Collider2D))]
    public class ConnectionDot : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Color idleColor = Color.white;
        [SerializeField] private Color markedColor = new Color(0.4f, 1f, 0.6f);
        [SerializeField] private Color hoverColor = new Color(1f, 0.95f, 0.6f);

        public bool IsMarked { get; private set; }

        public void Mark()
        {
            if (IsMarked) return;
            IsMarked = true;
            if (_renderer) _renderer.DOColor(markedColor, 0.12f);
        }

        public void Unmark()
        {
            IsMarked = false;
            if (_renderer) _renderer.DOColor(idleColor, 0.1f);
        }

        private void OnMouseDown()
        {
            var ctrl = FindObjectOfType<ThirdGameController>();
            ctrl?.BeginDragFromDot(this);
        }

        private void OnMouseEnter()
        {
            if (_renderer) _renderer.DOColor(IsMarked ? markedColor : hoverColor, 0.08f);
        }

        private void OnMouseExit()
        {
            if (_renderer) _renderer.DOColor(IsMarked ? markedColor : idleColor, 0.08f);
        }
    }
}