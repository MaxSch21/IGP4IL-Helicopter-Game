using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class HelicopterController : MonoBehaviour
{
    [Header("Horizontal Movement")]
    [Tooltip("Maximum horizontal speed in units/second")]
    public float maxHorizontalSpeed = 5f;

    [Tooltip("How quickly horizontal speed builds up")]
    public float horizontalAcceleration = 8f;

    [Tooltip("Maximum tilt angle in degrees")]
    public float maxTiltAngle = 30f;

    [Tooltip("How fast the helicopter tilts visually")]
    public float tiltSpeed = 5f;

    [Header("Vertical Movement")]
    [Tooltip("Each Up Arrow key press adds this much vertical speed")]
    public float verticalSpeedStep = 1f;

    [Tooltip("Maximum vertical speed, up or down")]
    public float maxVerticalSpeed = 8f;

    [Tooltip("How quickly vertical speed decays when no vertical key is held")]
    public float verticalDamping = 2f;

    [Header("Physics")]
    [Tooltip("Gravity scale applied to the Rigidbody2D")]
    public float gravityScale = 1f;

    private Rigidbody2D rb;
    private float currentHorizontalInput;
    private float currentVerticalSpeed;
    private bool inputEnabled = true;

    public float CurrentVerticalSpeed => currentVerticalSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        if (inputEnabled)
        {
            HandleKeyboardHorizontalInput();
            HandleKeyboardVerticalInput();
        }

        UpdateVisualTilt();
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    public void SetHorizontalInputFromArduino(float joystickX)
    {
        if (!inputEnabled)
            return;

        currentHorizontalInput = Mathf.Clamp(joystickX, -1f, 1f);
    }

    public void SetVerticalSpeedFromArduino(float mappedPotValue)
    {
        if (!inputEnabled)
            return;

        currentVerticalSpeed = Mathf.Clamp(mappedPotValue, -maxVerticalSpeed, maxVerticalSpeed);
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (enabled)
            return;

        currentHorizontalInput = 0f;
        currentVerticalSpeed = 0f;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void HandleKeyboardHorizontalInput()
    {
        currentHorizontalInput = 0f;

        if (Input.GetKey(KeyCode.A))
            currentHorizontalInput = -1f;
        else if (Input.GetKey(KeyCode.D))
            currentHorizontalInput = 1f;
    }

    private void HandleKeyboardVerticalInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
            currentVerticalSpeed += verticalSpeedStep;

        if (Input.GetKeyDown(KeyCode.DownArrow))
            currentVerticalSpeed -= verticalSpeedStep;

        currentVerticalSpeed = Mathf.Clamp(currentVerticalSpeed, -maxVerticalSpeed, maxVerticalSpeed);

        bool verticalKeyHeld = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow);
        if (!verticalKeyHeld)
            currentVerticalSpeed = Mathf.MoveTowards(currentVerticalSpeed, 0f, verticalDamping * Time.deltaTime);
    }

    private void ApplyMovement()
    {
        if (rb == null)
            return;

        float targetHorizontalVelocity = currentHorizontalInput * maxHorizontalSpeed;
        float newHorizontalVelocity = Mathf.MoveTowards(
            rb.linearVelocity.x,
            targetHorizontalVelocity,
            horizontalAcceleration * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(newHorizontalVelocity, currentVerticalSpeed);
    }

    private void UpdateVisualTilt()
    {
        float targetTiltAngle = -currentHorizontalInput * maxTiltAngle;
        float currentZ = transform.eulerAngles.z;

        if (currentZ > 180f)
            currentZ -= 360f;

        float newZ = Mathf.LerpAngle(currentZ, targetTiltAngle, tiltSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newZ);
    }
}
