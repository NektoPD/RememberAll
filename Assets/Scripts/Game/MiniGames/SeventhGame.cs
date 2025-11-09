// SeventhGame.cs
using System.Collections.Generic;
using System.Linq;
using Game.MiniGames.GameElements;
using Game.Popups;
using UnityEngine;
using UnityEngine.Events;

namespace Game.MiniGames
{
    public class SeventhGame : MonoBehaviour
    {
        [Header("Три правильных блока для Ж")]
        [SerializeField] private GameBlock _leftSlash;   // «/» (BlockRole.LeftLeg)
        [SerializeField] private GameBlock _rightSlash;  // «\» (BlockRole.RightLeg)
        [SerializeField] private GameBlock _vertical;    // прямая (BlockRole.Crossbar)

        [Header("Автоподбор по BlockRole")]
        [SerializeField] private bool _autoAssignByRoles = true;
        [Tooltip("Если включено — искать блоки только среди детей объекта с SeventhGame. Если выключено — во всей сцене.")]
        [SerializeField] private bool _searchOnlyInChildren = true;

        [Header("Остальные блоки (обманки)")]
        [SerializeField] private List<GameBlock> _otherBlocks = new List<GameBlock>();

        [Header("Случайные трансформации для прочих")]
        [SerializeField] private Vector2 _randomRotationRangeDeg = new Vector2(0f, 360f);

        [Header("Случайная раскладка по экрану")]
        [SerializeField] private float _screenPaddingWorld = 0.5f;
        [SerializeField] private float _maxY = 86f;
        [SerializeField] private int _positioningTriesPerBlock = 15;

        [Header("Цветовая палитра")]
        [SerializeField] private List<Color> _palette = new List<Color>
        {
            new Color32(0xFF,0xFA,0xE0,0xFF),
            new Color32(0xF2,0xC5,0x72,0xFF),
            new Color32(0xC1,0x8C,0x5D,0xFF),
            new Color32(0xB8,0x6B,0x6B,0xFF),
            new Color32(0x8A,0x6D,0x4F,0xFF),
            new Color32(0xD9,0xC8,0xA9,0xFF)
        };

        [Header("Попап победы")]
        [SerializeField] private GamePopup _winPopup;

        public System.Action OnWin;
        
        public UnityEvent OnWinEvent = new UnityEvent();

        private System.Action<GameBlock> _onConnChangedHandler;

        // —— NEW: флаг завершения и кэш позиций, чтобы победа засчитывалась без доп. столкновений ——
        private bool _finished;
        private Vector3 _prevLeftPos, _prevRightPos, _prevVertPos;

        private void Awake()
        {
            // Автоподбор по ролям (или если ссылки пустые)
            if (_autoAssignByRoles || !_leftSlash || !_rightSlash || !_vertical)
                AssignBlocksByRoles();

            if (!_leftSlash || !_rightSlash || !_vertical)
            {
                Debug.LogError("SeventhGame: Нужны блоки с ролями LeftLeg / RightLeg / Crossbar.");
                enabled = false;
                return;
            }

            _onConnChangedHandler = OnBlockConnectionsChanged;
            _leftSlash.ConnectionsChanged  += _onConnChangedHandler;
            _rightSlash.ConnectionsChanged += _onConnChangedHandler;
            _vertical.ConnectionsChanged   += _onConnChangedHandler;

            // Кэш стартовых позиций
            _prevLeftPos  = _leftSlash.transform.position;
            _prevRightPos = _rightSlash.transform.position;
            _prevVertPos  = _vertical.transform.position;
        }

        private void Start()
        {
            RandomizeColorsForAll();
            RandomizeTransformsForOthers();
            ShuffleAllBlocksInsideScreen();
            ValidateWin(); // на случай, если стартовая сцена уже собрана
        }

        private void Update()
        {
            if (_finished) return;
            if (!_leftSlash || !_rightSlash || !_vertical) return;

            // Если любую из трёх частей подвинули — проверяем геометрию без ожидания новых триггеров
            var lp = _leftSlash.transform.position;
            var rp = _rightSlash.transform.position;
            var vp = _vertical.transform.position;

            if (lp != _prevLeftPos || rp != _prevRightPos || vp != _prevVertPos)
            {
                _prevLeftPos  = lp;
                _prevRightPos = rp;
                _prevVertPos  = vp;
                ValidateWin();
            }
        }

        private void OnDestroy()
        {
            if (_leftSlash)   _leftSlash.ConnectionsChanged  -= _onConnChangedHandler;
            if (_rightSlash)  _rightSlash.ConnectionsChanged -= _onConnChangedHandler;
            if (_vertical)    _vertical.ConnectionsChanged   -= _onConnChangedHandler;
        }

        private void OnBlockConnectionsChanged(GameBlock _)
        {
            if (_finished) return;
            ValidateWin();
        }

