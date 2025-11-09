using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using Game.Popups;
using UnityEngine.Events;
using Random = UnityEngine.Random; // для GamePopup

namespace Game.MiniGames.SixthGame
{
    public class SixthGame : MonoBehaviour
    {
        [Header("Prefabs & Refs")]
        [SerializeField] private SixthLetterButtonElement _tilePrefab;
        [SerializeField] private RectTransform _parent;
        [SerializeField] private RectTransform _spawnOrigin; // привяжи к верхнему левому углу контейнера (или оставь null)
        [SerializeField] private GamePopup _winPopup;        // <-- заменяем CanvasGroup на GamePopup
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private Button _hintButton;

        [Header("Layout")]
        [SerializeField] private Vector2 _elementSize = new(160, 60);
        [SerializeField] private Vector2 _spacing = new(16, 12);
        [SerializeField] private bool _autoComputeColumns = true;
        [SerializeField] private int _manualColumns = 10;
        [Tooltip("Небольшое покачивание контейнера — по-другому, чем во второй игре")]
        [SerializeField] private bool _wiggleContainer = true;

        [Header("Gameplay")]
        [SerializeField] private int _spawnCount = 60;
        [SerializeField, Range(0.02f, 0.5f)] private float _letterChangeInterval = 0.07f;
        [SerializeField] private float _pauseOnTargetSec = 0.9f;

        [Tooltip("Символ-«муха» (частый)")]
        [SerializeField] private char _decoyChar = 'Ё';
        [Tooltip("Символ-«цель» (редкий)")]
        [SerializeField] private char _targetChar = 'Е';

        [Header("Weights")]
        [Tooltip("Вероятность показать Ё (0..1). Остальное — Е")]
        [SerializeField, Range(0.5f, 0.99f)] private float _decoyProbability = 0.86f;

        [Header("Timer & Penalties")]
        [SerializeField] private float _timeLimitSec = 25f;
        [SerializeField] private float _wrongClickPenaltySec = 2.5f;

        [Header("Hints")]
        [SerializeField] private int _hintCount = 2;
        [SerializeField] private float _hintFlashSec = 0.8f;

        [Header("Theme")]
        [SerializeField] private Color _normalTextColor = Color.white;
        [SerializeField] private Color _hintTextColor = new Color(1f, 0.95f, 0.4f);
        [SerializeField] private Color _winTint = new Color(0.3f, 0.9f, 0.5f, 1f);
        
        [SerializeField] private Button _backButton;

        public event Action OnBackClicked;

        public UnityEvent OnWinEvent = new UnityEvent();
        public UnityEvent OnLoseEvent = new UnityEvent();
        
        private readonly List<SixthLetterButtonElement> _tiles = new();
        private bool _isRunning;
        private bool _isFinished;
        private float _timeLeft;
        private RectTransform _parentRt;

        private void Awake()
        {
            _parentRt = _parent == null ? transform as RectTransform : _parent;

            if (_hintButton != null)
                _hintButton.onClick.AddListener(TryHint);
            
            _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        }

        private void Start()
        {
            SpawnAll();
            StartGame();
        }

        private void Update()
        {
            if (!_isRunning || _isFinished) return;

            _timeLeft -= Time.deltaTime;
            UpdateTimerVisual();

            if (_timeLeft <= 0f)
            {
                _timeLeft = 0f;
                Lose();
            }

            // Альтернативное управление подсказкой с клавиатуры
            if (Input.GetKeyDown(KeyCode.H))
                TryHint();
        }

        private void UpdateTimerVisual()
        {
            if (_timerText == null) return;
            _timerText.text = Mathf.CeilToInt(_timeLeft).ToString();
        }

