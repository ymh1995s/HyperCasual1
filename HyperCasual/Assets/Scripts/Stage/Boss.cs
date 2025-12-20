using System.Collections;
using UnityEngine;
using DG.Tweening;

public class Boss : MonoBehaviour
{
    [Header("Handover")]
    [Tooltip("Where incoming items should be placed on the boss (assign a transform in inspector)")]
    [SerializeField] private Transform _itemReturnTransform;

    [Tooltip("Time between starting each item's receive (seconds)")]
    [SerializeField] private float _receiveInterval = 0.1f;

    [Header("Tween Settings")]
    [Tooltip("Duration of the incoming item tween")]
    [SerializeField] private float _tweenDuration = 0.3f;

    [Tooltip("Jump power for the parabolic arc")]
    [SerializeField] private float _jumpPower = 0.5f;

    [Tooltip("Number of jumps during the tween (usually 1)")]
    [SerializeField] private int _numJumps = 1;

    [Tooltip("Ease for the tween")]
    [SerializeField] private Ease _tweenEase = Ease.OutQuad;

    private bool _isReceiving = false;

    public Transform ItemReturnTransform => _itemReturnTransform;

    // Event to notify someone when all items have been received
    public System.Action OnAllItemsReceived;

    // Called by FinalArea when player reaches it
    public void StartReceivingFromPlayer(GameObject player)
    {
        if (_isReceiving)
            return;

        var inv = player.GetComponent<PlayerInventory>();
        if (inv == null)
        {
            Debug.LogWarning("PlayerInventory not found on player while starting handover.");
            return;
        }

        StartCoroutine(ReceiveItemsCoroutine(inv));
    }

    private IEnumerator ReceiveItemsCoroutine(PlayerInventory inv)
    {
        _isReceiving = true;

        while (inv != null && inv.HasStackedItems())
        {
            // Peek the top item without removing it so the inventory is updated only after tween completes
            var item = inv.PeekTopStackedItem();
            if (item == null)
                break;

            if (_itemReturnTransform != null)
            {
                // Ensure item is unparented so it moves in world space
                item.transform.SetParent(null);

                var rb = item.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.isKinematic = true;

                Vector3 targetPos = _itemReturnTransform.position;

                // Kill any existing tweens on the transform
                item.transform.DOKill();

                // Animate with DOJump for a parabolic arc and wait for completion
                var tweener = item.transform.DOJump(targetPos, _jumpPower, _numJumps, _tweenDuration)
                    .SetEase(_tweenEase);

                yield return tweener.WaitForCompletion();

                // After tween completes, inventory should remove and destroy the item
                inv.RemoveTopStackedItem(item);
            }
            else
            {
                // No return transform; just remove and destroy top item
                inv.RemoveTopStackedItem(item);
            }

            // Wait a bit before processing next item to space arrivals
            yield return new WaitForSeconds(_receiveInterval);
        }

        _isReceiving = false;

        // Notify listeners that boss finished receiving all items
        if (OnAllItemsReceived != null)
            OnAllItemsReceived.Invoke();
    }
}
