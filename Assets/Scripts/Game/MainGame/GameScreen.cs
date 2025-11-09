using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Game.Intro;
using Game.MainGame.DTO;
using Game.SaveSystem;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.MainGame
{
    public class GameScreen : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup _screenCanvas;      // CanvasGroup самого GameScreen
        [SerializeField] private List<LevelButton> _levelButtons;
        [SerializeField] private IntroScreen _introScreen;
        [SerializeField] private TMP_Text _livesText;

        [Tooltip("Элементы главного экрана, скрываются до окончания интро")]
        [SerializeField] private GameObject[] _mainScreenElements;

        [Header("Анимация появления главного экрана")]
        [SerializeField] private float _fadeDuration = 0.35f;

        [Header("Game Flow Screens")]
        [SerializeField] private GameLostScreen _lostScreen;
        [SerializeField] private GameWinScreen  _winScreen;

        [Header("Lives")]
        [SerializeField] private int _startLives = 5;

        [Header("MiniGames (1..7)")]
        [SerializeField] private MiniGameRef[] _miniGames = new MiniGameRef[7];

        private List<LevelData> _levels;
        private int _currentGameIndex = -1;
        private int _lives;
        private bool _isTransitioning;

        [Serializable]
        public class MiniGameRef
        {
            [Tooltip("Для удобства в инспекторе")]
            public string name;

            [Header("Scene refs")]
            public GameObject root;         // корень игры (Canvas/GameObject)
            public CanvasGroup canvas;      // CanvasGroup для фейдов
            public Button closeButton;      // кнопка закрытия

            [Header("Game script (с событиями)")]
            public MonoBehaviour gameBehaviour; // компонент самой игры

            // — хранилище подписок, настраивается в коде:
            [HideInInspector] public UnityEventProxy winProxy;
            [HideInInspector] public UnityEventProxy loseProxy;

            public void SetActive(bool on)
            {
                if (root) root.SetActive(on);
            }
        }

        /// <summary>Небольшой адаптер, чтобы унифицировать доступ к UnityEvent’ам у разных игр.</summary>
        public class UnityEventProxy
        {
            public event Action Fired;
            public void Raise() => Fired?.Invoke();
        }

        private void Awake()
        {
            // 1) Интро
            var player = PlayerDataSaver.LoadOrDefault();
            if (!player.IsIntroCompleted)
            {
                SetMainElementsActive(false, immediate: true);
                _introScreen.gameObject.SetActive(true);
                _introScreen.ShowIntro();
                _introScreen.OnIntroFinished += HandleIntroFinished;
            }
            else
            {
                SetMainElementsActive(true, immediate: true);
            }

            // 2) Данные уровней
            _levels = LevelDataSaver.LoadOrCreateCompleteSet();

            // Первый уровень открыт, остальные — как в сейве
            EnsureFirstUnlocked(_levels);

            // 3) Привязка кнопок
            BindLevelButtons(_levels);

            // 4) Жизни
            _lives = _startLives;
            UpdateLivesUI();

            // 5) Игры — выключить все и навесить события
            InitMiniGames();

            // 6) Экран проигрыша/победы — подписки
            if (_lostScreen) _lostScreen.Finished += OnLostScreenFinished;
            if (_winScreen)  _winScreen.Finished  += OnWinScreenFinished;
        }

        private void OnDestroy()
        {
            if (_introScreen != null) _introScreen.OnIntroFinished -= HandleIntroFinished;
            if (_lostScreen  != null) _lostScreen.Finished -= OnLostScreenFinished;
            if (_winScreen   != null) _winScreen.Finished  -= OnWinScreenFinished;
            UnsubscribeMiniGames();
        }

        // ---------- Intro / Main visibility ----------
        private void HandleIntroFinished()
        {
            var player = PlayerDataSaver.LoadOrDefault();
            player.IsIntroCompleted = true;
            PlayerDataSaver.Save(player);
            SetMainElementsActive(true, immediate: false);
        }

        private void SetMainElementsActive(bool active, bool immediate)
        {
            foreach (var go in _mainScreenElements.Where(e => e != null))
            {
                var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
                if (active)
                {
                    go.SetActive(true);
                    if (immediate)
                    {
                        cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true;
                    }
                    else
                    {
                        cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;
                        cg.DOFade(1f, _fadeDuration).OnComplete(() =>
                        {
                            cg.interactable = true; cg.blocksRaycasts = true;
                        });
                    }
                }
                else
                {
                    if (immediate)
                    {
                        cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false; go.SetActive(false);
                    }
                    else
                    {
                        cg.DOFade(0f, _fadeDuration).OnComplete(() =>
                        {
                            cg.interactable = false; cg.blocksRaycasts = false; go.SetActive(false);
                        });
                    }
                }
            }
        }

        // ---------- Levels ----------
        private static void EnsureFirstUnlocked(List<LevelData> levels)
        {
            // Гарантируем, что самый первый тип разблокирован
            levels.Sort((a,b) => a.LevelType.CompareTo(b.LevelType));
            if (levels.Count > 0) levels[0].IsUnlocked = true;
            LevelDataSaver.Save(levels);
        }

        private void BindLevelButtons(List<LevelData> levels)
        {
            var byType = levels.ToDictionary(l => l.LevelType, l => l);

            foreach (var btn in _levelButtons)
            {
                if (!btn) continue;
                LevelType typeToUse;

                if (btn.LevelData != null) typeToUse = btn.LevelData.LevelType;
                else
                {
                    int idx = _levelButtons.IndexOf(btn);
                    var types = (LevelType[])Enum.GetValues(typeof(LevelType));
                    typeToUse = (idx >= 0 && idx < types.Length) ? types[idx] : types[0];
                }

                if (!byType.TryGetValue(typeToUse, out var ld))
                    ld = new LevelData { LevelType = typeToUse, IsUnlocked = false, Progress = 0f };

                btn.Initialize(ld);
                btn.OnOpenClicked -= HandleLevelOpenClicked;
                btn.OnOpenClicked += HandleLevelOpenClicked;
            }
        }

        private void RefreshAllLevelButtons()
        {
            foreach (var b in _levelButtons) b?.RefreshVisual();
        }

        private void HandleLevelOpenClicked(LevelType type)
        {
            // Только открыть — если разблокирован
            var ld = _levels.FirstOrDefault(l => l.LevelType == type);
            if (ld == null || !ld.IsUnlocked) return;

            int idx = IndexForLevelType(type);
            if (idx < 0 || idx >= _miniGames.Length) return;

            OpenMiniGame(idx);
        }

        private int IndexForLevelType(LevelType type)
        {
            // Полагаемся на enum-порядок
            var types = (LevelType[])Enum.GetValues(typeof(LevelType));
            for (int i = 0; i < types.Length; i++)
                if (types[i].Equals(type)) return i;
            return -1;
        }

        // ---------- MiniGames orchestration ----------
        private void InitMiniGames()
        {
            for (int i = 0; i < _miniGames.Length; i++)
            {
                var mg = _miniGames[i];
                if (mg == null) continue;

                // выключить
                mg.SetActive(false);
                if (mg.canvas) mg.canvas.alpha = 0f;

                // подписки
                TryWireGameEvents(mg, i);

                if (mg.closeButton)
                {
                    int idx = i;
                    mg.closeButton.onClick.RemoveAllListeners();
                    mg.closeButton.onClick.AddListener(() => CloseMiniGame(idx));
                }
            }
        }

        private void UnsubscribeMiniGames()
        {
            foreach (var mg in _miniGames)
            {
                if (mg?.winProxy != null)  mg.winProxy.Fired  -= () => { };
                if (mg?.loseProxy != null) mg.loseProxy.Fired -= () => { };
                if (mg?.closeButton != null) mg.closeButton.onClick.RemoveAllListeners();
            }
        }

        private void TryWireGameEvents(MiniGameRef mg, int index)
        {
            if (!mg.gameBehaviour) return;

            // Ищем поля UnityEvent OnWinEvent/OnLoseEvent с помощью рефлексии (чтобы не лезть в типы)
            var t = mg.gameBehaviour.GetType();

            // Win
            var winEvtField = t.GetField("OnWinEvent");
            if (winEvtField != null)
            {
                var unityEvent = winEvtField.GetValue(mg.gameBehaviour) as UnityEvent;
                if (unityEvent != null)
                {
                    mg.winProxy ??= new UnityEventProxy();
                    unityEvent.AddListener(() => mg.winProxy.Raise());
                    mg.winProxy.Fired += () => OnGameWon(index);
                }
            }
            // Lose
            var loseEvtField = t.GetField("OnLoseEvent");
            if (loseEvtField != null)
            {
                var unityEvent = loseEvtField.GetValue(mg.gameBehaviour) as UnityEvent;
                if (unityEvent != null)
                {
                    mg.loseProxy ??= new UnityEventProxy();
                    unityEvent.AddListener(() => mg.loseProxy.Raise());
                    mg.loseProxy.Fired += () => OnGameLost(index);
                }
            }
        }

        private void OpenMiniGame(int index)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            _currentGameIndex = index;

            // скрыть GameScreen
            FadeCanvas(_screenCanvas, false, () =>
            {
                SetMainElementsActive(false, immediate: true);

                // показать игру
                var mg = _miniGames[index];
                mg.SetActive(true);
                FadeCanvas(mg.canvas, true, () =>
                {
                    _isTransitioning = false;
                });
            });
        }

        private void CloseMiniGame(int index)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            var mg = _miniGames[index];
            FadeCanvas(mg.canvas, false, () =>
            {
                mg.SetActive(false);
                _currentGameIndex = -1;

                // показать GameScreen
                SetMainElementsActive(true, immediate: true);
                FadeCanvas(_screenCanvas, true, () =>
                {
                    _isTransitioning = false;
                });
            });
        }

        private void OnGameWon(int index)
        {
            // Отмечаем прогресс уровня
            var type = ((LevelType[])Enum.GetValues(typeof(LevelType)))[index];
            var ld = _levels.FirstOrDefault(l => l.LevelType.Equals(type));
            if (ld != null) { ld.Progress = 1f; LevelDataSaver.Save(_levels); }

            // Разблокировать следующий уровень (если есть)
            if (index + 1 < _levels.Count)
            {
                var next = _levels.OrderBy(l => l.LevelType).ElementAt(index + 1);
                if (!next.IsUnlocked)
                {
                    next.IsUnlocked = true;
                    LevelDataSaver.Save(_levels);
                }
            }

            RefreshAllLevelButtons();

            // Закрыть игру и вернуть главную
            if (_currentGameIndex == index) CloseMiniGame(index);

            // Проверить «все пройдены?»
            if (AllLevelsCompleted())
            {
                // Показать GameWinScreen
                ShowWinScreen();
            }
        }

        private void OnGameLost(int index)
        {
            _lives = Mathf.Max(0, _lives - 1);
            UpdateLivesUI();

            if (_lives <= 0)
            {
                // Закрываем открытую игру (если открыта)
                if (_currentGameIndex == index) ForceCloseGameNow(index);

                // показать экран проигрыша
                ShowLostScreen();
            }
            else
            {
                // Просто вернуть на главный экран, чтобы игрок мог переиграть
                if (_currentGameIndex == index) CloseMiniGame(index);
            }
        }

        private bool AllLevelsCompleted() => _levels.All(l => l.Progress >= 1f);

        // ---------- Flow: Lost / Win ----------
        private void ShowLostScreen()
        {
            if (_lostScreen == null) { ResetSavesAndGoMain(); return; }

            // скрыть главный экран (на всякий), показать Lost
            FadeCanvas(_screenCanvas, false, () =>
            {
                _lostScreen.gameObject.SetActive(true);
                _lostScreen.ShowScreen(); // строки из инспектора
            });
        }

        private void OnLostScreenFinished()
        {
            ResetSavesAndGoMain();
        }

        private void ShowWinScreen()
        {
            if (_winScreen == null) { GoMainScene(); return; }

            FadeCanvas(_screenCanvas, false, () =>
            {
                _winScreen.gameObject.SetActive(true);
                _winScreen.ShowScreen(); // строки из инспектора
            });
        }

        private void OnWinScreenFinished()
        {
            // сейвы НЕ чистим
            GoMainScene();
        }

        private void ResetSavesAndGoMain()
        {
            try
            {
                PlayerDataSaver.DeleteAll();
                LevelDataSaver.DeleteAll();
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to delete saves: " + e);
            }
            GoMainScene();
        }

        private void GoMainScene()
        {
            SceneManager.LoadScene("MainScene");
        }

        // ---------- Helpers ----------
        private void UpdateLivesUI()
        {
            if (_livesText) _livesText.text = $"Жизней: {_lives}";
        }

        private void FadeCanvas(CanvasGroup cg, bool show, Action onDone)
        {
            if (!cg)
            {
                onDone?.Invoke();
                return;
            }
            float from = cg.alpha;
            float to = show ? 1f : 0f;

            if (show)
            {
                cg.gameObject.SetActive(true);
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            cg.DOKill();
            cg.DOFade(to, _fadeDuration).OnComplete(() =>
            {
                cg.interactable = show;
                cg.blocksRaycasts = show;
                if (!show) cg.gameObject.SetActive(false);
                onDone?.Invoke();
            });
        }

        private void ForceCloseGameNow(int index)
        {
            var mg = _miniGames[index];
            mg.canvas?.DOKill();
            if (mg.canvas) { mg.canvas.alpha = 0f; mg.canvas.interactable = false; mg.canvas.blocksRaycasts = false; }
            mg.SetActive(false);
            _currentGameIndex = -1;

            // Вернуть GameScreen сразу
            SetMainElementsActive(true, immediate: true);
            if (_screenCanvas)
            {
                _screenCanvas.alpha = 1f; _screenCanvas.interactable = true; _screenCanvas.blocksRaycasts = true;
            }
        }
    }
}
