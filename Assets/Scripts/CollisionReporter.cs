using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CollisionReporter : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private DamageZoneType damageZoneType = DamageZoneType.Body;

    void OnTriggerEnter2D(Collider2D other)
    {
        Report(other?.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Report(collision?.gameObject);
    }

    void Report(GameObject other)
    {
        if (other == null) return;

        var tag = other.tag;
        if (tag != "Package" && tag != "DropZone" && tag != "Obstacle" && tag != "Wall")
            return;

        var manager = GameManager.Instance;
        if (manager == null) return;

        switch (tag)
        {
            case "Package":
                manager.TryPickupPackage(other);
                break;
            case "DropZone":
                manager.TryDeliverPackage();
                break;
            case "Obstacle":
                if (damageZoneType == DamageZoneType.Rotor)
                    manager.TakeRotorHit();
                else
                    manager.TakeBodyHit();
                break;
            case "Wall":
                if (manager.IsFuelDepleted)
                    manager.TryFuelDepletedGroundHit();
                else if (damageZoneType == DamageZoneType.Rotor)
                    manager.TakeRotorHit();
                else
                    manager.TakeBodyHit();
                break;
        }
    }
}
