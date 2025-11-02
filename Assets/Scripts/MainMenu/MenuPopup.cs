using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace MainMenu
{
    public class MenuPopup : MonoBehaviour
    {
        private const string MusicVolumeKey = "MusicVolume"; // PlayerPrefs key + имя exposed параметра
        private const string SFXVolumeKey   = "SFXVolume";   // PlayerPrefs key + имя exposed параметра

        [Header("UI")]
        [SerializeField] private RectTransform _panelToAnimate;
        [SerializeField] private Button _closeButton;

        [SerializeField] private Slider _musicSlider;
        [SerializeField] private TMP_Text _musicVolumeAmount;

        [SerializeField] private Slider _soundlider; // оставляем имя, как в вашем коде
        [SerializeField] private TMP_Text _soundVolumeAmount;

        [Header("Audio")]
        [SerializeField] private AudioMixer _musicMixer; // должен иметь exposed параметр "MusicVolume"
        [SerializeField] private AudioMixer _sfxMixer;   // должен иметь exposed параметр "SFXVolume"

        [Header("Animation")]
        [SerializeField] private float _animDuration = 0.25f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField] private Ease _hideEase = Ease.InBack;

        private bool _isAnimating;

        // -------- Public API --------
        public void Open()
        {
            if (gameObject.activeSelf && _panelToAnimate.localScale.x > 0.99f) return;

            gameObject.SetActive(true);

            // Сбросим масштаб и запустим анимацию раскрытия
            _panelToAnimate.DOKill(true);
            _panelToAnimate.localScale = Vector3.zero;
            _isAnimating = true;

            _panelToAnimate
                .DOScale(1f, _animDuration)
                .SetEase(_showEase)
                .OnComplete(() => _isAnimating = false);
        }

        // Закрытие через кнопку вызовет это
        private void Close()
        {
            if (_isAnimating) return;

            _panelToAnimate.DOKill(true);
            _isAnimating = true;

            _panelToAnimate
                .DOScale(0f, _animDuration)
                .SetEase(_hideEase)
                .OnComplete(() =>
                {
                    _isAnimating = false;
                    gameObject.SetActive(false); // выключаем после анимации
                });
        }

        // -------- Unity lifecycle --------
        private void Awake()
        {
            // Листенеры
            if (_closeButton) _closeButton.onClick.AddListener(Close);

            if (_musicSlider)
                _musicSlider.onValueChanged.AddListener(SetMusicFromSlider);

            if (_soundlider)
                _soundlider.onValueChanged.AddListener(SetSfxFromSlider);
        }

        private void OnDestroy()
        {
            if (_closeButton) _closeButton.onClick.RemoveListener(Close);

            if (_musicSlider)
                _musicSlider.onValueChanged.RemoveListener(SetMusicFromSlider);

            if (_soundlider)
                _soundlider.onValueChanged.RemoveListener(SetSfxFromSlider);
        }

        private void OnEnable()
        {
            // Загружаем сохранённые значения (линейные 0..1). По умолчанию — 1.
            float musicLin = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
            float sfxLin   = PlayerPrefs.GetFloat(SFXVolumeKey, 1f);

            // Применяем к слайдерам (это вызовет коллбеки и применит к миксерам)
            if (_musicSlider) _musicSlider.SetValueWithoutNotify(musicLin);
            if (_soundlider)  _soundlider.SetValueWithoutNotify(sfxLin);

            // Обновим руками, чтобы тексты и миксеры были сразу выставлены
            SetMusicFromSlider(musicLin);
            SetSfxFromSlider(sfxLin);

            // На всякий случай подготовим панель
            if (_panelToAnimate)
            {
                _panelToAnimate.DOKill(true);
                _panelToAnimate.localScale = Vector3.one; // если открываем через Open(), он сам сменит на 0 -> 1
            }
        }

        private void OnDisable()
        {
            _panelToAnimate?.DOKill(true);
        }

        // -------- Volume logic --------
        private void SetMusicFromSlider(float linear)
        {
            ApplyLinearToMixer(_musicMixer, MusicVolumeKey, linear);
            UpdateText(_musicVolumeAmount, linear);
            PlayerPrefs.SetFloat(MusicVolumeKey, linear);
        }

        private void SetSfxFromSlider(float linear)
        {
            ApplyLinearToMixer(_sfxMixer, SFXVolumeKey, linear);
            UpdateText(_soundVolumeAmount, linear);
            PlayerPrefs.SetFloat(SFXVolumeKey, linear);
        }

        private static void ApplyLinearToMixer(AudioMixer mixer, string exposedParam, float linear)
        {
            if (mixer == null) return;

            // Преобразование 0..1 в dB: избегаем -∞, ставим нижний потолок
            const float minDb = -80f;
            float dB;

            if (linear <= 0.0001f)
                dB = minDb;
            else
                dB = Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;

            mixer.SetFloat(exposedParam, dB);
        }

        private static void UpdateText(TMP_Text label, float linear)
        {
            if (!label) return;
            int percent = Mathf.RoundToInt(linear * 100f);
            label.text = $"{percent}%";
        }
    }
}
