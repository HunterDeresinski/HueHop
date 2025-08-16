using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class ColorPickup : MonoBehaviour
{
    public PlayerController.PlayerColor color = PlayerController.PlayerColor.Green;
    public bool destroyOnPickup = true;

    void Reset()        { Configure(); }
    void OnValidate()   { Configure(); }

    private void Configure()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        int pickupLayer = LayerMask.NameToLayer("Pickup");
        if (pickupLayer != -1)
            gameObject.layer = pickupLayer;
        else
            Debug.LogWarning("Create a layer named 'Pickup' and enable Player_* ↔ Pickup in Physics 2D → Layer Collision Matrix.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // robustly find the player even if collider is on a child
        var player = other.attachedRigidbody
                    ? other.attachedRigidbody.GetComponent<PlayerController>()
                    : other.GetComponentInParent<PlayerController>();

        if (!player) return;

        player.SetPlayerColor(color);   // swaps visuals + player layer + ground mask

        if (destroyOnPickup)
            Destroy(gameObject);
    }
}