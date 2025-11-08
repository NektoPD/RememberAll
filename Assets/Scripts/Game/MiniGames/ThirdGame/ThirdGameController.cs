using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace Game.MiniGames.ThirdGame
{
    public class ThirdGameController : MonoBehaviour
    {
        [Header("Scene References")]
        [Tooltip("Все ConnectionDot на сцене (в любом порядке).")]
        [SerializeField] private List<ConnectionDot> dots = new();

        [Tooltip("Только те точки, которые должны быть помечены (win).")]
        [SerializeField] private List<ConnectionDot> winPattern = new();

        [Tooltip("Контейнер, куда будут складываться LineRenderer-линии (опционально).")]
        [SerializeField] private Transform linesRoot;

        [Tooltip("Префаб LineRenderer (пустой, без материалов — всё настроим кодом).")]
        [SerializeField] private LineRenderer linePrefab;

        [Header("Input Rules")]
        [Tooltip("Ограничивать ли соединения соседством (если нет координат — оставь false).")]
        [SerializeField] private bool requireAdjacent = false;

        [Header("Line Style")]
        [Tooltip("Материал постоянных линий (URP: Universal Render Pipeline/Unlit или Sprites/Default).")]
        [SerializeField] private Material lineMaterial;

        [Tooltip("Материал превью-линии (можно тот же, просто будет полупрозрачной).")]
        [SerializeField] private Material previewMaterial;

        [Tooltip("Слой сортировки для линий.")]
        [SerializeField] private string sortingLayerName = "Default";

        [Tooltip("Порядок сортировки (чем больше, тем поверх).")]
        [SerializeField] private int sortingOrder = 100;

        [Tooltip("Z-позиция линий (чуть ближе к камере, чем точки/фон).")]
        [SerializeField] private float lineZ = -1f;

        [Header("Width: Auto from grid spacing")]
        [Tooltip("Автоподбор толщины от шага между точками.")]
        [SerializeField] private bool autoWidth = true;

        [Tooltip("Доля от шага между точками (0.02–0.35).")]
        [SerializeField, Range(0.02f, 0.35f)] private float widthFactor = 0.1f;

        [SerializeField] private float minWidth = 0.01f;
        [SerializeField] private float maxWidth = 0.08f;

        [Header("Width: Screen-space emulation (optional)")]
        [Tooltip("Эмулировать толщину в пикселях экрана (перекрывает autoWidth).")]
        [SerializeField] private bool screenSpaceWidth = false;

        [Tooltip("Желаемая толщина линии в пикселях (если включен screenSpaceWidth).")]
        [SerializeField] private float pixels = 3f;

        [Header("Animation")]
        [Tooltip("Время дорисовки линии (сек).")]
        [SerializeField] private float drawDuration = 0.15f;

        [Tooltip("Скругление концов линии.")]
        [SerializeField] private int capVertices = 6;

        [Tooltip("Скругление поворотов линии.")]
        [SerializeField] private int cornerVertices = 4;

        [Header("Events")]
        public UnityEvent OnWin;

        // --- runtime ---
        private readonly List<LineRenderer> _drawnLines = new();
        private readonly HashSet<ConnectionDot> _marked = new();
        private ConnectionDot _dragStart;
        private LineRenderer _preview;

        private float _computedWidth = 0.04f;

        private void Start()
        {
            RecomputeWidth();
        }

        private void Update()
        {
            if (_dragStart == null) return;

            Vector3 world = GetMouseWorld();
            UpdatePreview(_dragStart.transform.position, world);

            if (Input.GetMouseButtonUp(0))
            {
                var end = RaycastDot();
                TryCommit(_dragStart, end);
                DestroyPreview();
                _dragStart = null;
            }
        }

        // ===================== DRAG =====================
        public void BeginDragFromDot(ConnectionDot start)
        {
            _dragStart = start;
            CreatePreview(start.transform.position);
        }

        private void TryCommit(ConnectionDot a, ConnectionDot b)
        {
            if (b == null || b == a) return;

            if (requireAdjacent && !AreAdjacent(a, b))
            {
                Shake(a.transform);
                return;
            }

            // линия
            SpawnLine(a.transform.position, b.transform.position);

            // пометки
            a.Mark(); b.Mark();
            _marked.Add(a); _marked.Add(b);
        }

        // ===================== CHECK / RESET =====================
        [ContextMenu("Check Result (Editor)")]
        public void CheckResult()
        {
            // 1) Помеченные не должны выходить за пределы winPattern
            foreach (var d in _marked)
            {
                if (!winPattern.Contains(d))
                {
                    FailFeedback();
                    return;
                }
            }
            // 2) Все из winPattern должны быть помечены
            foreach (var d in winPattern)
            {
                if (d == null || !d.IsMarked)
                {
                    FailFeedback();
                    return;
                }
            }

            // успех
            WinFeedback();
            OnWin?.Invoke();
        }

        [ContextMenu("Reset Lines (Editor)")]
        public void ResetLines()
        {
            foreach (var lr in _drawnLines)
                if (lr) Destroy(lr.gameObject);
            _drawnLines.Clear();

            _marked.Clear();
            foreach (var d in dots)
                d?.Unmark();

            DestroyPreview();
        }

        // ===================== LINES =====================
        private void SpawnLine(Vector3 a, Vector3 b)
        {
            if (!linePrefab) return;

            var lr = Instantiate(linePrefab, linesRoot ? linesRoot : transform);

            // стиль
            ApplyLineStyle(lr, preview: false);

            // позиции
            a.z = lineZ; b.z = lineZ;
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, a);

            // анимация «дорисовки»
            Vector3 cur = a;
            DOTween.To(() => cur, v => { cur = v; lr.SetPosition(1, cur); }, b, drawDuration)
                   .SetEase(Ease.OutSine);

            _drawnLines.Add(lr);
        }

        private void CreatePreview(Vector3 start)
        {
            if (!linePrefab) return;

            _preview = Instantiate(linePrefab, linesRoot ? linesRoot : transform);

            ApplyLineStyle(_preview, preview: true);

            start.z = lineZ;
            _preview.positionCount = 2;
            _preview.SetPosition(0, start);
            _preview.SetPosition(1, start);
        }

        private void UpdatePreview(Vector3 a, Vector3 b)
        {
            if (!_preview) return;
            a.z = lineZ; b.z = lineZ;
            _preview.SetPosition(0, a);
            _preview.SetPosition(1, b);
        }

        private void DestroyPreview()
        {
            if (_preview) Destroy(_preview.gameObject);
            _preview = null;
        }

        private void ApplyLineStyle(LineRenderer lr, bool preview)
        {
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.View;
            lr.textureMode = LineTextureMode.Stretch;
            lr.numCapVertices = capVertices;
            lr.numCornerVertices = cornerVertices;

            lr.material = preview && previewMaterial ? previewMaterial : lineMaterial;

            float baseW = screenSpaceWidth ? WorldWidthFromPixels(pixels) : _computedWidth;
            float width = baseW * ScaleCompensation(lr.transform);

            lr.startWidth = lr.endWidth = Mathf.Max(0.001f, width);

            var c = Color.white;
            if (preview) c.a = 0.7f; // полупрозрачный превью
            lr.startColor = lr.endColor = c;

            lr.sortingLayerName = sortingLayerName;
            lr.sortingOrder = sortingOrder;
        }

        // ===================== WIDTH / UTILS =====================
        private void RecomputeWidth()
        {
            if (screenSpaceWidth)
            {
                _computedWidth = WorldWidthFromPixels(pixels);
                return;
            }

            if (autoWidth)
            {
                float spacing = EstimateSpacing();
                _computedWidth = Mathf.Clamp(spacing * widthFactor, minWidth, maxWidth);
            }
            else
            {
                _computedWidth = Mathf.Clamp(_computedWidth, minWidth, maxWidth);
            }
        }

        // медиана минимальных расстояний между точками — оценка шага сетки
        private float EstimateSpacing()
        {
            if (dots == null || dots.Count < 2) return 0.1f;
            var mins = new List<float>(dots.Count);
            for (int i = 0; i < dots.Count; i++)
            {
                if (dots[i] == null) continue;
                float best = float.MaxValue;
                var pi = dots[i].transform.position;
                for (int j = 0; j < dots.Count; j++)
                {
                    if (i == j || dots[j] == null) continue;
                    float d = Vector3.Distance(pi, dots[j].transform.position);
                    if (d > 1e-4f && d < best) best = d;
                }
                if (best < float.MaxValue) mins.Add(best);
            }
            if (mins.Count == 0) return 0.1f;
            mins.Sort();
            return mins[mins.Count / 2];
        }

        private float WorldWidthFromPixels(float px)
        {
            var cam = Camera.main;
            if (!cam || !cam.orthographic) return Mathf.Clamp(px * 0.001f, minWidth, maxWidth);
            float unitsPerPixel = (cam.orthographicSize * 2f) / Screen.height;
            return Mathf.Clamp(px * unitsPerPixel, minWidth, maxWidth);
        }

        private float ScaleCompensation(Transform t)
        {
            if (t == null) return 1f;
            var s = t.lossyScale;
            float k = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            return k > 1e-5f ? 1f / k : 1f;
        }

        private Vector3 GetMouseWorld()
        {
            var cam = Camera.main;
            var p = Input.mousePosition;
            // расстояние по Z до линии
            float z = cam ? Mathf.Abs((linesRoot ? linesRoot.position.z : transform.position.z) - cam.transform.position.z) : 10f;
            p.z = z;
            return cam ? cam.ScreenToWorldPoint(p) : Vector3.zero;
        }

        private ConnectionDot RaycastDot()
        {
            var pos = GetMouseWorld();
            var hit = Physics2D.OverlapPoint(pos);
            return hit ? hit.GetComponent<ConnectionDot>() : null;
        }

        private static void Shake(Transform t) => t.DOShakeScale(0.18f, 0.12f, 10, 90);

        // Заглушка: если хочешь ограничить соседством — реализуй здесь
        private static bool AreAdjacent(ConnectionDot a, ConnectionDot b)
        {
            // В твоей упрощенной схеме координат может не быть — вернём true.
            // Если в ConnectionDot есть (x,y), тут можно проверить Манхэттен-соседство: (|dx|+|dy|)==1
            return true;
        }

        // На случай изменения параметров в инспекторе в режиме Play
        private void OnValidate()
        {
            if (Application.isPlaying) RecomputeWidth();
        }
    }
}
