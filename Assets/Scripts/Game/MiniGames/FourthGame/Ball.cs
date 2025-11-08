using DG.Tweening;
using UnityEngine;

namespace Game.MiniGames.FourthGame
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class Ball : MonoBehaviour
    {
        [Header("Spawn/Despawn")]
        [SerializeField] private float _spawnFadeDuration = 0.3f;
        [SerializeField] private float _despawnFadeDuration = 0.25f;

        [Header("Control")]
        [Tooltip("Целевая горизонтальная скорость при нажатой стрелке")]
        [SerializeField] private float _moveSpeed = 6f;
        [Tooltip("Ускорение набора/сброса горизонтальной скорости")]
        [SerializeField] private float _acceleration = 20f;
        [Tooltip("Ограничение по |vx| (страховка)")]
        [SerializeField] private float _maxHorizontalSpeed = 10f;

        private SpriteRenderer _sr;
        private Rigidbody2D _rb;
        private bool _controlsEnabled;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (!_controlsEnabled || _rb == null || !_rb.simulated) return;

            // простое осевое управление
            float input = 0f;
            // поддержим и стрелки, и A/D на случай клавиатуры без русской раскладки
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) input -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) input += 1f;

            float targetVx = input * _moveSpeed;
            float vx = _rb.linearVelocity.x;

            // сглаженно тянем текущую скорость к целевой
            vx = Mathf.MoveTowards(vx, targetVx, _acceleration * Time.deltaTime);

            // гарантированный кламп
            vx = Mathf.Clamp(vx, -_maxHorizontalSpeed, _maxHorizontalSpeed);

            _rb.linearVelocity = new Vector2(vx, _rb.linearVelocity.y);
        }

        public void AddImpulse(Vector2 impulse)
        {
            if (_rb != null && _rb.simulated)
                _rb.AddForce(impulse, ForceMode2D.Impulse);
        }
        
        public void PrepareAt(Transform spawnPoint)
        {
            transform.position = spawnPoint.position;
            _rb.simulated = false;

            var c = _sr.color;
            c.a = 0f;
            _sr.color = c;
        }

        public void PlaySpawn()
        {
            _sr.DOFade(1f, _spawnFadeDuration)
               .OnComplete(() => _rb.simulated = true);
        }

        public void FadeOutAndDestroy()
        {
            _controlsEnabled = false;
            _rb.simulated = false;
            _sr.DOFade(0f, _despawnFadeDuration)
               .OnComplete(() => Destroy(gameObject));
        }

        public void SetControlEnabled(bool enabled)
        {
            _controlsEnabled = enabled;
        }
    }
}
