using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Game.MiniGames.FourthGame
{
    public class FourthGame : MonoBehaviour
    {
        [Header("Scene Links")]
        [SerializeField] private Ball _ballPrefab;
        [SerializeField] private RectTransform _ballSpawnPosition;
        [SerializeField] private Platform _platform;
        [SerializeField] private BallCatcher _catcher;

        [Header("UI")]
        [SerializeField] private TMP_Text _percentageProgress;

        [Header("Popups")]
        [SerializeField] private Game.Popups.GamePopup _losePopup;
        [SerializeField] private Game.Popups.GamePopup _winPopup;

        [Header("Gameplay")]
        [Tooltip("Скорость заполнения (доля в сек.) пока шарик на платформе")]
        [SerializeField] private float _fillPerSecond = 0.075f; 

        [Header("Tutorial")]
        [SerializeField] private TMP_Text _tutorialText;
        [SerializeField] private float _tutorialFadeDuration = 0.25f;
        [SerializeField] private float _tutorialShowSeconds = 2f;

        [Header("Random Nudges")]
        [SerializeField] private Vector2 _nudgeIntervalRange = new Vector2(0.6f, 1.4f); // как часто пинаем
        [SerializeField] private Vector2 _nudgeImpulseRange = new Vector2(0.45f, 1.25f); // сила по X
        [SerializeField] private float _nudgeUpward = 0.15f; // небольшой импульс вверх (0 = выключить)

        private float _nextNudgeAt = -1f;
        
        private Ball _currentBall;
        private bool _isPlaying;
        private bool _ballOnPlatform;
        private bool _ended;
        private bool _tutorialShown; // показываем только один раз за сессию

        private Tween _tutorialTween;
        
        public UnityEvent OnWinEvent = new UnityEvent();
        public UnityEvent OnLoseEvent = new UnityEvent();

        private void Awake()
        {
            // гарантируем подписку один раз
            if (_losePopup != null)
                _losePopup.Hidden += OnLosePopupHidden;
        }
        
        private void Start()
        {
            RestartGame();
            TryShowTutorialOnce();
        }

        public void LosePopupClosed()
        {
            if (!_ended) return;
            RestartGame();
        }

        public void CloseLosePopupButton()
        {
            _losePopup?.Hide();
        }
        
        private void OnLosePopupHidden()
        {
            // когда попап реально закрылся — перезапускаем
            if (_ended)
                RestartGame();
        }
        
        public void OnBallCaught(Ball ball)
        {
            if (_ended) return;

            if (_currentBall != null) _currentBall.FadeOutAndDestroy();

            _isPlaying = false;
            _ended = true;
            _platform.StopWobble();
            _currentBall?.SetControlEnabled(false);

            // просто показываем попап; после нажатия кнопки он вызовет Hide(),
            // событие Hidden подхватит перезапуск
            _losePopup?.Show("Нужно попробовать еще раз", 1.5f);
            OnLoseEvent?.Invoke();
        }

        // в OnBallOnPlatform(bool on)
        public void OnBallOnPlatform(bool on)
        {
            if (_ended) return;

            _ballOnPlatform = on;

            if (on && !_isPlaying)
            {
                _isPlaying = true;
                _platform.StartWobble();
            }

            // планируем первый толчок, когда шар оказался на платформе
            if (on)
                ScheduleNextNudge();
        }

        private void Update()
        {
            if (!_isPlaying || _ended) return;

            if (_ballOnPlatform)
            {
                if (_ballOnPlatform && Time.time >= _nextNudgeAt)
                {
                    DoRandomNudge();
                    ScheduleNextNudge();
                }
                
                float p = _platform.Progress + _fillPerSecond * Time.deltaTime;
                _platform.Progress = p;
                UpdateProgressText(_platform.Progress);

                if (_platform.Progress >= 1f)
                {
                    Win();
                }
            }
        }

        
        // новые приватные методы
        private void ScheduleNextNudge()
        {
            _nextNudgeAt = Time.time + Random.Range(_nudgeIntervalRange.x, _nudgeIntervalRange.y);
        }

        private void DoRandomNudge()
        {
            if (_currentBall == null) return;

            float dir = Random.value < 0.5f ? -1f : 1f;
            float magX = Random.Range(_nudgeImpulseRange.x, _nudgeImpulseRange.y);
            float up = _nudgeUpward > 0f ? Random.Range(0f, _nudgeUpward) : 0f;

            Vector2 impulse = new Vector2(dir * magX, up);
            _currentBall.AddImpulse(impulse);
        }

        private void Win()
        {
            if (_ended) return;
            _ended = true;
            _isPlaying = false;
            _platform.StopWobble();
            _currentBall?.SetControlEnabled(false);
            if (_currentBall != null) _currentBall.FadeOutAndDestroy();
            _winPopup?.Show();
            OnWinEvent?.Invoke(); 
        }

        private void RestartGame()
        {
            _ended = false;
            _isPlaying = false;
            _ballOnPlatform = false;
            _nextNudgeAt = -1f;

            if (_currentBall != null)
            {
                Destroy(_currentBall.gameObject);
                _currentBall = null;
            }

            _platform.ResetState();
            UpdateProgressText(0f);
            SpawnBall();
        }

        private void SpawnBall()
        {
            if (_ballPrefab == null || _ballSpawnPosition == null)
            {
                Debug.LogError("[FourthGame] Prefab или позиция спавна не заданы");
                return;
            }

            _currentBall = Instantiate(_ballPrefab, transform);
            _currentBall.PrepareAt(_ballSpawnPosition);
            _currentBall.PlaySpawn();

            // управление доступно сразу после спавна
            _currentBall.SetControlEnabled(true);
        }

        private void UpdateProgressText(float progress01)
        {
            if (_percentageProgress == null) return;
            int percent = Mathf.Clamp(Mathf.RoundToInt(progress01 * 100f), 0, 100);
            _percentageProgress.text = $"{percent}%";
        }

        private void TryShowTutorialOnce()
        {
            if (_tutorialShown || _tutorialText == null) return;

            _tutorialShown = true;

            // гарантируем стартовую прозрачность
            var c = _tutorialText.color;
            c.a = 0f;
            _tutorialText.color = c;

            _tutorialTween?.Kill();
            _tutorialTween = DOTween.Sequence()
                .Append(_tutorialText.DOFade(1f, _tutorialFadeDuration))
                .AppendInterval(_tutorialShowSeconds)
                .Append(_tutorialText.DOFade(0f, _tutorialFadeDuration));
        }
    }
}
