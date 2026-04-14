using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CollisionReporter : MonoBehaviour
{
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
        if (tag != "Package" && tag != "DropZone" && tag != "Obstacle")
            return;

        var manager = CollisionManager.Instance;
        if (manager == null) return;

        manager.ReportCollision(tag, other);
    }
}
