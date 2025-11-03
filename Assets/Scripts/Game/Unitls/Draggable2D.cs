// Draggable2D.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Draggable2D : MonoBehaviour
{
    [Header("Drag settings")]
    [SerializeField] private float followSpeed = 30f; // чем больше, тем жёстче следует
    [SerializeField] private bool constrainToZ = true;

    private Rigidbody2D _rb;
    private Camera _cam;
    private bool _dragging;
    private Vector3 _grabOffsetWorld;
    private float _zAtGrab;
    private Vector2 _targetPos;
    private int? _activePointerId;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.isKinematic = true;
        _rb.gravityScale = 0f;
        _cam = Camera.main;
    }

    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouse();
#else
        HandleTouch();
#endif
    }

    private void FixedUpdate()
    {
        if (!_dragging) return;
        var next = Vector2.Lerp(_rb.position, _targetPos, followSpeed * Time.fixedDeltaTime);
        _rb.MovePosition(next);
    }

    // ---------- Mouse ----------
    private void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (HitThisAt(Input.mousePosition))
                BeginDrag(ScreenToWorld(Input.mousePosition));
        }
        else if (Input.GetMouseButton(0) && _dragging)
        {
            UpdateDrag(ScreenToWorld(Input.mousePosition));
        }
        else if (Input.GetMouseButtonUp(0) && _dragging)
        {
            EndDrag();
        }
    }

    // ---------- Touch ----------
    private void HandleTouch()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);

            if (!_dragging && t.phase == TouchPhase.Began && HitThisAt(t.position))
            {
                _activePointerId = t.fingerId;
                BeginDrag(ScreenToWorld(t.position));
                return;
            }

            if (_dragging && _activePointerId == t.fingerId)
            {
                if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                    UpdateDrag(ScreenToWorld(t.position));
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    EndDrag();
                return;
            }
        }
    }

    private bool HitThisAt(Vector2 screenPos)
    {
        if (_cam == null) _cam = Camera.main;
        var world = ScreenToWorld(screenPos);
        var hit = Physics2D.OverlapPoint(world);
        return hit && hit.transform.IsChildOf(transform);
    }

    private Vector3 ScreenToWorld(Vector2 screen)
    {
        if (_cam == null) _cam = Camera.main;
        var z = constrainToZ ? _zAtGrab : Mathf.Abs(_cam.transform.position.z);
        var w = _cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(z - _cam.transform.position.z)));
        if (constrainToZ) w.z = _zAtGrab;
        return w;
    }

    private void BeginDrag(Vector3 worldAtPointer)
    {
        _dragging = true;
        _zAtGrab = transform.position.z;
        _grabOffsetWorld = transform.position - worldAtPointer;
        _targetPos = transform.position;
    }

    private void UpdateDrag(Vector3 worldAtPointer)
    {
        _targetPos = worldAtPointer + _grabOffsetWorld;
    }

    private void EndDrag()
    {
        _dragging = false;
        _activePointerId = null;
    }
}
