using UnityEngine;

// Deprecated compatibility shim.
// Active collision routing is handled only by CollisionReporter, which forwards to GameManager.
// This class remains empty to avoid missing-script references in legacy recovery scenes/assets.
[System.Obsolete("CollisionManager is unused. Use CollisionReporter for collision forwarding.")]
public class CollisionManager : MonoBehaviour
{
}
