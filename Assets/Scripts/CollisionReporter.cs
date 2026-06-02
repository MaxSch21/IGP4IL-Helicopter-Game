using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CollisionReporter : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private DamageZoneType damageZoneType = DamageZoneType.Body;

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other?.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision?.gameObject);
    }

    private void HandleHit(GameObject other)
    {
        if (other == null)
            return;

        GameManager manager = GameManager.Instance;
        if (manager == null)
            return;

        if (other.CompareTag("Package"))
        {
            manager.TryPickupPackage(other);
            return;
        }

        if (other.CompareTag("DropZone"))
        {
            manager.TryDeliverPackage();
            return;
        }

        if (other.CompareTag("Obstacle") || other.CompareTag("Wall"))
        {
            if (manager.CurrentState == GameManager.GameState.FuelDepleted)
            {
                manager.TryFuelDepletedGroundHit();
                return;
            }

            HandleDamageHit(manager);
        }
    }

    private void HandleDamageHit(GameManager manager)
    {
        if (damageZoneType == DamageZoneType.Rotor)
        {
            manager.TakeRotorHit();
            return;
        }

        manager.TakeBodyHit();
    }
}
