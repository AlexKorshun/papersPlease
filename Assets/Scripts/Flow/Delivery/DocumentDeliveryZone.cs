using UnityEngine;

/// <summary>
/// Place this on a 2D trigger collider that represents the "hand over documents" zone.
/// </summary>
public class DocumentDeliveryZone : MonoBehaviour
{
    [Tooltip("Optional: where delivered documents are moved before being hidden/destroyed.")]
    [SerializeField] private Transform deliveredPoint;

    public Transform DeliveredPoint => deliveredPoint;
}

