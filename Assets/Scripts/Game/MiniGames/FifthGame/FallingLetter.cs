using TMPro;
using UnityEngine;

namespace Game.MiniGames.FifthGame
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class FallingLetter : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        private FifthGame _controller;
        private LetterSpawner _owner;
        private Rigidbody2D _rb;
        private Collider2D _col;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            if (_text == null) _text = GetComponentInChildren<TMP_Text>(true);
        }

        public void SetLetterAndController(FifthGame controller, string letter)
        {
            _controller = controller;
            if (_text != null) _text.text = letter;
        }

        public void SetOwner(LetterSpawner owner) => _owner = owner;

        public void ResetPhysics()
        {
            if (_rb == null) return;
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }

        public void SetRandomDrift(float minXVel, float maxXVel, float minRot, float maxRot)
        {
            if (_rb)
            {
                _rb.linearVelocity = new Vector2(Random.Range(minXVel, maxXVel), _rb.linearVelocity.y);
                _rb.MoveRotation(Random.Range(minRot, maxRot));
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            // Попадание в ведро
            if (other.CompareTag("Player"))
            {
                bool isCorrect = _text != null && _text.text == "Д";
                _controller?.ResolveCatch(isCorrect);
                ReturnToPool();
            }
        }

        /// <summary>Вернуть объект в пул (например, из ловушки промахов).</summary>
        public void ReturnToPool() => _owner?.ReleaseToPool(this);

        /// <summary>Сброс состояния перед возвращением в пул.</summary>
        public void OnDespawn()
        {
            ResetPhysics();
            // Если нужно — очистить текст/эффекты
            // if (_text) _text.text = string.Empty; // обычно не требуется
        }
    }
}
