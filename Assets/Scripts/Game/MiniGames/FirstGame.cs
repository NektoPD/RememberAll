// FirstGame.cs
using System.Collections.Generic;
using System.Linq;
using Game.MiniGames.GameElements;
using UnityEngine;
using UnityEngine.Events;

namespace Game.MiniGames
{
    public class FirstGame : MonoBehaviour
    {
        [Header("Три правильных блока")]
        [SerializeField] private GameBlock _leftLeg;
        [SerializeField] private GameBlock _rightLeg;
        [SerializeField] private GameBlock _crossbar;

        [Header("Прочие блоки (обманки и т.п.)")]
        [SerializeField] private List<GameBlock> _otherBlocks = new List<GameBlock>();

        [Header("Случайные трансформации для прочих блоков")]
        [SerializeField] private Vector2 _randomUniformScaleRange = new Vector2(0.75f, 1.25f);
        [SerializeField] private Vector2 _randomRotationRangeDeg = new Vector2(0f, 360f);

        [Header("Случайная раскладка по экрану")]
        [SerializeField] private float _screenPaddingWorld = 0.5f; // отступ от краёв
        [SerializeField] private float _maxY = 86f;                // ограничение по Y (мировая координата)
        [SerializeField] private int _positioningTriesPerBlock = 15;

        [Header("Цветовая палитра игры")]
        [SerializeField] private List<Color> _palette = new List<Color>
        {
            new Color32(0xFF,0xFA,0xE0,0xFF), // #FFFAE0 Sand (как текст)
            new Color32(0xF2,0xC5,0x72,0xFF), // #F2C572 Amber
            new Color32(0xC1,0x8C,0x5D,0xFF), // #C18C5D Clay
            new Color32(0xB8,0x6B,0x6B,0xFF), // #B86B6B Rose
            new Color32(0x8A,0x6D,0x4F,0xFF), // #8A6D4F Olive
            new Color32(0xD9,0xC8,0xA9,0xFF)  // #D9C8A9 Mist
        };

        public System.Action OnWin;
        
        public UnityEvent OnWinEvent = new UnityEvent();

        private System.Action<GameBlock> _onConnChangedHandler;

        private void Awake()
        {
            if (!_leftLeg || !_rightLeg || !_crossbar)
            {
                Debug.LogError("FirstGame: Укажите все три блока (Left/Right/Crossbar).");
                enabled = false; return;
            }

            _onConnChangedHandler = OnBlockConnectionsChanged;
            _leftLeg.ConnectionsChanged   += _onConnChangedHandler;
            _rightLeg.ConnectionsChanged  += _onConnChangedHandler;
            _crossbar.ConnectionsChanged  += _onConnChangedHandler;
        }

        private void Start()
        {
            RandomizeColorsForAll();        // 1) цвета
            RandomizeTransformsForOthers(); // 2) масштаб/угол — только для прочих
            ShuffleAllBlocksInsideScreen(); // 3) позиции всех в пределах экрана (Y <= _maxY)
            ValidateWin();                  // 4) на всякий случай
        }

        private void OnDestroy()
        {
            if (_leftLeg)   _leftLeg.ConnectionsChanged   -= _onConnChangedHandler;
            if (_rightLeg)  _rightLeg.ConnectionsChanged  -= _onConnChangedHandler;
            if (_crossbar)  _crossbar.ConnectionsChanged  -= _onConnChangedHandler;
        }

        private void OnBlockConnectionsChanged(GameBlock _) => ValidateWin();

        // ——— Победа: перекладина касается обеих опор, и наоборот (двусторонне) ———
        private void ValidateWin()
        {
            bool crossTouchesLeft  = _crossbar.IsTouching(_leftLeg);
            bool crossTouchesRight = _crossbar.IsTouching(_rightLeg);
            bool leftTouchesCross  = _leftLeg.IsTouching(_crossbar);
            bool rightTouchesCross = _rightLeg.IsTouching(_crossbar);

            bool ok = crossTouchesLeft && crossTouchesRight && leftTouchesCross && rightTouchesCross;

            if (ok)
            {
                Debug.Log("WIN: Собрана буква А!");
                OnWin?.Invoke();
                OnWinEvent?.Invoke(); // ← A
                // если хочешь — отключи дальнейшую проверку:
                // _leftLeg.ConnectionsChanged   -= _onConnChangedHandler;
                // _rightLeg.ConnectionsChanged  -= _onConnChangedHandler;
                // _crossbar.ConnectionsChanged  -= _onConnChangedHandler;
            }
        }