        // —— Победа: обе наклонные касаются прямой с обеих сторон, и они по разные стороны от вертикали ——
        private void ValidateWin()
        {
            if (_finished) return;
            if (!_leftSlash || !_rightSlash || !_vertical) return;

            // Парные проверки, чтобы исключить ложные срабатывания (двустороннее касание)
            bool leftTouchesV_ByV   = _vertical.IsTouching(_leftSlash);
            bool rightTouchesV_ByV  = _vertical.IsTouching(_rightSlash);
            bool vTouchesLeft_ByL   = _leftSlash.IsTouching(_vertical);
            bool vTouchesRight_ByR  = _rightSlash.IsTouching(_vertical);

            bool touchesOk = leftTouchesV_ByV && rightTouchesV_ByV && vTouchesLeft_ByL && vTouchesRight_ByR;

            // Геометрия: наклонные должны быть по разные стороны от вертикали
            float dxL = _leftSlash.transform.position.x - _vertical.transform.position.x;
            float dxR = _rightSlash.transform.position.x - _vertical.transform.position.x;
            bool differentSides = dxL * dxR < 0f;

            bool ok = touchesOk && differentSides;
            if (!ok) return;

            // —— Победа! Останавливаем дальнейшие проверки полностью ——
            _finished = true;
            OnWin?.Invoke();
            OnWinEvent?.Invoke();

            if (_winPopup != null)
                _winPopup.Show("Победа! Буква Ж собрана");

            // Отписка от событий
            _leftSlash.ConnectionsChanged  -= _onConnChangedHandler;
            _rightSlash.ConnectionsChanged -= _onConnChangedHandler;
            _vertical.ConnectionsChanged   -= _onConnChangedHandler;

            // Отключаем компонент, чтобы не было Update/ValidateWin
            enabled = false;
        }

        // —— Автоподбор ссылок из ролей —— 
        private void AssignBlocksByRoles()
        {
            IEnumerable<GameBlock> source = _searchOnlyInChildren
                ? GetComponentsInChildren<GameBlock>(true)
                : FindObjectsOfType<GameBlock>(true);

            var list = source.Where(b => b != null).ToList();

            var left  = list.FirstOrDefault(b => b.Role == BlockRole.LeftLeg);
            var right = list.FirstOrDefault(b => b.Role == BlockRole.RightLeg);
            var cross = list.FirstOrDefault(b => b.Role == BlockRole.Crossbar);

            if (_autoAssignByRoles || !_leftSlash)  _leftSlash  = left;
            if (_autoAssignByRoles || !_rightSlash) _rightSlash = right;
            if (_autoAssignByRoles || !_vertical)   _vertical   = cross;

            _otherBlocks = list
                .Where(b => b && b != _leftSlash && b != _rightSlash && b != _vertical)
                .ToList();

#if UNITY_EDITOR
            int leftCount  = list.Count(b => b.Role == BlockRole.LeftLeg);
            int rightCount = list.Count(b => b.Role == BlockRole.RightLeg);
            int crossCount = list.Count(b => b.Role == BlockRole.Crossbar);

            if (leftCount > 1)  Debug.LogWarning($"SeventhGame: найдено {leftCount} блоков с ролью LeftLeg — будет взят первый.");
            if (rightCount > 1) Debug.LogWarning($"SeventhGame: найдено {rightCount} блоков с ролью RightLeg — будет взят первый.");
            if (crossCount > 1) Debug.LogWarning($"SeventhGame: найдено {crossCount} блоков с ролью Crossbar — будет взят первый.");
#endif
        }

        [ContextMenu("Assign blocks by roles (editor)")]
        private void AssignBlocksByRoles_Editor()
        {
            AssignBlocksByRoles();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        // —— Цвета —— 
        private void RandomizeColorsForAll()
        {
            var all = new List<GameBlock> { _leftSlash, _rightSlash, _vertical };
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

        // —— Масштаб/угол только для обманок —— 
        private void RandomizeTransformsForOthers()
        {
            float minA = Mathf.Min(_randomRotationRangeDeg.x, _randomRotationRangeDeg.y);
            float maxA = Mathf.Max(_randomRotationRangeDeg.x, _randomRotationRangeDeg.y);

            foreach (var b in _otherBlocks.Where(b => b))
            {
                float ang = Random.Range(minA, maxA);
                b.transform.rotation = Quaternion.Euler(0f, 0f, ang);
            }
        }

        // —— Перемешивание позиций для всех блоков в пределах экрана —— 
        private void ShuffleAllBlocksInsideScreen()
        {
            var cam = Camera.main;
            if (!cam)
            {
                Debug.LogWarning("SeventhGame: Нет Camera.main — пропускаю перемешивание позиций.");
                return;
            }

            var all = new List<GameBlock> { _leftSlash, _rightSlash, _vertical };
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

                    // при необходимости здесь можно добавить проверку на пересечения с другими
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

        /// <summary>Оценка размеров объекта (renderer > collider > дефолт).</summary>
        private static Vector2 EstimateHalfExtents(GameObject go)
        {
            var rend = go.GetComponentInChildren<Renderer>();
            if (rend) return new Vector2(rend.bounds.extents.x, rend.bounds.extents.y);

            var col = go.GetComponentInChildren<Collider2D>();
            if (col) return new Vector2(col.bounds.extents.x, col.bounds.extents.y);

            return new Vector2(0.5f, 0.5f);
        }

        [ContextMenu("Randomize Layout Now")]
        private void RandomizeLayoutNow()
        {
            RandomizeColorsForAll();
            RandomizeTransformsForOthers();
            ShuffleAllBlocksInsideScreen();
        }
    }
}
