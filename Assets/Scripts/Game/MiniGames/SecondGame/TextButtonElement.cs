using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Game.MiniGames.SecondGame
{
    public class TextButtonElement : MonoBehaviour
    {
        [SerializeField] private Button _textButton;
        [SerializeField] private TMP_Text _textElement;

        public event Action<TextButtonElement, char> ElementClicked;

        public char CurrentChar { get; private set; }

        // Настройки приходят из SecondGame
        private string _alphabet;
        private float _changeInterval;
        private float _pauseOnTargetSec;
        private char _targetChar;
        private bool _running;
        private Tween _pulseTween;
        private Coroutine _loopRoutine;

        public void Init(string alphabet, float changeInterval, float pauseOnTargetSec, char targetChar, float randomPhase = 0f)
        {
            _alphabet = alphabet;
            _changeInterval = changeInterval;
            _pauseOnTargetSec = pauseOnTargetSec;
            _targetChar = targetChar;

            // Базовая пульс-анимация через DOTween
            _pulseTween?.Kill();
            transform.localScale = Vector3.one;
            _pulseTween = transform
                .DOScale(1.08f, 0.35f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetAutoKill(false);

            // Немного рандомной расфазы, чтобы все не моргали синхронно
            if (randomPhase > 0f)
            {
                _pulseTween.Goto(UnityEngine.Random.Range(0f, randomPhase), true);
            }

            StartLoop();
        }

        private void OnEnable()
        {
            if (_textButton != null)
                _textButton.onClick.AddListener(OnButtonClicked);
        }

        private void OnDisable()
        {
            if (_textButton != null)
                _textButton.onClick.RemoveListener(OnButtonClicked);
        }

        private void OnDestroy()
        {
            _pulseTween?.Kill();
        }

        public void StartLoop()
        {
            if (_running) return;
            _running = true;
            _loopRoutine = StartCoroutine(LettersLoop());
        }

        public void StopLoop()
        {
            if (!_running) return;
            _running = false;
            if (_loopRoutine != null)
            {
                StopCoroutine(_loopRoutine);
                _loopRoutine = null;
            }
        }

        private IEnumerator LettersLoop()
        {
            var wait = new WaitForSeconds(_changeInterval);
            var chars = _alphabet.ToCharArray();
            int idx = UnityEngine.Random.Range(0, chars.Length); // старт с произвольной буквы

            while (_running)
            {
                CurrentChar = chars[idx];
                if (_textElement != null) _textElement.text = CurrentChar.ToString();

                // Если выпала целевая буква — ставим анимацию на паузу и ждём 1 сек
                if (CurrentChar == _targetChar)
                {
                    // Небольшой визуальный «выстрел»
                    _pulseTween?.Pause();
                    // Подсветим элемент цветом и слегка «подпрыгнем»
                    var seq = DOTween.Sequence();
                    if (_textElement != null)
                    {
                        var baseColor = _textElement.color;
                        seq.Append(_textElement.DOColor(new Color(baseColor.r, baseColor.g, baseColor.b, 1f), 0f));
                        seq.Join(_textElement.DOFade(1f, 0f));
                        seq.Join(_textElement.transform.DOScale(1.2f, 0.15f).SetEase(Ease.OutBack));
                        seq.AppendInterval(0.15f);
                        seq.Append(_textElement.transform.DOScale(1.0f, 0.2f).SetEase(Ease.InOutSine));
                    }
                    // Лёгкая тряска
                    seq.Join(transform.DOShakeRotation(0.4f, 8f, 20, 90f, false, ShakeRandomnessMode.Harmonic));

                    yield return new WaitForSeconds(_pauseOnTargetSec);
                    _pulseTween?.Play();
                }
                else
                {
                    yield return wait;
                }

                idx++;
                if (idx >= chars.Length) idx = 0;
            }
        }

        private void OnButtonClicked()
        {
            ElementClicked?.Invoke(this, CurrentChar);

            // Небольшой отклик клика
            transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 12, 0.8f);
        }
    }
}
