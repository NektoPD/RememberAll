using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public abstract class BaseTypewriterScreen : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text _text;

        [Header("Anim")]
        [SerializeField] private float _animDuration = 0.6f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Ease _hideEase = Ease.InBack;

        [Header("Typewriter")]
        [SerializeField] private float _charDelay = 0.04f;
        [SerializeField] private float _fastFactor = 0.15f;
        [SerializeField] private float _afterLinePause = 0.35f;

        public event Action Finished;

        [TextArea] [SerializeField] private string[] _lines;
        private int _idx;
        private Vector2 _onPos, _offPos;
        private bool _shown, _typing, _fast, _skip;
        private IEnumerator _cr;

        protected virtual void Awake()
        {
            _onPos = _panel.anchoredPosition;
            float offY = _onPos.y - (Screen.height + _panel.rect.height);
            _offPos = new Vector2(_onPos.x, offY);

            _panel.anchoredPosition = _offPos;
            _text.text = string.Empty;
            _text.maxVisibleCharacters = 0;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_shown) return;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (_typing)
                {
                    if (!_fast) _fast = true;
                    else _skip = true;
                }
                else ProceedNext();
            }
        }

        public void ShowScreen(string[] linesOverride = null)
        {
            if (linesOverride != null && linesOverride.Length > 0) _lines = linesOverride;
            _idx = 0;
            _text.text = string.Empty;
            _text.maxVisibleCharacters = 0;

            gameObject.SetActive(true);
            _shown = true;

            _panel.DOAnchorPos(_onPos, _animDuration).SetEase(_showEase)
                .OnComplete(StartTypingCurrent);
        }

        private void StartTypingCurrent()
        {
            if (_cr != null) StopCoroutine(_cr);
            if (_idx >= _lines.Length) { HideAndFinish(); return; }
            _cr = Fill(_lines[_idx]);
            StartCoroutine(_cr);
        }

        private IEnumerator Fill(string line)
        {
            _typing = true; _fast = false; _skip = false;

            _text.text = line;
            _text.ForceMeshUpdate();
            int total = _text.textInfo.characterCount;
            _text.maxVisibleCharacters = 0;

            int vis = 0;
            while (vis < total)
            {
                if (_skip) { vis = total; _text.maxVisibleCharacters = vis; break; }
                vis++;
                _text.maxVisibleCharacters = vis;
                yield return new WaitForSeconds(_fast ? _charDelay * _fastFactor : _charDelay);
            }

            _typing = false;
            float t = 0f;
            while (t < _afterLinePause && !_typing)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) break;
                t += Time.deltaTime; yield return null;
            }
            ProceedNext();
        }

        private void ProceedNext()
        {
            if (_typing) return;
            _idx++;
            if (_idx >= _lines.Length) HideAndFinish();
            else StartTypingCurrent();
        }

        private void HideAndFinish()
        {
            _panel.DOAnchorPos(_offPos, _animDuration).SetEase(_hideEase)
                .OnComplete(() =>
                {
                    _shown = false;
                    gameObject.SetActive(false);
                    Finished?.Invoke();
                });
        }
    }
}