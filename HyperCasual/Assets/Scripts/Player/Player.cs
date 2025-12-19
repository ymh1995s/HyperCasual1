using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(PlayerInventory))]
public class Player : MonoBehaviour
{
    private PlayerController _controller;
    private PlayerInventory _inventory;

    // Exposed proxy fields so values can be edited on the Player GameObject in the Inspector
    [Header("Movement (Proxy to PlayerController)")]
    [SerializeField] private float _forwardSpeed = 5f;
    [SerializeField] private float _lateralSpeed = 10f;
    [SerializeField] private float _lateralSmooth = 10f;
    [SerializeField] private float _maxX = 3f;

    void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _inventory = GetComponent<PlayerInventory>();

        // Ensure controller exists and sync initial values
        if (_controller != null)
        {
            _controller.ForwardSpeed = _forwardSpeed;
            _controller.LateralSpeed = _lateralSpeed;
            _controller.LateralSmooth = _lateralSmooth;
            _controller.MaxX = _maxX;
        }
    }

    // Called in the editor when values are changed in Inspector
    void OnValidate()
    {
        // Try to get the controller in edit-time as well
        if (_controller == null)
            _controller = GetComponent<PlayerController>();

        if (_controller != null)
        {
            _controller.ForwardSpeed = _forwardSpeed;
            _controller.LateralSpeed = _lateralSpeed;
            _controller.LateralSmooth = _lateralSmooth;
            _controller.MaxX = _maxX;
        }
    }

    // Preserve existing Interactable integration: delegate to inventory
    public void PickupItem(ItemType type, int value)
    {
        if (_inventory != null)
            _inventory.PickupItem(type, value);
    }

    // Optional helpers for other systems
    public PlayerController Controller => _controller;
    public PlayerInventory Inventory => _inventory;
}
