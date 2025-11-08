using UnityEngine;
using DG.Tweening;
using UnityEngine.Serialization;

namespace Game.MiniGames.ThirdGame
{
    [RequireComponent(typeof(Collider2D))]
    public class ConnectionDot : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Color _idleColor = Color.white;
        [SerializeField] private Color _markedColor = new Color(0.4f, 1f, 0.6f);
        [SerializeField] private Color _hoverColor = new Color(1f, 0.95f, 0.6f);

        public bool IsMarked { get; private set; }

        public void Mark()
        {
            if (IsMarked) return;
            IsMarked = true;
            if (_renderer) _renderer.DOColor(_markedColor, 0.12f);
        }

        public void Unmark()
        {
            IsMarked = false;
            if (_renderer) _renderer.DOColor(_idleColor, 0.1f);
        }

        private void OnMouseDown()
        {
            var ctrl = FindObjectOfType<ThirdGameController>();
            ctrl?.BeginDragFromDot(this);
        }

        private void OnMouseEnter()
        {
            if (_renderer) _renderer.DOColor(IsMarked ? _markedColor : _hoverColor, 0.08f);
        }

        private void OnMouseExit()
        {
            if (_renderer) _renderer.DOColor(IsMarked ? _markedColor : _idleColor, 0.08f);
        }
    }
}