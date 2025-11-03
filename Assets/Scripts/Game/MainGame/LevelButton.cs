using System;
using Game.MainGame.DTO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace Game.MainGame
{
    public class LevelButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TMP_Text _levelTypeText;
        [SerializeField] private Button _button;

        [Header("Визуал")]
        [Tooltip("Материал с эффектом шума/глитча для закрытых уровней (TMP)")]
        [SerializeField] private Material _lockedNoiseMaterial;
        [Tooltip("Обычный материал TMP для открытых уровней (если пусто — берётся текущий)")]
        [SerializeField] private Material _unlockedMaterial;
        [Tooltip("Множитель пульсации при наведении")]
        [SerializeField] private float _hoverScale = 1.08f;
        [SerializeField] private float _hoverDuration = 0.25f;
        [SerializeField] private Ease _hoverEase = Ease.OutSine;

        [field: SerializeField] public LevelData LevelData { get; private set; }

        public event Action<LevelType> OnOpenClicked;

        private Tweener _pulseTween;
        private Vector3 _initialScale;
        private Material _initialMaterial;

        private void Awake()
        {
            if (_levelTypeText == null) _levelTypeText = GetComponentInChildren<TMP_Text>(true);
            if (_button == null) _button = GetComponentInChildren<Button>(true);

            _initialScale = transform.localScale;
            _initialMaterial = _levelTypeText ? _levelTypeText.fontMaterial : null;

            if (_button)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(HandleClick);
            }
        }

        public void Initialize(LevelData data)
        {
            LevelData = data ?? throw new ArgumentNullException(nameof(data));
            ApplyText();
            RefreshVisual();
        }

        public void RefreshVisual()
        {
            bool unlocked = LevelData != null && LevelData.IsUnlocked;

            if (_levelTypeText)
            {
                // Материал
                if (unlocked)
                {
                    var mat = _unlockedMaterial ? _unlockedMaterial : _initialMaterial;
                    if (mat) _levelTypeText.fontMaterial = mat;
                    _levelTypeText.alpha = 1f;
                }
                else
                {
                    if (_lockedNoiseMaterial)
                    {
                        _levelTypeText.fontMaterial = _lockedNoiseMaterial;
                    }
                    else
                    {
                        // «псевдо-замочек»: делаем бледным, если нет шейдера
                        _levelTypeText.alpha = 0.5f;
                    }
                }
            }

            // Кнопка кликабельна только если открыт
            if (_button) _button.interactable = unlocked;
        }

        private void ApplyText()
        {
            if (_levelTypeText && LevelData != null)
            {
                // Имя берём напрямую из enum (в нём русские буквы)
                _levelTypeText.text = LevelData.LevelType.ToString();
            }
        }

        private void HandleClick()
        {
            if (LevelData == null) return;
            if (!LevelData.IsUnlocked) return; // на всякий случай

            OnOpenClicked?.Invoke(LevelData.LevelType);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (LevelData == null || !LevelData.IsUnlocked) return;

            _pulseTween?.Kill();
            _pulseTween = transform
                .DOScale(_initialScale * _hoverScale, _hoverDuration)
                .SetEase(_hoverEase)
                .SetUpdate(false)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _pulseTween?.Kill();
            _pulseTween = null;
            transform.DOScale(_initialScale, 0.12f).SetEase(Ease.OutSine);
        }

        private void OnDisable()
        {
            _pulseTween?.Kill();
            _pulseTween = null;
            if (transform) transform.localScale = _initialScale;
        }
    }
}
