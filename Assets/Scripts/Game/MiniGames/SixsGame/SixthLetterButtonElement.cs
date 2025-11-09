using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Game.MiniGames.SixthGame
{
    public class SixthLetterButtonElement : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _text;

        public event Action<SixthLetterButtonElement, char> ElementClicked;

        public char CurrentChar { get; private set; }

        // Настройка из SixthGame
        private char _decoy;
        private char _target;
        private float _interval;
        private float _pauseOnTargetSec;
        private float _decoyProb;
        private bool _running;
        private Coroutine _loop;
        private Tween _swingTween;

        private Color _baseTextColor;

        public void Init(char decoy, char target, float changeInterval, float pauseOnTargetSec, float decoyProbability, Color normalTextColor)
        {
            _decoy = decoy;
            _target = target;
            _interval = changeInterval;
            _pauseOnTargetSec = pauseOnTargetSec;
            _decoyProb = Mathf.Clamp01(decoyProbability);

            _baseTextColor = normalTextColor;
            if (_text != null) _text.color = _baseTextColor;

            // Вместо пульса — лёгкий маятник вращения
            _swingTween?.Kill();
            transform.localRotation = Quaternion.identity;
            _swingTween = transform
                .DOLocalRotate(new Vector3(0, 0, 6f), 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetAutoKill(false);
            _swingTween.Goto(UnityEngine.Random.Range(0f, 1f), true); // рассинхрон

            StartLoop();
        }

        private void OnEnable()
        {
            if (_button != null)
                _button.onClick.AddListener(OnClicked);
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClicked);
        }

        private void OnDestroy()
        {
            _swingTween?.Kill();
        }

        public void StartLoop()
        {
            if (_running) return;
            _running = true;
            _loop = StartCoroutine(LettersLoop());
        }

        public void StopLoop()
        {
            if (!_running) return;
            _running = false;
            if (_loop != null)
            {
                StopCoroutine(_loop);
                _loop = null;
            }
        }

        private IEnumerator LettersLoop()
        {
            var wait = new WaitForSeconds(_interval);

            while (_running)
            {
                // Взвешенный выбор
                bool showDecoy = UnityEngine.Random.value < _decoyProb;
                CurrentChar = showDecoy ? _decoy : _target;

                if (_text != null) _text.text = CurrentChar.ToString();

                if (CurrentChar == _target)
                {
                    // Небольшой «всплеск» при появлении цели
                    var seq = DOTween.Sequence();
                    if (_text != null)
                    {
                        seq.Join(_text.transform.DOPunchScale(Vector3.one * 0.12f, 0.2f, 12, 0.9f));
                    }
                    // Пауза подлиннее — как «окно возможности»
                    yield return new WaitForSeconds(_pauseOnTargetSec);
                }
                else
                {
                    yield return wait;
                }
            }
        }

        private void OnClicked()
        {
            ElementClicked?.Invoke(this, CurrentChar);

            // щелчок — пружинка
            transform.DOPunchScale(Vector3.one * 0.14f, 0.18f, 12, 0.9f);
        }

        // Внешние вызовы — визуальные реакции
        public void MissFeedback()
        {
            // Иная реакция на промах: краткий «wiggle» + лёгкая смена прозрачности текста
            DOTween.Sequence()
                .Join(transform.DOShakeRotation(0.18f, 10f, 24, 100f, false, ShakeRandomnessMode.Harmonic))
                .Join(_text != null ? _text.DOFade(0.6f, 0.09f).OnComplete(() => _text.DOFade(1f, 0.09f)) : null);
        }

        public void FlashTint(Color tint, float dur)
        {
            if (_text == null) return;
            var baseCol = _baseTextColor;
            DOTween.Sequence()
                .Append(_text.DOColor(tint, dur * 0.5f))
                .Append(_text.DOColor(baseCol, dur * 0.5f));
        }

        public void TryHintReveal(char target, Color hintColor, float showSec)
        {
            if (_text == null) return;
            if (CurrentChar != target) return;

            var baseCol = _text.color;
            DOTween.Sequence()
                .Append(_text.DOColor(hintColor, 0.05f))
                .Join(_text.transform.DOPunchScale(Vector3.one * 0.12f, 0.18f, 10, 1f))
                .AppendInterval(showSec * 0.85f)
                .Append(_text.DOColor(_baseTextColor, 0.12f));
        }
    }
}
