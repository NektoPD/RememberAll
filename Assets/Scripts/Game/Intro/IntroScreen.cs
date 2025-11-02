using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Game.Intro
{
    public class IntroScreen : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text _introText;

        [Header("Anim")]
        [SerializeField] private float _animDuration = 0.6f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Ease _hideEase = Ease.InBack;

        [Header("Typewriter")]
        [SerializeField] private float _charDelay = 0.04f;        // задержка между символами
        [SerializeField] private float _fastFactor = 0.15f;       // во сколько раз быстрее при ускорении
        [SerializeField] private float _afterLinePause = 0.35f;   // пауза после строки, если не нажимают Enter

        public event Action OnIntroFinished;               // событие завершения интро

        private int _currentTextIndex;
        private Vector2 _onScreenPos;
        private Vector2 _offScreenPos;

        private bool _isTyping;
        private bool _fastForward;
        private bool _skipLine;
        private bool _isShown;

        protected IEnumerator _fillingCoroutine;

        private readonly string[] _texts = new[]
        {
            "Я забыл",
            "Я забыл их всех",
            "Помню только",
            "Щ"
        };

        private void Awake()
        {
            // Запоминаем целевую позицию и рассчитываем позицию "снизу за экраном"
            _onScreenPos = _panel.anchoredPosition;
            float offY = _onScreenPos.y - (Screen.height + _panel.rect.height);
            _offScreenPos = new Vector2(_onScreenPos.x, offY);

            // Стартуем панель за экраном и пустой текст
            _panel.anchoredPosition = _offScreenPos;
            _introText.text = string.Empty;
            _introText.maxVisibleCharacters = 0;
            gameObject.SetActive(false);
            
            ShowIntro();
        }

        private void Update()
        {
            if (!_isShown) return;

            // Enter — ускорить, затем — раскрыть строку целиком, затем — перейти дальше/закрыть
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (_isTyping)
                {
                    if (!_fastForward)
                    {
                        _fastForward = true;           // первое нажатие — ускорение
                    }
                    else
                    {
                        _skipLine = true;              // второе — мгновенно показать всю строку
                    }
                }
                else
                {
                    // Если строка уже показана — сразу к следующей
                    ProceedNext();
                }
            }
        }

        public void ShowIntro()
        {
            gameObject.SetActive(true);
            _isShown = true;

            // Подготовка состояния
            _panel.anchoredPosition = _offScreenPos;
            _currentTextIndex = 0;
            _introText.text = string.Empty;
            _introText.maxVisibleCharacters = 0;

            // Въезд панели
            _panel
                .DOAnchorPos(_onScreenPos, _animDuration)
                .SetEase(_showEase)
                .OnComplete(() =>
                {
                    // Начинаем первую строку
                    StartTypingCurrent();
                });
        }

        private void StartTypingCurrent()
        {
            if (_fillingCoroutine != null)
            {
                StopCoroutine(_fillingCoroutine);
                _fillingCoroutine = null;
            }

            if (_currentTextIndex >= _texts.Length)
            {
                // Все строки показаны — закрываем экран
                HideAndFinish();
                return;
            }

            _fillingCoroutine = FillTextBox(_texts[_currentTextIndex]);
            StartCoroutine(_fillingCoroutine);
        }

        private IEnumerator FillTextBox(string line)
        {
            _isTyping = true;
            _fastForward = false;
            _skipLine = false;

            _introText.text = line;
            _introText.ForceMeshUpdate();
            int total = _introText.textInfo.characterCount;
            _introText.maxVisibleCharacters = 0;

            int visible = 0;
            while (visible < total)
            {
                if (_skipLine)
                {
                    visible = total;
                    _introText.maxVisibleCharacters = visible;
                    break;
                }

                visible++;
                _introText.maxVisibleCharacters = visible;

                float delay = _fastForward ? _charDelay * _fastFactor : _charDelay;
                yield return new WaitForSeconds(delay);
            }

            _isTyping = false;

            // Небольшая пауза, если пользователь не жмет Enter — иначе он может нажать и пойти дальше мгновенно
            float t = 0f;
            while (t < _afterLinePause && !_isTyping)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    break;

                t += Time.deltaTime;
                yield return null;
            }

            ProceedNext();
        }

        private void ProceedNext()
        {
            if (_isTyping) return;

            _currentTextIndex++;

            if (_currentTextIndex >= _texts.Length)
            {
                HideAndFinish();
            }
            else
            {
                StartTypingCurrent();
            }
        }

        private void HideAndFinish()
        {
            // Выезд панели обратно за экран
            _panel
                .DOAnchorPos(_offScreenPos, _animDuration)
                .SetEase(_hideEase)
                .OnComplete(() =>
                {
                    _isShown = false;
                    OnIntroFinished?.Invoke();  // сообщаем, что интро завершилось
                    gameObject.SetActive(false);
                });
        }
    }
}
