// GameBlock.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.MiniGames.GameElements
{
    public enum BlockRole { LeftLeg, RightLeg, Crossbar, Any }

    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class GameBlock : MonoBehaviour
    {
        [Header("Hit area (must be isTrigger=true)")]
        [SerializeField] private Collider2D _hitCollider2D;

        [Header("Semantic role (for win validation)")]
        [SerializeField] private BlockRole _role = BlockRole.Any;

        private readonly HashSet<GameBlock> _touchingBlocks = new HashSet<GameBlock>();

        public Collider2D HitCollider => _hitCollider2D;
        public BlockRole Role => _role;

        public event Action<GameBlock> ConnectionsChanged;

        private void Reset()
        {
            if (_hitCollider2D == null)
                _hitCollider2D = GetComponent<Collider2D>();

            var rb = GetComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0f;

            var col = GetComponent<Collider2D>();
            if (col) col.isTrigger = true; // удобный дефолт
        }

        private void OnValidate()
        {
            if (_hitCollider2D != null && !_hitCollider2D.isTrigger)
                Debug.LogWarning($"{name}: _hitCollider2D должен быть isTrigger=true");
        }

        private void OnTriggerEnter2D(Collider2D other) => TryAdd(other);
        private void OnTriggerExit2D (Collider2D other) => TryRemove(other);

        private void TryAdd(Collider2D other)
        {
            var otherBlock = other ? other.GetComponentInParent<GameBlock>() : null;
            if (!otherBlock || otherBlock == this) return;
            if (other != otherBlock._hitCollider2D) return; // считаем только попадание в "рабочий" хит-коллайдер

            if (_touchingBlocks.Add(otherBlock))
                ConnectionsChanged?.Invoke(this);
        }

        private void TryRemove(Collider2D other)
        {
            var otherBlock = other ? other.GetComponentInParent<GameBlock>() : null;
            if (!otherBlock) return;

            if (_touchingBlocks.Remove(otherBlock))
                ConnectionsChanged?.Invoke(this);
        }

        public bool IsTouching(GameBlock other) => other && _touchingBlocks.Contains(other);

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            foreach (var b in _touchingBlocks)
                if (b) Gizmos.DrawLine(transform.position, b.transform.position);
        }
#endif
    }
}
