using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class HelicopterController : MonoBehaviour
{
    public enum InputSourceMode
    {
        KeyboardOnly,
        ExternalOnly
    }

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

    [Header("Crash")]
    [SerializeField, Min(0f)] private float crashGravityScale = 20f;

    [Header("Direction Visuals")]
    [SerializeField] private GameObject facingRightObject;
    [SerializeField] private GameObject facingLeftObject;
    [SerializeField] private GameObject turningObject;
    [SerializeField] private Collider2D facingRightCollider;
    [SerializeField] private Collider2D facingLeftCollider;
    [SerializeField] private Collider2D turningCollider;
    [SerializeField, Min(0f)] private float turnInputHoldTime = 0.25f;
    [SerializeField, Min(0f)] private float turnFrameTime = 0.15f;

    [Header("Input")]
    [SerializeField] private InputSourceMode inputSourceMode = InputSourceMode.KeyboardOnly;
    [SerializeField] private bool allowKeyboardInputWithExternal = true;
    [SerializeField, Range(0f, 1f)] private float externalInputDeadzone = 0.08f;
    [SerializeField] private bool invertVisualTilt = true;

    private Rigidbody2D rb;
    private float currentHorizontalInput;
    private float currentVerticalSpeed;
    private float keyboardHorizontalInput;
    private float keyboardVerticalSpeed;
    private float externalHorizontalInput;
    private float externalVerticalSpeed;
    private bool inputEnabled = true;
    private float baseGravityScale;
    private bool crashMode;
    private int facingDirection = 1;
    private float oppositeInputTime;
    private bool isTurning;
    private Coroutine turnRoutine;

    public float CurrentVerticalSpeed => currentVerticalSpeed;
    public float CurrentHorizontalInput => currentHorizontalInput;
    public int CurrentFacingDirection => isTurning ? 0 : facingDirection;
    public event Action<int> OnDirectionVisualChanged;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseGravityScale = gravityScale;
        rb.gravityScale = gravityScale;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        SetDirectionVisual(facingDirection);
    }

    void Update()
    {
        if (inputEnabled)
        {
            if (inputSourceMode == InputSourceMode.KeyboardOnly || allowKeyboardInputWithExternal)
            {
                HandleKeyboardHorizontalInput();
                HandleKeyboardVerticalInput();
            }

            UpdateCurrentInput();
        }

        UpdateDirectionVisuals();
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

        externalHorizontalInput = ApplyDeadzoneAndClamp(joystickX);
        UpdateCurrentInput();
    }

    public void SetVerticalSpeedFromArduino(float mappedPotValue)
    {
        if (!inputEnabled)
            return;

        externalVerticalSpeed = Mathf.Clamp(mappedPotValue, -maxVerticalSpeed, maxVerticalSpeed);
        UpdateCurrentInput();
    }

    public void SetExternalInput(float horizontalInput, float verticalSpeed)
    {
        SetHorizontalInputFromArduino(horizontalInput);
        SetVerticalSpeedFromArduino(verticalSpeed);
    }

    public void SetInputSourceMode(InputSourceMode mode)
    {
        inputSourceMode = mode;

        if (mode == InputSourceMode.KeyboardOnly)
            return;

        externalHorizontalInput = ApplyDeadzoneAndClamp(externalHorizontalInput);
        UpdateCurrentInput();
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (enabled)
            return;

        currentHorizontalInput = 0f;
        currentVerticalSpeed = 0f;
        keyboardHorizontalInput = 0f;
        keyboardVerticalSpeed = 0f;
        externalHorizontalInput = 0f;
        externalVerticalSpeed = 0f;

        if (rb != null && !crashMode)
            rb.linearVelocity = Vector2.zero;
    }

    public void SetCrashMode(bool enabled)
    {
        crashMode = enabled;

        if (rb == null)
            return;

        rb.gravityScale = enabled ? crashGravityScale : baseGravityScale;
    }

    private void HandleKeyboardHorizontalInput()
    {
        keyboardHorizontalInput = 0f;

        if (Input.GetKey(KeyCode.A))
            keyboardHorizontalInput = -1f;
        else if (Input.GetKey(KeyCode.D))
            keyboardHorizontalInput = 1f;
    }

    private void HandleKeyboardVerticalInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
            keyboardVerticalSpeed += verticalSpeedStep;

        if (Input.GetKeyDown(KeyCode.DownArrow))
            keyboardVerticalSpeed -= verticalSpeedStep;

        keyboardVerticalSpeed = Mathf.Clamp(keyboardVerticalSpeed, -maxVerticalSpeed, maxVerticalSpeed);

        bool verticalKeyHeld = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow);
        if (!verticalKeyHeld)
            keyboardVerticalSpeed = Mathf.MoveTowards(keyboardVerticalSpeed, 0f, verticalDamping * Time.deltaTime);
    }

    private void UpdateCurrentInput()
    {
        if (inputSourceMode == InputSourceMode.KeyboardOnly)
        {
            currentHorizontalInput = keyboardHorizontalInput;
            currentVerticalSpeed = keyboardVerticalSpeed;
            return;
        }

        currentHorizontalInput = keyboardHorizontalInput != 0f ? keyboardHorizontalInput : externalHorizontalInput;
        currentVerticalSpeed = Mathf.Clamp(externalVerticalSpeed + keyboardVerticalSpeed, -maxVerticalSpeed, maxVerticalSpeed);
    }

    private void UpdateDirectionVisuals()
    {
        if (isTurning)
            return;

        int inputDirection = GetHorizontalInputDirection();
        if (inputDirection == 0 || inputDirection == facingDirection)
        {
            oppositeInputTime = 0f;
            return;
        }

        oppositeInputTime += Time.deltaTime;
        if (oppositeInputTime >= turnInputHoldTime)
            StartTurn(inputDirection);
    }

    private int GetHorizontalInputDirection()
    {
        if (currentHorizontalInput < 0f)
            return -1;

        if (currentHorizontalInput > 0f)
            return 1;

        return 0;
    }

    private void StartTurn(int direction)
    {
        if (turnRoutine != null)
            StopCoroutine(turnRoutine);

        turnRoutine = StartCoroutine(TurnToDirection(direction));
    }

    private IEnumerator TurnToDirection(int direction)
    {
        isTurning = true;
        oppositeInputTime = 0f;
        SetTurningVisual();

        if (turnFrameTime > 0f)
            yield return new WaitForSeconds(turnFrameTime);

        facingDirection = direction;
        SetDirectionVisual(facingDirection);
        isTurning = false;
        turnRoutine = null;
    }

    private void SetTurningVisual()
    {
        SetActiveIfAssigned(facingRightObject, false);
        SetActiveIfAssigned(facingLeftObject, false);
        SetActiveIfAssigned(turningObject, true);
        SetDirectionColliders(0);
        OnDirectionVisualChanged?.Invoke(0);
    }

    private void SetDirectionVisual(int direction)
    {
        SetActiveIfAssigned(facingRightObject, direction > 0);
        SetActiveIfAssigned(facingLeftObject, direction < 0);
        SetActiveIfAssigned(turningObject, false);
        SetDirectionColliders(direction);
        OnDirectionVisualChanged?.Invoke(direction);
    }

    private void SetDirectionColliders(int direction)
    {
        SetEnabledIfAssigned(facingRightCollider, direction > 0);
        SetEnabledIfAssigned(facingLeftCollider, direction < 0);
        SetEnabledIfAssigned(turningCollider, direction == 0);
    }

    private void SetActiveIfAssigned(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    private void SetEnabledIfAssigned(Collider2D target, bool enabled)
    {
        if (target != null)
            target.enabled = enabled;
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

    private float ApplyDeadzoneAndClamp(float value)
    {
        float clampedValue = Mathf.Clamp(value, -1f, 1f);

        if (Mathf.Abs(clampedValue) < externalInputDeadzone)
            return 0f;

        return clampedValue;
    }

    private void UpdateVisualTilt()
    {
        float tiltDirection = invertVisualTilt ? -1f : 1f;
        float targetTiltAngle = currentHorizontalInput * maxTiltAngle * tiltDirection;
        float currentZ = transform.eulerAngles.z;

        if (currentZ > 180f)
            currentZ -= 360f;

        float newZ = Mathf.LerpAngle(currentZ, targetTiltAngle, tiltSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newZ);
    }
}
