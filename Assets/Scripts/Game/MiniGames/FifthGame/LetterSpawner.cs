using UnityEngine;

namespace Game.MiniGames.FifthGame
{
    public class LetterSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject letterPrefab; // Префаб UI Image + TMP_Text + FallingLetter

        [Header("Letters")]
        public string correctLetter = "Д";
        public string[] baitLetters = new[] { "Б", "Л", "Р", "А", "О", "П", "Г", "Ж" };

        [Header("Spawn")]
        public float spawnEvery = 0.9f;
        public float spreadX = 7.5f;
        [Range(0f, 1f)] public float correctChance = 0.45f;

        private float _timer;
        private FifthGame _controller;

        // ✅ Этот метод и есть "Init" — только переименован в SetController для ясности
        public void SetController(FifthGame controller) => _controller = controller;

        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= spawnEvery)
            {
                _timer = 0f;
                SpawnOne();
            }
        }

        private void SpawnOne()
        {
            bool spawnCorrect = Random.value < correctChance;
            string letter = spawnCorrect
                ? correctLetter
                : baitLetters[Random.Range(0, baitLetters.Length)];

            Vector3 pos = transform.position + Vector3.right * Random.Range(-spreadX, spreadX);
            var go = Instantiate(letterPrefab, pos, Quaternion.identity, transform.parent);

            var falling = go.GetComponent<FallingLetter>();
            if (falling != null)
            {
                falling.SetLetterAndController(_controller, letter);
                falling.SetRandomDrift(-0.7f, 0.7f, -10f, 10f);
            }
        }
    }
}