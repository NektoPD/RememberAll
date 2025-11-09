using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine.Events;
using UnityEngine.UI;

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
        private bool _ended;
        private Tween _shakeTween;
        private Vector3 _cameraStartLocalPos;
        
        [SerializeField] private TMP_Text _tutorialText;
        [SerializeField] private float _tutorialFadeDuration = 0.25f;
        [SerializeField] private float _tutorialShowSeconds = 2f;
        
        private bool _tutorialShown;

        private Tween _tutorialTween;
        
        public UnityEvent OnWinEvent = new UnityEvent();
        public UnityEvent OnLoseEvent = new UnityEvent();
        
        [SerializeField] private Button _backButton;

        public event Action OnBackClicked;

        private void Awake()
        {
            _spawners = FindObjectsOfType<LetterSpawner>(true);

            foreach (var s in _spawners)
                s.SetController(this);

            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_mainCamera != null)
                _cameraStartLocalPos = _mainCamera.transform.localPosition;

            if (_loseScreen != null)
                _loseScreen.Hidden += OnLoseHidden;
            
            _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        }

        private void Start()
        {
            RestartGame();
            TryShowTutorialOnce();
        }

        private void OnDestroy()
        {
            if (_loseScreen != null)
                _loseScreen.Hidden -= OnLoseHidden;
        }

        public void ResolveCatch(bool correct)
        {
            if (_ended) return;

            if (correct)
            {
                have++;
                UpdateUI();

                if (have >= need)
                    Win();
            }
            else
            {
                Lose();
                ShakeCamera();
            }
        }

        public void CloseLosePopupButton()
        {
            _loseScreen?.Hide();
        }
        
        private void TryShowTutorialOnce()
        {
            if (_tutorialShown || _tutorialText == null) return;

            _tutorialShown = true;

            var c = _tutorialText.color;
            c.a = 0f;
            _tutorialText.color = c;

            _tutorialTween?.Kill();
            _tutorialTween = DOTween.Sequence()
                .Append(_tutorialText.DOFade(1f, _tutorialFadeDuration))
                .AppendInterval(_tutorialShowSeconds)
                .Append(_tutorialText.DOFade(0f, _tutorialFadeDuration));
        }

        private void Win()
        {
            if (_ended) return;

            _ended = true;
            StopSpawners();
            _winScreen?.Show();
            OnWinEvent?.Invoke();
        }

        private void Lose()
        {
            if (_ended) return;

            _ended = true;
            StopSpawners();

            if (_loseScreen != null)
            {
                _loseScreen.Show("Надо попробовать снова", 1.5f);
            }
            else
            {
                RestartGame();
            }
            
            OnLoseEvent?.Invoke();
        }

        private void OnLoseHidden()
        {
            if (_ended)
                RestartGame();
        }

        private void RestartGame()
        {
            _ended = false;
            have = 0;

            UpdateUI();
            ResetCamera();

            StartSpawners();
        }

        private void StartSpawners()
        {
            foreach (var s in _spawners.Where(x => x != null))
                s.enabled = true;
        }

        private void StopSpawners()
        {
            foreach (var s in _spawners.Where(x => x != null))
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
            var t = _mainCamera.transform;

            _shakeTween = t
                .DOShakePosition(_shakeDuration, _shakeStrength)
                .OnKill(ResetCamera)
                .OnComplete(ResetCamera);
        }

        private void ResetCamera()
        {
            if (_mainCamera == null) return;

            _shakeTween?.Kill();
            _mainCamera.transform.localPosition = _cameraStartLocalPos;
        }
    }
}
