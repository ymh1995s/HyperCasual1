using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Drone : MonoBehaviour
{
    [Header("참조 (References)")]
    [Tooltip("드론이 들고 있는 총(하위 오브젝트를 재귀적으로 탐색하여 찾음)")]
    [SerializeField] private Transform _gun;
    [Tooltip("프로펠러 Transform(하위 오브젝트를 재귀적으로 탐색하여 찾음)")]
    [SerializeField] private Transform _propeller;

    [Header("호버 / 고도 (Hover / Altitude)")]
    [Tooltip("허용되는 최소 고도 (Y)")]
    [SerializeField] private float _minAltitude = 1.5f;
    [Tooltip("허용되는 최대 고도 (Y)")]
    [SerializeField] private float _maxAltitude = 3f;
    [Tooltip("호버 진폭: 위아래 흔들림 크기 (Inspector에서 조절 가능)")]
    [Range(0f, 1f)]
    [SerializeField] private float _hoverAmplitude = 0.2f;
    [Tooltip("호버 반주기(초): -진폭에서 +진폭으로 이동하는 데 걸리는 시간. 값이 클수록 느리게 움직입니다 (Inspector에서 조절 가능)")]
    [Range(0.1f, 10f)]
    [SerializeField] private float _hoverDuration = 4f;
    [Tooltip("호버 효과 사용 여부 (케거나 끌 수 있음)")]
    [SerializeField] private bool _useHover = true;
    [Tooltip("호버 Y 보간 스무스 계수. 값이 클수록 Y 보간이 더 빠르고 부드럽습니다.")]
    [Range(1f, 30f)]
    [SerializeField] private float _hoverSmooth = 8f;

    [Header("이동 (Movement)")]
    [Tooltip("이동 속도(거리 기반으로 duration 계산에 사용)")]
    [SerializeField] private float _moveSpeed = 3f; // used to calculate duration
    [Tooltip("플레이어로부터 최대 허용 거리 (플레이어에서 멀어지지 않도록 함)")]
    [SerializeField] private float _maxDistanceFromPlayer = 8f;
    [Tooltip("플레이어 주변 순회 반경")]
    [SerializeField] private float _patrolRadius = 4f;
    [Tooltip("순회 지점 갱신 주기(초)")]
    [Range(0.1f, 30f)]
    [SerializeField] private float _patrolInterval = 2f;
    [Tooltip("순회 지점 최초 지연(초)")]
    [Range(0f, 10f)]
    [SerializeField] private float _patrolInitialDelay = 1.5f;
    [Tooltip("이동시 사용되는 이징 (DoTween Ease)")]
    [SerializeField] private Ease _movementEase = Ease.InOutSine;

    [Header("프로펠러 (Propeller)")]
    [Tooltip("프로펠러 회전 속도 (도/초)")]
    [SerializeField] private float _propellerSpeed = 720f; // degrees per second

    [Header("검색 / 타게팅 (Search / Targeting)")]
    [Tooltip("적 탐색 범위")]
    [SerializeField] private float _searchRange = 12f;
    [Tooltip("적 탐색 주기(초)")]
    [SerializeField] private float _searchInterval = 0.6f;
    [Tooltip("타겟을 고정하는 최소 시간(초)")]
    [SerializeField] private float _minTargetLockTime = 2f;
    [Tooltip("타겟을 조준할 때 허용되는 최대 피치(세로 회전) 각도")]
    [SerializeField] private float _maxPitchForTarget = 45f; // degrees

    [Header("기즈모 (Gizmos)")]
    [Tooltip("기즈모 표시 여부")]
    [SerializeField] private bool _drawGizmos = true;
    [Tooltip("검색 범위 기즈모 색상")]
    [SerializeField] private Color _searchGizmoColor = new Color(1f, 0.5f, 0.0f, 0.25f);
    [Tooltip("타겟 기즈모 색상")]
    [SerializeField] private Color _targetGizmoColor = Color.red;

    // 캐싱된 초기 상태
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    // 내부 상태
    private Transform _player;
    private Transform _targetEnemy;
    private float _lastTargetSetTime = -999f;

    private float _hoverOffset = 0f;
    // 베이스 Y: 호버를 적용하는 중심 고도 (호버 오프셋과 합쳐서 실제 Y가 됨)
    private float _baseY = 0f;
    private Tween _hoverTween;
    private Tween _movementTween;

    private Vector3 _nextPatrolPoint;
    private bool _isMoving = false;

    private System.Random _rng = new System.Random();

    // 이전 값 캐시 (인스펙터에서 런타임에 변경시 반영하기 위함)
    private float _prevHoverAmplitude;
    private float _prevHoverDuration;
    private bool _prevUseHover;

    // Awake: 참조 자동 할당 (하위 오브젝트는 재귀적으로 탐색)
    void Awake()
    {
        if (_gun == null)
            _gun = FindDeepChild(transform, "Gun");
        if (_propeller == null)
            _propeller = FindDeepChild(transform, "Propeller");

        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;

        // cache initial transform so resets can restore
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
    }

    // Start: 호버 트윈 및 주기적 탐색/순회 시작
    void Start()
    {
        // 초기 호버 오프셋을 -진폭으로 설정하여 왕복 시작 위치를 맞춤
        _hoverOffset = -_hoverAmplitude;

        // 베이스Y 초기화: 현재 위치를 기반으로 하되 허용 범위 내에서 조정
        float desiredBase = transform.position.y;
        float minBase = _minAltitude + _hoverAmplitude;
        float maxBase = _maxAltitude - _hoverAmplitude;
        _baseY = Mathf.Clamp(desiredBase, minBase, maxBase);

        // 캐시 초기값
        _prevHoverAmplitude = _hoverAmplitude;
        _prevHoverDuration = _hoverDuration;
        _prevUseHover = _useHover;

        SetupHoverTween();

        // 주기적인 적 탐색 시작
        InvokeRepeating(nameof(SearchForEnemies), 0.2f, _searchInterval);

        // 초기 순회 지점 설정 및 주기적 갱신
        PickNewPatrolPoint();
        InvokeRepeating(nameof(PickNewPatrolPoint), _patrolInitialDelay, _patrolInterval);
    }

    // Update: 프로펠러 회전, 타겟 조준, 이동 시작 처리
    void Update()
    {
        // 인스펙터에서 런타임에 호버 관련 값이 바뀌면 트윈을 재설정
        if (_prevHoverAmplitude != _hoverAmplitude || _prevHoverDuration != _hoverDuration || _prevUseHover != _useHover)
        {
            _prevHoverAmplitude = _hoverAmplitude;
            _prevHoverDuration = _hoverDuration;
            _prevUseHover = _useHover;
            // clamp offset to new amplitude to avoid jumps
            _hoverOffset = Mathf.Clamp(_hoverOffset, -_hoverAmplitude, _hoverAmplitude);
            SetupHoverTween();
        }

        // 프로펠러 회전 (Z축)
        if (_propeller != null)
        {
            _propeller.Rotate(0f, 0f, _propellerSpeed * Time.deltaTime, Space.Self);
        }

        // 플레이어 참조 갱신
        if (_player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null)
                _player = p.transform;
        }

        // 타겟이 있는 경우: 타겟을 향해 회전, 피치 제한 적용
        if (_targetEnemy != null)
        {
            if (!IsValidTarget(_targetEnemy))
            {
                ClearTarget();
            }
            else
            {
                Vector3 dir = (_targetEnemy.position - transform.position).normalized;
                Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
                float requiredPitch = NormalizeAngle(look.eulerAngles.x);
                if (Mathf.Abs(requiredPitch) > _maxPitchForTarget)
                {
                    // 피치가 너무 크면 포기
                    ClearTarget();
                }
                else
                {
                    // 부드럽게 타겟을 바라보도록 회전
                    transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 4f);
                }
            }
        }
        else
        {
            // 타겟이 없을 때는 전방을 유지 (-10 ~ 10 Y 회전)
            Vector3 e = transform.eulerAngles;
            float desiredY = Mathf.Clamp(NormalizeAngle(e.y), -10f, 10f);
            Quaternion aim = Quaternion.Euler(0f, desiredY, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, aim, Time.deltaTime * 1.5f);
        }

        // 이동: 현재 이동중이 아니라면 목표로 이동 시작
        if (!_isMoving)
        {
            Vector3 goal = _nextPatrolPoint;

            // 플레이어로부터 너무 멀어지면 플레이어 쪽으로 보정
            if (_player != null)
            {
                float distToPlayer = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(_player.position.x, 0f, _player.position.z));
                if (distToPlayer > _maxDistanceFromPlayer)
                {
                    Vector3 dirToPlayer = (_player.position - transform.position).normalized;
                    goal = _player.position - dirToPlayer * (_maxDistanceFromPlayer * 0.6f);
                }
            }

            MoveTo(goal);
        }
    }

    // SetupHoverTween: 현재 설정값으로 호버 트윈을 생성하거나 해제
    private void SetupHoverTween()
    {
        if (_hoverTween != null)
        {
            _hoverTween.Kill();
            _hoverTween = null;
        }

        if (!_useHover)
        {
            // 호버 비활성화: 오프셋을 0으로 점진적으로 맞춤
            _hoverOffset = 0f;
            return;
        }

        // 초기 오프셋은 -amp로 설정
        _hoverOffset = Mathf.Clamp(_hoverOffset, -_hoverAmplitude, _hoverAmplitude);

        // Ensure baseY respects min/max considering amplitude
        float minBase = _minAltitude + _hoverAmplitude;
        float maxBase = _maxAltitude - _hoverAmplitude;
        _baseY = Mathf.Clamp(_baseY, minBase, maxBase);

        // 부드러운 왕복 트윈 생성
        _hoverTween = DOTween.To(() => _hoverOffset, x => _hoverOffset = x, _hoverAmplitude, _hoverDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    // LateUpdate: 호버 오프셋을 Y에 적용하고 고도 범위를 유지
    void LateUpdate()
    {
        if (!_useHover)
            return;

        // 목표 Y는 베이스Y + 오프셋
        float targetY = _baseY + _hoverOffset;
        // 안전하게 최종 클램프
        targetY = Mathf.Clamp(targetY, _minAltitude, _maxAltitude);

        // 부드러운 보간으로 Y를 이동시켜 순간이동/뚝 끊기는 현상 완화
        float smoothedY = Mathf.Lerp(transform.position.y, targetY, Time.deltaTime * _hoverSmooth);
        transform.position = new Vector3(transform.position.x, smoothedY, transform.position.z);
    }

    // DoTween 기반 이동 (X,Z만 애니메이션하여 Y축 호버와 충돌하지 않도록 함)
    private void MoveTo(Vector3 worldPos)
    {
        if (_movementTween != null && _movementTween.IsActive())
            _movementTween.Kill();

        Vector3 target = new Vector3(worldPos.x, transform.position.y, worldPos.z);

        float distance = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(worldPos.x, 0f, worldPos.z));
        float duration = Mathf.Max(0.2f, distance / _moveSpeed);

        _isMoving = true;

        // X와 Z를 따로 트윈하여 Y축 호버와 충돌을 방지
        var tX = transform.DOMoveX(target.x, duration).SetEase(_movementEase);
        var tZ = transform.DOMoveZ(target.z, duration).SetEase(_movementEase);
        _movementTween = DOTween.Sequence().Join(tX).Join(tZ).OnComplete(() => _isMoving = false);
    }

    // 플레이어 주변 순회 지점 선택
    private void PickNewPatrolPoint()
    {
        if (_player == null)
            return;

        float angle = (float)(_rng.NextDouble() * Math.PI * 2.0);
        float radius = (float)(_rng.NextDouble() * _patrolRadius);
        Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        _nextPatrolPoint = _player.position + offset;
    }

    // 주기적 적 탐색 (가까운 적에 가중치를 더 줌)
    private void SearchForEnemies()
    {
        if (_player == null)
            return;

        if (_targetEnemy != null && Time.time - _lastTargetSetTime < _minTargetLockTime)
            return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies == null || enemies.Length == 0)
            return;

        List<Transform> candidates = new List<Transform>();
        List<float> weights = new List<float>();

        foreach (var go in enemies)
        {
            if (go == null) continue;
            var t = go.transform;

            float planarDist = Vector3.Distance(new Vector3(t.position.x, 0f, t.position.z), new Vector3(transform.position.x, 0f, transform.position.z));
            if (planarDist > _searchRange) continue;

            if (_player != null && t.position.z <= _player.position.z) continue; // 플레이어 앞에 있는 적만

            candidates.Add(t);
            float w = 1f / (planarDist + 0.01f);
            weights.Add(w);
        }

        if (candidates.Count == 0) return;

        float total = 0f;
        foreach (var w in weights) total += w;
        float pick = (float)(_rng.NextDouble() * total);
        float accum = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            accum += weights[i];
            if (pick <= accum)
            {
                SetTarget(candidates[i]);
                return;
            }
        }

        SetTarget(candidates[candidates.Count - 1]);
    }

    // 타겟 설정
    private void SetTarget(Transform t)
    {
        if (t == null) return;
        _targetEnemy = t;
        _lastTargetSetTime = Time.time;
    }

    // 타겟 해제
    private void ClearTarget()
    {
        _targetEnemy = null;
    }

    // 타겟 유효성 검사
    private bool IsValidTarget(Transform t)
    {
        if (t == null) return false;
        float planarDist = Vector3.Distance(new Vector3(t.position.x, 0f, t.position.z), new Vector3(transform.position.x, 0f, transform.position.z));
        if (planarDist > _searchRange * 1.2f) return false;
        return t.gameObject.activeInHierarchy;
    }

    // 각도 정규화 (-180 ~ 180)
    private static float NormalizeAngle(float a)
    {
        if (a > 180f) a -= 360f;
        return a;
    }

    // 하위 오브젝트를 재귀적으로 찾는 유틸리티
    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null) return null;
        // BFS
        var queue = new Queue<Transform>();
        queue.Enqueue(parent);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.name == name)
                return current;
            for (int i = 0; current != null && i < current.childCount; i++)
                queue.Enqueue(current.GetChild(i));
        }
        return null;
    }

    // 기즈모 표시
    void OnDrawGizmosSelected()
    {
        if (!_drawGizmos) return;

        Gizmos.color = _searchGizmoColor;
        Gizmos.DrawWireSphere(transform.position, _searchRange);

        if (_targetEnemy != null)
        {
            Gizmos.color = _targetGizmoColor;
            Gizmos.DrawLine(transform.position, _targetEnemy.position);
            Gizmos.DrawWireSphere(_targetEnemy.position, 0.3f);
        }
    }

    void OnDestroy()
    {
        if (_hoverTween != null) _hoverTween.Kill();
        if (_movementTween != null) _movementTween.Kill();
    }

    // Public: reset drone to initial start state (used by StageController when restarting)
    public void ResetToStart()
    {
        // Stop tweens
        if (_hoverTween != null)
        {
            _hoverTween.Kill();
            _hoverTween = null;
        }
        if (_movementTween != null)
        {
            _movementTween.Kill();
            _movementTween = null;
        }

        // Cancel invokes
        CancelInvoke(nameof(SearchForEnemies));
        CancelInvoke(nameof(PickNewPatrolPoint));

        // restore transform
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;

        // reset internal flags
        _isMoving = false;
        _targetEnemy = null;
        _lastTargetSetTime = -999f;

        // reset hover and baseY
        _hoverOffset = -_hoverAmplitude;
        float desiredBase = transform.position.y;
        float minBase = _minAltitude + _hoverAmplitude;
        float maxBase = _maxAltitude - _hoverAmplitude;
        _baseY = Mathf.Clamp(desiredBase, minBase, maxBase);

        // restart periodic invokes
        InvokeRepeating(nameof(SearchForEnemies), 0.2f, _searchInterval);
        PickNewPatrolPoint();
        InvokeRepeating(nameof(PickNewPatrolPoint), _patrolInitialDelay, _patrolInterval);

        // restart hover tween if enabled
        SetupHoverTween();
    }
}
