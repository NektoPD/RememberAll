using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

namespace Game.MiniGames.SecondGame
{
    public class SecondGame : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private TextButtonElement _textButtonElementPrefab;
        [SerializeField] private RectTransform _parent;

        [Header("Layout")]
        [Tooltip("Точка старта раскладки. Рекомендуется привязать к верхнему левому углу контейнера.")]
        [SerializeField] private RectTransform _spawnOrigin;
        [Tooltip("Размер каждой кнопки (RectTransform.sizeDelta).")]
        [SerializeField] private Vector2 _elementSize = new Vector2(160, 60);
        [Tooltip("Отступы между кнопками X/Y.")]
        [SerializeField] private Vector2 _spacing = new Vector2(16, 12);
        [Tooltip("Если включено — количество колонок высчитывается по ширине контейнера.")]
        [SerializeField] private bool _autoComputeColumns = true;
        [Tooltip("Если автоподбор выключен — используем фиксированное число колонок.")]
        [SerializeField] private int _manualColumns = 10;

        [Header("Gameplay")]
        [SerializeField] private int _spawnCount = 50;

        [Tooltip("Скорость смены букв В ЭТОМ МИНИ-ИГРОВОМ ПРЕФАБЕ. Настраивается из инспектора.")]
        [SerializeField, Range(0.02f, 0.5f)]
        private float _letterChangeInterval = 0.08f;

        [SerializeField] private float _pauseOnTargetSec = 1.0f;
        [SerializeField] private char _targetChar = 'Б';

        [Tooltip("Только кириллица. Можно редактировать из инспектора.")]
        [SerializeField] private string _alphabet = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";

        [Header("FX")]
        [SerializeField] private CanvasGroup _winOverlay;

        private readonly List<TextButtonElement> _spawned = new();
        private bool _isRunning;
        private bool _isFinished;
        
        public UnityEvent OnWinEvent = new UnityEvent();

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(_alphabet))
                _alphabet = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
            if (!_alphabet.Contains(_targetChar.ToString()))
                _alphabet = _alphabet.Insert(1, _targetChar.ToString());
        }

        private void Start()
        {
            SpawnAll();
            StartGame();
        }

        private void SpawnAll()
        {
            ClearAll();

            // Готовим геометрию раскладки
            var parentRt = _parent;
            if (parentRt == null) parentRt = transform as RectTransform;

            // Стартовая точка относительно родителя
            Vector2 startAnchorPos = Vector2.zero;
            if (_spawnOrigin != null)
                startAnchorPos = _spawnOrigin.anchoredPosition;

            float cellW = _elementSize.x;
            float cellH = _elementSize.y;
            float stepX = cellW + _spacing.x;
            float stepY = cellH + _spacing.y;

            // Вычисляем кол-во колонок
            int columns;
            if (_autoComputeColumns)
            {
                float availableWidth = parentRt.rect.width - startAnchorPos.x;
                columns = Mathf.Max(1, Mathf.FloorToInt((availableWidth + _spacing.x) / stepX));
            }
            else
            {
                columns = Mathf.Max(1, _manualColumns);
            }

            for (int i = 0; i < _spawnCount; i++)
            {
                var elem = Instantiate(_textButtonElementPrefab, _parent);

                // Настраиваем RectTransform элемента (якорим к верхнему левому углу)
                var rt = elem.transform as RectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = _elementSize;

                // Позиция в сетке
                int row = i / columns;
                int col = i % columns;
                Vector2 pos = new Vector2(
                    startAnchorPos.x + col * stepX,
                    startAnchorPos.y - row * stepY
                );
                rt.anchoredPosition = pos;

                // Лёгкая входная анимация
                var startScale = Vector3.one * Random.Range(0.9f, 0.98f);
                var endScale = Vector3.one;
                rt.localScale = startScale;
                rt.DOScale(endScale, 0.22f).SetEase(Ease.OutBack).SetDelay(Random.Range(0f, 0.08f));

                // Подписка и запуск логики
                elem.ElementClicked += OnElementClicked;
                elem.Init(_alphabet, _letterChangeInterval, _pauseOnTargetSec, _targetChar, randomPhase: 1.0f);

                _spawned.Add(elem);
            }
        }

        private void StartGame()
        {
            if (_isRunning) return;
            _isRunning = true;
            _isFinished = false;

            if (_parent != null)
            {
                _parent.DOKill();
                _parent.DOShakeAnchorPos(3f, new Vector2(8f, 5f), 10, 90f, false, true)
                       .SetLoops(-1, LoopType.Yoyo)
                       .SetEase(Ease.InOutSine);
            }

            if (_winOverlay != null)
            {
                _winOverlay.alpha = 0f;
                _winOverlay.gameObject.SetActive(false);
            }
        }

        private void OnElementClicked(TextButtonElement element, char currentChar)
        {
            if (_isFinished) return;

            if (currentChar == _targetChar)
                Win(element);
            else
                element.transform.DOShakeScale(0.2f, 0.2f, 18, 90f, true, ShakeRandomnessMode.Harmonic);
        }

        private void Win(TextButtonElement winner)
        {
            _isFinished = true;
            _isRunning = false;

            foreach (var e in _spawned) e.StopLoop();

            var parentRt = _parent != null ? _parent : (winner != null ? winner.transform.parent as RectTransform : null);
            parentRt?.DOKill();
            parentRt?.DOShakeAnchorPos(0.6f, new Vector2(25f, 18f), 20, 100f, false, true);

            if (winner != null)
            {
                DOTween.Sequence()
                    .Append(winner.transform.DOPunchScale(Vector3.one * 0.35f, 0.35f, 24, 1.0f))
                    .Join(winner.transform.DORotate(new Vector3(0, 0, 360f), 0.6f, RotateMode.FastBeyond360).SetEase(Ease.OutBack));
            }

            if (_winOverlay != null)
            {
                _winOverlay.gameObject.SetActive(true);
                _winOverlay.DOFade(1f, 0.35f);
            }
            
            OnWinEvent?.Invoke();
        }

        private void ClearAll()
        {
            foreach (var e in _spawned)
            {
                if (e != null)
                {
                    e.ElementClicked -= OnElementClicked;
                    if (e.gameObject != null) Destroy(e.gameObject);
                }
            }
            _spawned.Clear();
        }

        private void OnDestroy()
        {
            ClearAll();
            _parent?.DOKill();
        }
    }
}