        // ——— Цвета ———
        private void RandomizeColorsForAll()
        {
            var all = new List<GameBlock> { _leftLeg, _rightLeg, _crossbar };
            foreach (var b in _otherBlocks) if (b) all.Add(b);

            if (_palette == null || _palette.Count == 0) return;

            foreach (var block in all.Where(b => b))
            {
                var sr = block.GetComponentInChildren<SpriteRenderer>();
                if (!sr) continue;

                var color = _palette[Random.Range(0, _palette.Count)];
                sr.color = color;
            }
        }

        // ——— Масштаб/угол для прочих ———
        private void RandomizeTransformsForOthers()
        {
            float minS = Mathf.Min(_randomUniformScaleRange.x, _randomUniformScaleRange.y);
            float maxS = Mathf.Max(_randomUniformScaleRange.x, _randomUniformScaleRange.y);
            float minA = Mathf.Min(_randomRotationRangeDeg.x, _randomRotationRangeDeg.y);
            float maxA = Mathf.Max(_randomRotationRangeDeg.x, _randomRotationRangeDeg.y);

            foreach (var b in _otherBlocks.Where(b => b))
            {
                float s = Random.Range(minS, maxS);
                b.transform.localScale = new Vector3(s, s, b.transform.localScale.z);

                float ang = Random.Range(minA, maxA);
                b.transform.rotation = Quaternion.Euler(0f, 0f, ang);
            }
        }

        // ——— Перемешивание позиций для всех блоков в пределах экрана ———
        private void ShuffleAllBlocksInsideScreen()
        {
            var cam = Camera.main;
            if (!cam)
            {
                Debug.LogWarning("FirstGame: Нет Camera.main — пропускаю перемешивание позиций.");
                return;
            }

            var all = new List<GameBlock> { _leftLeg, _rightLeg, _crossbar };
            foreach (var b in _otherBlocks) if (b) all.Add(b);

            // Перемешаем порядок
            for (int i = 0; i < all.Count; i++)
            {
                int j = Random.Range(i, all.Count);
                (all[i], all[j]) = (all[j], all[i]);
            }

            // Границы камеры в мире + паддинги
            GetCameraWorldBounds(cam, out var worldMin, out var worldMax);
            worldMin += new Vector2(_screenPaddingWorld, _screenPaddingWorld);
            worldMax -= new Vector2(_screenPaddingWorld, _screenPaddingWorld);

            // Ограничение по Y
            worldMax.y = Mathf.Min(worldMax.y, _maxY);

            foreach (var b in all)
            {
                if (!b) continue;

                var extents = EstimateHalfExtents(b.gameObject);
                bool placed = false;

                for (int t = 0; t < _positioningTriesPerBlock; t++)
                {
                    float x = Random.Range(worldMin.x + extents.x, worldMax.x - extents.x);
                    float y = Random.Range(worldMin.y + extents.y, worldMax.y - extents.y);
                    var pos = new Vector3(x, y, b.transform.position.z);
                    b.transform.position = pos;

                    // (можно добавить проверку на пересечения с уже поставленными блоками)
                    placed = true;
                    break;
                }

                if (!placed)
                {
                    var p = b.transform.position;
                    p.y = Mathf.Min(p.y, _maxY);
                    b.transform.position = p;
                }
            }
        }

        private static void GetCameraWorldBounds(Camera cam, out Vector2 min, out Vector2 max)
        {
            float z = Mathf.Abs(cam.transform.position.z);
            var bl = cam.ScreenToWorldPoint(new Vector3(0, 0, z));
            var tr = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, z));
            min = new Vector2(Mathf.Min(bl.x, tr.x), Mathf.Min(bl.y, tr.y));
            max = new Vector2(Mathf.Max(bl.x, tr.x), Mathf.Max(bl.y, tr.y));
        }

        /// <summary>Пытаемся оценить размеры объекта (renderer > collider > дефолт).</summary>
        private static Vector2 EstimateHalfExtents(GameObject go)
        {
            var rend = go.GetComponentInChildren<Renderer>();
            if (rend) return new Vector2(rend.bounds.extents.x, rend.bounds.extents.y);

            var col = go.GetComponentInChildren<Collider2D>();
            if (col) return new Vector2(col.bounds.extents.x, col.bounds.extents.y);

            return new Vector2(0.5f, 0.5f);
        }

        // Удобная кнопка в контекстном меню (по желанию):
        [ContextMenu("Randomize Layout Now")]
        private void RandomizeLayoutNow()
        {
            RandomizeColorsForAll();
            RandomizeTransformsForOthers();
            ShuffleAllBlocksInsideScreen();
        }
    }
}
