using UnityEngine;

namespace Game.MiniGames.FifthGame
{
    [RequireComponent(typeof(Collider2D))]
    public class PlayerBucket : MonoBehaviour
    {
        public float speed = 8f;

        float _minX, _maxX, _halfWidth;

        void Start()
        {
            var col = GetComponent<Collider2D>();
            if (col) _halfWidth = col.bounds.extents.x;
            else
            {
                var rend = GetComponent<Renderer>();
                _halfWidth = rend ? rend.bounds.extents.x : 0.5f;
            }

            CacheClamp();
        }

        void CacheClamp()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                _minX = -Mathf.Infinity;
                _maxX = Mathf.Infinity;
                return;
            }

            Vector3 left = cam.ViewportToWorldPoint(new Vector3(0, 0.5f, Mathf.Abs(cam.transform.position.z)));
            Vector3 right = cam.ViewportToWorldPoint(new Vector3(1, 0.5f, Mathf.Abs(cam.transform.position.z)));

            _minX = left.x + _halfWidth;
            _maxX = right.x - _halfWidth;
        }

        void Update()
        {
            float h = Input.GetAxisRaw("Horizontal");
            Vector3 pos = transform.position + Vector3.right * h * speed * Time.deltaTime;
            pos.x = Mathf.Clamp(pos.x, _minX, _maxX);
            transform.position = pos;
        }
    }
}