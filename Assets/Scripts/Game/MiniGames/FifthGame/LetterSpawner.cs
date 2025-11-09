using System.Collections.Generic;
using UnityEngine;

namespace Game.MiniGames.FifthGame
{
    public class LetterSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("Префаб UI-Image с дочерним TMP_Text и компонентом FallingLetter")]
        public GameObject letterPrefab;

        [Header("Letters")]
        public string correctLetter = "Д";
        public string[] baitLetters = new[] { "Б", "Л", "Р", "А", "О", "П", "Г", "Ж" };

        [Header("Spawn")]
        public float spawnEvery = 0.9f;
        public float spreadX = 7.5f;
        [Range(0f, 1f)] public float correctChance = 0.45f;

        [Header("Pool")]
        [Min(0)] public int prewarm = 20;

        private float _timer;
        private FifthGame _controller;
        private readonly Queue<GameObject> _pool = new Queue<GameObject>();

        public void SetController(FifthGame controller) => _controller = controller;

        void Awake()
        {
            // Предпрогрев
            if (letterPrefab == null) return;
            for (int i = 0; i < prewarm; i++)
            {
                var go = Instantiate(letterPrefab, transform);
                go.SetActive(false);
                _pool.Enqueue(go);
            }
        }

        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= spawnEvery)
            {
                _timer = 0f;
                SpawnOne();
            }
        }

        void SpawnOne()
        {
            if (letterPrefab == null || _controller == null) return;

            bool spawnCorrect = Random.value < correctChance;
            string letter = spawnCorrect ? correctLetter : baitLetters[Random.Range(0, baitLetters.Length)];

            var go = GetFromPool();
            // Размещаем под тем же родителем, что и спавнер (например, Canvas/слой)
            go.transform.SetParent(transform.parent, false);

            Vector3 pos = transform.position + Vector3.right * Random.Range(-spreadX, spreadX);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;
            go.SetActive(true);

            var falling = go.GetComponent<FallingLetter>();
            if (falling != null)
            {
                falling.SetLetterAndController(_controller, letter);
                falling.SetOwner(this);
                falling.ResetPhysics();
                falling.SetRandomDrift(-0.7f, 0.7f, -10f, 10f);
            }
        }

        GameObject GetFromPool()
        {
            if (_pool.Count > 0)
                return _pool.Dequeue();

            var go = Instantiate(letterPrefab, transform);
            go.SetActive(false);
            return go;
        }

        public void ReleaseToPool(FallingLetter letter)
        {
            if (letter == null) return;
            var go = letter.gameObject;

            // Сброс визуала/физики
            letter.OnDespawn();

            // Спрячем и сложим назад в очередь
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            _pool.Enqueue(go);
        }
    }
}
