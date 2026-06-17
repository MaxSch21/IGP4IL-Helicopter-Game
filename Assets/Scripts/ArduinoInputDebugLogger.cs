using UnityEngine;

public class ArduinoInputDebugLogger : MonoBehaviour
{
    [SerializeField] private ArduinoActionMapper actionMapper;
    [SerializeField] private HelicopterController helicopterController;
    [SerializeField, Min(0.05f)] private float logInterval = 0.25f;
    [SerializeField] private bool logRawInput = true;
    [SerializeField] private bool logMappedInput = true;
    [SerializeField] private bool logServoOutput = true;
    [SerializeField] private bool logSerialStatus = true;
    [SerializeField] private bool logReceivedLines;

    private int lastJoystickRaw;
    private float lastPotentiometerValue;
    private float lastHorizontalInput;
    private float lastVerticalSpeed;
    private int lastServoAngle;
    private bool hasRawInput;
    private bool hasMappedInput;
    private bool hasServoOutput;
    private float nextLogTime;

    private void Awake()
    {
        if (actionMapper == null)
            actionMapper = FindFirstObjectByType<ArduinoActionMapper>();

        if (helicopterController == null)
            helicopterController = FindFirstObjectByType<HelicopterController>();
    }

    private void OnEnable()
    {
        if (actionMapper == null)
            return;

        actionMapper.OnArduinoRawInput += HandleRawInput;
        actionMapper.OnArduinoMappedInput += HandleMappedInput;
        actionMapper.OnFuelServoAngleChanged += HandleServoAngleChanged;
        actionMapper.OnSerialStatusChanged += HandleSerialStatusChanged;
        actionMapper.OnSerialLineReceived += HandleSerialLineReceived;
    }

    private void OnDisable()
    {
        if (actionMapper == null)
            return;

        actionMapper.OnArduinoRawInput -= HandleRawInput;
        actionMapper.OnArduinoMappedInput -= HandleMappedInput;
        actionMapper.OnFuelServoAngleChanged -= HandleServoAngleChanged;
        actionMapper.OnSerialStatusChanged -= HandleSerialStatusChanged;
        actionMapper.OnSerialLineReceived -= HandleSerialLineReceived;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextLogTime)
            return;

        nextLogTime = Time.unscaledTime + logInterval;

        string message = "Arduino";

        if (logRawInput && hasRawInput)
            message += $" | raw joystick={lastJoystickRaw}, pot={lastPotentiometerValue:0.##}";

        if (logMappedInput && hasMappedInput)
            message += $" | mapped horizontal={lastHorizontalInput:0.00}, verticalSpeed={lastVerticalSpeed:0.00}";

        if (helicopterController != null)
            message += $" | heli horizontal={helicopterController.CurrentHorizontalInput:0.00}";

        if (logServoOutput && hasServoOutput)
            message += $" | fuelServo={lastServoAngle}deg";

        if (message != "Arduino")
            Debug.Log(message);
    }

    private void HandleRawInput(int joystickRaw, float potentiometerValue)
    {
        lastJoystickRaw = joystickRaw;
        lastPotentiometerValue = potentiometerValue;
        hasRawInput = true;
    }

    private void HandleMappedInput(float horizontalInput, float verticalSpeed)
    {
        lastHorizontalInput = horizontalInput;
        lastVerticalSpeed = verticalSpeed;
        hasMappedInput = true;
    }

    private void HandleServoAngleChanged(int servoAngle)
    {
        lastServoAngle = servoAngle;
        hasServoOutput = true;
    }

    private void HandleSerialStatusChanged(string status)
    {
        if (logSerialStatus)
            Debug.Log($"Arduino serial: {status}");
    }

    private void HandleSerialLineReceived(string line)
    {
        if (logReceivedLines)
            Debug.Log($"Arduino RX: {line}");
    }
}
