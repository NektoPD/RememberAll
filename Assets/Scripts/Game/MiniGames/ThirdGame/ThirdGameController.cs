using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using Game.Popups;

namespace Game.MiniGames.ThirdGame
{
    public class ThirdGameController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private List<ConnectionDot> dots = new();
        [SerializeField] private List<ConnectionDot> winPattern = new();
        [SerializeField] private Transform linesRoot;
        [SerializeField] private LineRenderer linePrefab;

        [Header("Input Rules")]
        [SerializeField] private bool requireAdjacent = false;

        [Header("Line Style")]
        [SerializeField] private Material lineMaterial;
        [SerializeField] private Material previewMaterial;
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = 100;
        [SerializeField] private float lineZ = -1f;

        [Header("Width Settings")]
        [SerializeField] private bool autoWidth = true;
        [SerializeField, Range(0.02f, 0.35f)] private float widthFactor = 0.1f;
        [SerializeField] private float minWidth = 0.01f;
        [SerializeField] private float maxWidth = 0.08f;
        [SerializeField] private bool screenSpaceWidth = false;
        [SerializeField] private float pixels = 3f;

        [Header("Animation")]
        [SerializeField] private float drawDuration = 0.15f;
        [SerializeField] private int capVertices = 6;
        [SerializeField] private int cornerVertices = 4;

        [Header("Events")]
        public UnityEvent OnWin;

        [Header("UI / Feedback")]
        [SerializeField] private GamePopup _winPopup;
        [SerializeField] private GamePopup _losePopup;
        [SerializeField] private float _loseAutoHide = 1.5f;

        [Tooltip("Что трясти при неправильном ответе (например, корень мини-игры).")]
        [SerializeField] private Transform _failShakeTarget;

        [Tooltip("Добавить тряску камеры при ошибке.")]
        [SerializeField] private bool _shakeCamera = true;

        [SerializeField] private float _failShakeDuration = 0.25f;
        [SerializeField] private float _failShakeStrength = 0.3f;
        [SerializeField] private int _failShakeVibrato = 15;
        [SerializeField] private float _failShakeRandomness = 90f;

        private readonly List<LineRenderer> _drawnLines = new();
        private readonly HashSet<ConnectionDot> _marked = new();
        private ConnectionDot _dragStart;
        private LineRenderer _preview;
        private float _computedWidth = 0.04f;

        private void Start() => RecomputeWidth();

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

            SpawnLine(a.transform.position, b.transform.position);
            a.Mark(); b.Mark();
            _marked.Add(a); _marked.Add(b);
        }

        [ContextMenu("Check Result (Editor)")]
        public void CheckResult()
        {
            foreach (var d in _marked)
            {
                if (!winPattern.Contains(d))
                {
                    FailFeedback();
                    return;
                }
            }

            foreach (var d in winPattern)
            {
                if (d == null || !d.IsMarked)
                {
                    FailFeedback();
                    return;
                }
            }

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

        // ===================== FEEDBACK =====================
        private void FailFeedback()
        {
            // 1️⃣ Тряска корня мини-игры
            if (_failShakeTarget)
            {
                _failShakeTarget.DOKill();
                _failShakeTarget.localScale = Vector3.one;
                _failShakeTarget.DOShakeScale(
                    _failShakeDuration,
                    _failShakeStrength * 0.4f,
                    _failShakeVibrato,
                    _failShakeRandomness
                );
            }

            // 2️⃣ Тряска камеры
            if (_shakeCamera && Camera.main)
            {
                var cam = Camera.main.transform;
                cam.DOKill();
                var original = cam.localPosition;
                cam.DOShakePosition(
                        _failShakeDuration,
                        _failShakeStrength,
                        _failShakeVibrato,
                        _failShakeRandomness,
                        false, true
                    )
                    .OnComplete(() => cam.localPosition = original);
            }

            // 3️⃣ Попап поражения
            if (_losePopup)
                _losePopup.Show(null, _loseAutoHide);
        }

        private void WinFeedback()
        {
            if (_winPopup)
                _winPopup.Show();
        }

        // ===================== Остальной код без изменений =====================
        private void SpawnLine(Vector3 a, Vector3 b)
        {
            if (!linePrefab) return;
            var lr = Instantiate(linePrefab, linesRoot ? linesRoot : transform);
            ApplyLineStyle(lr, preview: false);
            a.z = lineZ; b.z = lineZ;
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, a);
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
            if (preview) c.a = 0.7f;
            lr.startColor = lr.endColor = c;
            lr.sortingLayerName = sortingLayerName;
            lr.sortingOrder = sortingOrder;
        }

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

        private static bool AreAdjacent(ConnectionDot a, ConnectionDot b) => true;

        private void OnValidate()
        {
            if (Application.isPlaying) RecomputeWidth();
        }
    }
}