        private void SpawnAll()
        {
            ClearAll();

            Vector2 startAnchorPos = _spawnOrigin != null ? _spawnOrigin.anchoredPosition : Vector2.zero;

            float cellW = _elementSize.x;
            float cellH = _elementSize.y;
            float stepX = cellW + _spacing.x;
            float stepY = cellH + _spacing.y;

            int columns;
            if (_autoComputeColumns)
            {
                float availableWidth = _parentRt.rect.width - startAnchorPos.x;
                columns = Mathf.Max(1, Mathf.FloorToInt((availableWidth + _spacing.x) / stepX));
            }
            else
            {
                columns = Mathf.Max(1, _manualColumns);
            }

            for (int i = 0; i < _spawnCount; i++)
            {
                var tile = Instantiate(_tilePrefab, _parentRt);
                var rt = tile.transform as RectTransform;

                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = _elementSize;

                int row = i / columns;
                int col = i % columns;
                rt.anchoredPosition = new Vector2(
                    startAnchorPos.x + col * stepX,
                    startAnchorPos.y - row * stepY
                );

                // Входная анимация — падение + лёгкий наклон
                var startPos = rt.anchoredPosition + new Vector2(0f, 30f);
                rt.anchoredPosition = startPos;
                rt.localRotation = Quaternion.Euler(0, 0, Random.Range(-6f, 6f));
                rt.DOAnchorPosY(rt.anchoredPosition.y - 30f, 0.22f)
                  .SetRelative(false)
                  .SetEase(Ease.OutCubic)
                  .SetDelay(Random.Range(0f, 0.08f));

                tile.ElementClicked += OnTileClicked;

                tile.Init(
                    decoy: _decoyChar,
                    target: _targetChar,
                    changeInterval: _letterChangeInterval,
                    pauseOnTargetSec: _pauseOnTargetSec,
                    decoyProbability: _decoyProbability,
                    normalTextColor: _normalTextColor
                );

                _tiles.Add(tile);
            }
        }

        private void StartGame()
        {
            if (_isRunning) return;
            _isRunning = true;
            _isFinished = false;
            _timeLeft = _timeLimitSec;
            UpdateTimerVisual();

            if (_wiggleContainer && _parentRt != null)
            {
                _parentRt.DOKill();
                _parentRt
                    .DOShakeAnchorPos(2.6f, new Vector2(0f, 10f), 10, 80f, false, true)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }

            // Popup не трогаем: его видимость управляется самим GamePopup

            foreach (var t in _tiles) t.StartLoop();
        }

        private void OnTileClicked(SixthLetterButtonElement tile, char currentChar)
        {
            if (_isFinished) return;

            if (currentChar == _targetChar)
            {
                Win(tile);
            }
            else
            {
                // штраф за промах
                _timeLeft = Mathf.Max(0f, _timeLeft - _wrongClickPenaltySec);
                UpdateTimerVisual();
                // иной отклик на промах
                tile.MissFeedback();
            }
        }

        private void Win(SixthLetterButtonElement winner)
        {
            _isFinished = true;
            _isRunning = false;

            foreach (var t in _tiles) t.StopLoop();

            _parentRt?.DOKill();
            _parentRt?.DOShakeAnchorPos(0.5f, new Vector2(22f, 28f), 20, 100f, false, true);

            if (winner != null)
            {
                DOTween.Sequence()
                    .Append(winner.transform.DOPunchScale(Vector3.one * 0.38f, 0.35f, 24, 1.0f))
                    .Join(winner.transform.DORotate(new Vector3(0, 0, 400f), 0.58f, RotateMode.FastBeyond360)
                    .SetEase(Ease.OutBack));
            }

            // Попап победы
            if (_winPopup != null)
            {
                _winPopup.Show("Победа!");
            }

            // лёгкая цветовая вспышка всем плиткам
            foreach (var t in _tiles) t.FlashTint(_winTint, 0.25f);
            
            OnWinEvent?.Invoke(); 
        }

        private void Lose()
        {
            if (_isFinished) return;
            _isFinished = true;
            _isRunning = false;

            foreach (var t in _tiles) t.StopLoop();
            _parentRt?.DOKill();

            // Попап проигрыша
            if (_winPopup != null)
            {
                _winPopup.Show("Время вышло!");
            }
            
            OnLoseEvent?.Invoke();
        }

        private void TryHint()
        {
            if (!_isRunning || _isFinished || _hintCount <= 0) return;
            _hintCount--;

            foreach (var t in _tiles) t.TryHintReveal(_targetChar, _hintTextColor, _hintFlashSec);

            // Небольшой общий импульс
            _parentRt?.DOPunchAnchorPos(new Vector2(0f, 12f), 0.18f, 12, 1f);
        }

        private void ClearAll()
        {
            foreach (var t in _tiles)
            {
                if (t != null)
                {
                    t.ElementClicked -= OnTileClicked;
                    if (t.gameObject != null) Destroy(t.gameObject);
                }
            }
            _tiles.Clear();
        }

        private void OnDestroy()
        {
            ClearAll();
            _parentRt?.DOKill();

            if (_hintButton != null)
                _hintButton.onClick.RemoveListener(TryHint);
        }
    }
}
