using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // 이동 설정 (인스펙터에서 조정)
    [SerializeField] private float _forwardSpeed = 5f;
    [SerializeField] private float _lateralSpeed = 10f; // 화면 드래그 대비 이동 속도
    [SerializeField] private float _lateralSmooth = 10f; // 수평 이동 부드러움
    [SerializeField] private float _maxX = 3f; // 좌우 한계 위치

    // Expose properties so other components (Player) can sync editable values
    public float ForwardSpeed { get => _forwardSpeed; set => _forwardSpeed = value; }
    public float LateralSpeed { get => _lateralSpeed; set => _lateralSpeed = value; }
    public float LateralSmooth { get => _lateralSmooth; set => _lateralSmooth = value; }
    public float MaxX { get => _maxX; set => _maxX = value; }

    // 내부 상태
    private Vector2 _lastPointerPosition;
    private float _targetX;
    private bool _isDragging;

    void Start()
    {
        _targetX = transform.position.x;
    }

    void Update()
    {
        HandleInput();

        // 항상 앞으로 이동
        transform.position += Vector3.forward * _forwardSpeed * Time.deltaTime;

        // 부드럽게 목표 X로 이동
        float newX = Mathf.Lerp(transform.position.x, _targetX, Time.deltaTime * _lateralSmooth);
        newX = Mathf.Clamp(newX, -_maxX, _maxX);

        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    void HandleInput()
    {
        // 터치 우선 처리 (모바일)
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                _isDragging = true;
                _lastPointerPosition = t.position;
            }
            else if (t.phase == TouchPhase.Moved && _isDragging)
            {
                Vector2 delta = t.position - _lastPointerPosition;
                ApplyHorizontalDelta(delta.x);
                _lastPointerPosition = t.position;
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                _isDragging = false;
            }

            return;
        }

        // 마우스 입력 (에디터/PC)
        if (Input.GetMouseButtonDown(0))
        {
            _isDragging = true;
            _lastPointerPosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(0) && _isDragging)
        {
            Vector2 current = (Vector2)Input.mousePosition;
            Vector2 delta = current - _lastPointerPosition;
            ApplyHorizontalDelta(delta.x);
            _lastPointerPosition = current;
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
        }
    }

    void ApplyHorizontalDelta(float deltaX)
    {
        // 화면 픽셀 이동량을 정규화하여 기기 해상도에 관계없이 동작하도록 함
        float norm = deltaX / (float)Screen.width;
        float move = norm * _lateralSpeed;

        _targetX += move;
        _targetX = Mathf.Clamp(_targetX, -_maxX, _maxX);
    }
}
