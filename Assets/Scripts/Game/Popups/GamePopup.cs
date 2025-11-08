using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Game.Popups
{
    public class GamePopup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _frame;
        [SerializeField] private TMP_Text _text;

        [SerializeField] private float _animationDuration = 0.25f;

        private Tween _current;

        public event Action Shown;
        public event Action Hidden;

        public void Show(string text = null, float autoHideAfterSeconds = -1f)
        {
            if (!string.IsNullOrEmpty(text) && _text != null)
                _text.text = text;

            _current?.Kill();
            _current = null;

            gameObject.SetActive(true);
            _frame.alpha = 0f;
            _frame.interactable = false;
            _frame.blocksRaycasts = false;

            var seq = DOTween.Sequence();
            seq.Append(_frame.DOFade(1f, _animationDuration)
                .OnComplete(() =>
                {
                    // интерактив разрешаем, когда попап полностью виден
                    _frame.interactable = true;
                    _frame.blocksRaycasts = true;
                    Shown?.Invoke();
                }));

            if (autoHideAfterSeconds > 0f)
            {
                seq.AppendInterval(autoHideAfterSeconds);
                seq.Append(_frame.DOFade(0f, _animationDuration));
                seq.OnComplete(() =>
                {
                    _frame.interactable = false;
                    _frame.blocksRaycasts = false;
                    gameObject.SetActive(false);
                    Hidden?.Invoke();
                });
            }

            _current = seq;
        }

        public void Hide()
        {
            _current?.Kill();
            _frame.interactable = false;
            _frame.blocksRaycasts = false;

            _current = _frame.DOFade(0f, _animationDuration)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    Hidden?.Invoke();
                });
        }
    }
}
