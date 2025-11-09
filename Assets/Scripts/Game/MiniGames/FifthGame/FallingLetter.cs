using TMPro;
using UnityEngine;

namespace Game.MiniGames.FifthGame
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class FallingLetter : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private float _lifetime = 7f;

        private FifthGame _controller;
        private Rigidbody2D _rb;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_text == null) _text = GetComponentInChildren<TMP_Text>(true);
            Destroy(gameObject, _lifetime);
        }

        // ✅ Новый метод вместо Init — чтобы не конфликтовал с MonoBehaviour.Init()
        public void SetLetterAndController(FifthGame controller, string letter)
        {
            _controller = controller;
            if (_text != null)
                _text.text = letter;
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
            if (!other.TryGetComponent(out PlayerBucket playerBucket)) return;

            bool isCorrect = _text != null && _text.text == "Д";
            _controller?.ResolveCatch(isCorrect);

            Destroy(gameObject);
        }
    }
}