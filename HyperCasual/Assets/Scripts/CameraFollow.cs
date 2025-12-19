using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 5f, -10f);
    [SerializeField] private float _smooth = 5f;
    [SerializeField] private bool _followX = true;
    [SerializeField] private bool _followY = true; // NEW: option to follow Y axis
    [SerializeField] private bool _followZ = true;

    void Start()
    {
        if (_target == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
                _target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (_target == null)
            return;

        Vector3 desired = _target.position + _offset;
        Vector3 current = transform.position;

        Vector3 targetPos = current;
        if (_followX) targetPos.x = desired.x;
        if (_followY) targetPos.y = desired.y; // follow Y only when enabled
        if (_followZ) targetPos.z = desired.z;

        transform.position = Vector3.Lerp(current, targetPos, Time.deltaTime * _smooth);

        // Optional: look at target (comment out if not desired)
        transform.LookAt(_target);
    }
}
