using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MainMenu
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private MenuPopup _menuPopup;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;

        private void Awake()
        {
            _settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        private void OnDestroy()
        {
            _settingsButton.onClick.RemoveListener(OnSettingsClicked);
        }

        private void OnSettingsClicked()
        {
            if (_menuPopup == null) return;
            _menuPopup.Open(); // внутри попап сам сделает SetActive + анимацию
        }
    }
}