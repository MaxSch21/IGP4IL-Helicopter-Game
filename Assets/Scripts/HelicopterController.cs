using UnityEngine;

/// <summary>
/// Helicopter Controller - 2D Prototype
/// Keyboard controls (later replaced by Arduino input):
///   A / D        => Tilt left / right (Joystick)
///   Up / Down    => Altitude control (Potentiometer)
///                   Press UP multiple times to gain upward speed (simulates analog ramp-up)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class HelicopterController : MonoBehaviour
{
    [Header("Horizontal Movement (Joystick / A-D Keys)")]
    [Tooltip("Maximum horizontal speed in units/second")]
    public float maxHorizontalSpeed = 5f;

    [Tooltip("How quickly horizontal speed builds up")]
    public float horizontalAcceleration = 8f;

    [Tooltip("Maximum tilt angle in degrees")]
    public float maxTiltAngle = 30f;

    [Tooltip("How fast the helicopter tilts visually")]
    public float tiltSpeed = 5f;

    [Header("Vertical Movement (Potentiometer / Arrow Keys)")]
    [Tooltip("Each UP keypress adds this much vertical speed (simulates potentiometer ramp-up)")]
    public float verticalSpeedStep = 1f;

    [Tooltip("Maximum vertical speed (up or down)")]
    public float maxVerticalSpeed = 8f;

    [Tooltip("How quickly vertical speed decays when no input is given")]
    public float verticalDamping = 2f;

    [Header("Physics")]
    [Tooltip("Gravity scale applied to the Rigidbody2D")]
    public float gravityScale = 1f;

    // ── Private state ────────────────────────────────────────────────
    private Rigidbody2D rb;
    private float currentVerticalSpeed = 0f;   // accumulates with arrow presses
    private float currentHorizontalInput = 0f; // -1 to 1 from A/D
    private float targetTiltAngle = 0f;

    // ── Arduino serial (optional - stub for future use) ──────────────
    // When you integrate Arduino, read joystick X -> currentHorizontalInput (-1..1)
    // and potentiometer -> currentVerticalSpeed (mapped to -maxVerticalSpeed..maxVerticalSpeed)

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // we handle rotation visually
    }

    void Update()
    {
        HandleHorizontalInput();
        HandleVerticalInput();
        UpdateVisualTilt();
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    // ── Input Handlers ───────────────────────────────────────────────

    /// <summary>
    /// A/D keys simulate the joystick X-axis.
    /// Later: replace with Arduino joystick mapped to -1..1
    /// </summary>
    void HandleHorizontalInput()
    {
        currentHorizontalInput = 0f;

        if (Input.GetKey(KeyCode.A))
            currentHorizontalInput = -1f;
        else if (Input.GetKey(KeyCode.D))
            currentHorizontalInput = 1f;
    }

    /// <summary>
    /// UP arrow adds upward speed (each press = +step), DOWN arrow adds downward speed.
    /// This simulates a potentiometer that ramps up when turned further.
    /// Later: replace with Arduino potentiometer value mapped to vertical speed.
    /// </summary>
    void HandleVerticalInput()
    {
        // Each key-down event nudges the vertical speed
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentVerticalSpeed += verticalSpeedStep;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentVerticalSpeed -= verticalSpeedStep;
        }

        // Clamp to max
        currentVerticalSpeed = Mathf.Clamp(currentVerticalSpeed, -maxVerticalSpeed, maxVerticalSpeed);

        // When no vertical input is held, decay speed back toward zero
        bool verticalKeyHeld = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow);
        if (!verticalKeyHeld)
        {
            currentVerticalSpeed = Mathf.MoveTowards(
                currentVerticalSpeed, 0f, verticalDamping * Time.deltaTime);
        }
    }

    // ── Physics & Visual ─────────────────────────────────────────────

    void ApplyMovement()
    {
        float targetHorizontalVelocity = currentHorizontalInput * maxHorizontalSpeed;

        float newVx = Mathf.MoveTowards(
            rb.linearVelocity.x,
            targetHorizontalVelocity,
            horizontalAcceleration * Time.fixedDeltaTime);

        // Vertical: override gravity with our controlled speed
        rb.linearVelocity = new Vector2(newVx, currentVerticalSpeed);
    }

    /// <summary>
    /// Tilts the sprite based on horizontal movement (visual only, no physics rotation).
    /// </summary>
    void UpdateVisualTilt()
    {
        targetTiltAngle = -currentHorizontalInput * maxTiltAngle;

        float currentZ = transform.eulerAngles.z;
        // Convert to signed angle
        if (currentZ > 180f) currentZ -= 360f;

        float newZ = Mathf.LerpAngle(currentZ, targetTiltAngle, tiltSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newZ);
    }

    // ── Public API for Arduino Integration ──────────────────────────

    /// <summary>
    /// Call this from your Arduino serial reader to set horizontal input.
    /// joystickX should be in range -1 (full left) to 1 (full right).
    /// </summary>
    public void SetHorizontalInputFromArduino(float joystickX)
    {
        currentHorizontalInput = Mathf.Clamp(joystickX, -1f, 1f);
    }

    /// <summary>
    /// Call this from your Arduino serial reader to set vertical speed directly.
    /// potValue should be mapped to range -maxVerticalSpeed to maxVerticalSpeed.
    /// </summary>
    public void SetVerticalSpeedFromArduino(float mappedPotValue)
    {
        currentVerticalSpeed = Mathf.Clamp(mappedPotValue, -maxVerticalSpeed, maxVerticalSpeed);
    }

    // ── Debug Gizmos ─────────────────────────────────────────────────

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 250, 100));
        GUILayout.Label($"Horizontal Input: {currentHorizontalInput:F2}");
        GUILayout.Label($"Vertical Speed:   {currentVerticalSpeed:F2}");
        GUILayout.Label($"Velocity:         {rb.linearVelocity}");
        GUILayout.EndArea();
    }
}