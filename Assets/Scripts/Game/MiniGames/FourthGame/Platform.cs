using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.MiniGames.FourthGame
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class Platform : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image _platform;
        [SerializeField] private Image _filledImage;

        [Header("Links")]
        [SerializeField] private FourthGame _game;

        [Header("Rotation")]
        [SerializeField] private float _minAngle = -10f;
        [SerializeField] private float _maxAngle = 10f;
        [SerializeField] private float _rotateDuration = 0.8f;

        private Coroutine _wobbleCR;

        public float Progress
        {
            get => _filledImage != null ? _filledImage.fillAmount : 0f;
            set
            {
                if (_filledImage == null) return;
                _filledImage.fillAmount = Mathf.Clamp01(value);
            }
        }

        public void ResetState()
        {
            StopWobble();
            Progress = 0f;
            transform.rotation = Quaternion.identity;
        }

        public void StartWobble()
        {
            if (_wobbleCR != null) return;
            _wobbleCR = StartCoroutine(WobbleLoop());
        }

        public void StopWobble()
        {
            if (_wobbleCR != null)
            {
                StopCoroutine(_wobbleCR);
                _wobbleCR = null;
            }
        }

        private IEnumerator WobbleLoop()
        {
            while (true)
            {
                float targetZ = Random.Range(_minAngle, _maxAngle);
                Quaternion start = transform.rotation;
                Quaternion target = Quaternion.Euler(0f, 0f, targetZ);

                float t = 0f;
                while (t < _rotateDuration)
                {
                    t += Time.deltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, t / _rotateDuration);
                    transform.rotation = Quaternion.Slerp(start, target, k);
                    yield return null;
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.TryGetComponent(out Ball _))
                _game?.OnBallOnPlatform(true);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.collider.TryGetComponent(out Ball _))
                _game?.OnBallOnPlatform(false);
        }
    }
}
