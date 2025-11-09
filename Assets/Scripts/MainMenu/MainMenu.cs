using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            _playButton.onClick.AddListener(OnPlayButtonCLicked);
        }

        private void OnDestroy()
        {
            _playButton.onClick.RemoveAllListeners();
            _settingsButton.onClick.RemoveListener(OnSettingsClicked);
        }

        private void OnSettingsClicked()
        {
            if (_menuPopup == null) return;
            _menuPopup.Open(); // внутри попап сам сделает SetActive + анимацию
        }

        private void OnPlayButtonCLicked()
        {
            _playButton.onClick.AddListener((() => SceneManager.LoadScene("GameScene")));
        }
    }
}