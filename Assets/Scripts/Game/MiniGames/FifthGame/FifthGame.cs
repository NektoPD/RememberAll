using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace Game.MiniGames.FifthGame
{
    public class FifthGame : MonoBehaviour
    {
        [Header("Progress")]
        [Min(1)] public int need = 10;
        public int have = 0;

        [Header("Refs")]
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private Game.Popups.GamePopup _winScreen;
        [SerializeField] private Game.Popups.GamePopup _loseScreen;
        [SerializeField] private Camera _mainCamera;

        [Header("Shake")]
        [SerializeField] private float _shakeDuration = 0.35f;
        [SerializeField] private float _shakeStrength = 0.8f;

        private LetterSpawner[] _spawners;
        private bool _finished;
        private Tween _shakeTween;

        void Awake()
        {
            _spawners = FindObjectsOfType<LetterSpawner>(true);

            // ⚙️ Передаём ссылку на этот объект в каждый спавнер
            foreach (var s in _spawners)
                s.SetController(this);

            if (_mainCamera == null)
                _mainCamera = Camera.main;
        }

        void Start() => UpdateUI();

        public void ResolveCatch(bool correct)
        {
            if (_finished) return;

            if (correct)
            {
                have++;
                UpdateUI();

                if (have >= need)
                {
                    _finished = true;
                    StopSpawners();
                    _winScreen?.Show();
                }
            }
            else
            {
                _finished = true;
                StopSpawners();
                ShakeCamera();

                if (_loseScreen != null)
                {
                    _loseScreen.Hidden -= OnLoseHidden;
                    _loseScreen.Hidden += OnLoseHidden;
                    _loseScreen.Show();
                }
                else
                {
                    RestartScene();
                }
            }
        }

        private void OnLoseHidden() => RestartScene();

        private void RestartScene()
        {
            if (_loseScreen != null) _loseScreen.Hidden -= OnLoseHidden;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void StopSpawners()
        {
            foreach (var s in _spawners.Where(s => s != null))
                s.enabled = false;
        }

        private void UpdateUI()
        {
            if (_progressText != null)
                _progressText.text = $"Д: {have}/{need}";
        }

        private void ShakeCamera()
        {
            if (_mainCamera == null) return;

            _shakeTween?.Kill();
            Vector3 startPos = _mainCamera.transform.localPosition;

            _shakeTween = _mainCamera.transform
                .DOShakePosition(_shakeDuration, _shakeStrength)
                .OnKill(() => _mainCamera.transform.localPosition = startPos)
                .OnComplete(() => _mainCamera.transform.localPosition = startPos);
        }
    }
}
