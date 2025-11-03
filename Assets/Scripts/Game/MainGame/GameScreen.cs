using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Game.Intro;
using Game.MainGame.DTO;
using Game.SaveSystem;
using UnityEngine;

namespace Game.MainGame
{
    public class GameScreen : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private List<LevelButton> _levelButtons;
        [SerializeField] private IntroScreen _introScreen;

        [Tooltip("Элементы главного экрана, которые должны быть скрыты до окончания интро")]
        [SerializeField] private GameObject[] _mainScreenElements;

        [Header("Анимация появления главного экрана")]
        [SerializeField] private float _fadeDuration = 0.35f;

        private List<LevelData> _levels;

        private void Awake()
        {
            // 1) Загрузка/проверка данных игрока
            var player = PlayerDataSaver.LoadOrDefault();
            if (!player.IsIntroCompleted)
            {
                // Скрыть главный экран, показать интро
                SetMainElementsActive(false, immediate: true);
                _introScreen.gameObject.SetActive(true);
                _introScreen.ShowIntro();
                _introScreen.OnIntroFinished += HandleIntroFinished;
            }
            else
            {
                // Если интро уже пройдено — главный экран сразу виден
                SetMainElementsActive(true, immediate: true);
            }

            // 2) Загрузка/инициализация LevelData (строго по количеству LevelType)
            _levels = LevelDataSaver.LoadOrCreateCompleteSet();

            // 3) Привязка данных к кнопкам
            BindLevelButtons(_levels);
        }

        private void OnDestroy()
        {
            if (_introScreen != null)
            {
                _introScreen.OnIntroFinished -= HandleIntroFinished;
            }
        }

        private void HandleIntroFinished()
        {
            // Сохранить, что интро завершено
            var player = PlayerDataSaver.LoadOrDefault();
            player.IsIntroCompleted = true;
            PlayerDataSaver.Save(player);

            // Плавно показать элементы главного экрана
            SetMainElementsActive(true, immediate: false);
        }

        private void SetMainElementsActive(bool active, bool immediate)
        {
            foreach (var go in _mainScreenElements.Where(e => e != null))
            {
                // Обеспечим наличие CanvasGroup для фейда
                var cg = go.GetComponent<CanvasGroup>();
                if (!cg) cg = go.AddComponent<CanvasGroup>();

                if (active)
                {
                    go.SetActive(true);
                    if (immediate)
                    {
                        cg.alpha = 1f;
                        cg.interactable = true;
                        cg.blocksRaycasts = true;
                    }
                    else
                    {
                        cg.alpha = 0f;
                        cg.interactable = false;
                        cg.blocksRaycasts = false;
                        cg.DOFade(1f, _fadeDuration)
                          .OnComplete(() =>
                          {
                              cg.interactable = true;
                              cg.blocksRaycasts = true;
                          });
                    }
                }
                else
                {
                    if (immediate)
                    {
                        cg.alpha = 0f;
                        cg.interactable = false;
                        cg.blocksRaycasts = false;
                        go.SetActive(false);
                    }
                    else
                    {
                        cg.DOFade(0f, _fadeDuration)
                          .OnComplete(() =>
                          {
                              cg.interactable = false;
                              cg.blocksRaycasts = false;
                              go.SetActive(false);
                          });
                    }
                }
            }
        }

        private void BindLevelButtons(List<LevelData> levels)
        {
            // Карта: LevelType -> LevelData
            var byType = levels.ToDictionary(l => l.LevelType, l => l);

            foreach (var btn in _levelButtons)
            {
                if (btn == null) continue;

                // Если у кнопки уже назначен LevelData — используем его тип,
                // иначе подбираем по имени объекта/индексу в списке.
                LevelType typeToUse;

                if (btn.LevelData != null)
                {
                    typeToUse = btn.LevelData.LevelType;
                }
                else
                {
                    // Попробуем взять тип по индексу кнопки
                    int idx = _levelButtons.IndexOf(btn);
                    var types = (LevelType[])Enum.GetValues(typeof(LevelType));
                    if (idx >= 0 && idx < types.Length) typeToUse = types[idx];
                    else typeToUse = types[0]; // запасной вариант
                }

                if (!byType.TryGetValue(typeToUse, out var ld))
                {
                    // если чего-то не сложилось, создаём дефолт
                    ld = new LevelData { LevelType = typeToUse, IsUnlocked = false, Progress = 0f };
                }

                btn.Initialize(ld);
                btn.OnOpenClicked -= HandleLevelOpenClicked; // чтобы не задвоить
                btn.OnOpenClicked += HandleLevelOpenClicked;
            }
        }

        private void HandleLevelOpenClicked(LevelType type)
        {
            // Здесь ваша логика открытия/запуска уровня
            Debug.Log($"Open level: {type}");

            // Пример: если хотим по клику «разлочить» уровень и сохранить:
            var ld = _levels.FirstOrDefault(l => l.LevelType == type);
            if (ld != null && !ld.IsUnlocked)
            {
                ld.IsUnlocked = true;
                LevelDataSaver.Save(_levels);

                // Обновим визуал соответствующей кнопки
                var btn = _levelButtons.FirstOrDefault(b => b.LevelData != null && b.LevelData.LevelType == type);
                btn?.RefreshVisual();
            }
        }
    }
}
