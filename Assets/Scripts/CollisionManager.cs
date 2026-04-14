using UnityEngine;

public class CollisionManager : MonoBehaviour
{
    public static CollisionManager Instance { get; private set; }
    public bool hasPackage { get; private set; }
    public event System.Action PackagePickedUp;
    public event System.Action PackageDelivered;
    public event System.Action ObstacleHit;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ReportCollision(string tag, GameObject obj)
    {
        Debug.Log($"Collision reported: {tag} with {obj?.name}");

        switch (tag)
        {
            case "Package":
                if (!hasPackage)
                {
                    hasPackage = true;
                    Debug.Log("Package picked up");
                    PackagePickedUp?.Invoke();
                }
                else
                {
                    Debug.LogWarning("Already carrying package");
                }
                break;
            case "DropZone":
                if (hasPackage)
                {
                    hasPackage = false;
                    Debug.Log("Package delivered");
                    PackageDelivered?.Invoke();
                }
                else
                {
                    Debug.LogWarning("No package to deliver");
                }
                break;
            case "Obstacle":
                Debug.Log("Hit obstacle");
                ObstacleHit?.Invoke();
                break;
            default:
                Debug.LogWarning($"Unhandled collision tag: {tag}");
                break;
        }
    }
}
