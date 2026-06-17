using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
#if ARDUINO_SERIAL_PORTS && (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
using System.Runtime.InteropServices;
using System.Text;
#endif
using UnityEngine;

public class ArduinoActionMapper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HelicopterController helicopterController;
    [SerializeField] private GameManager gameManager;

    [Header("Serial")]
    [SerializeField] private string portName = "COM6";
    [SerializeField] private int baudRate = 9600;
    [SerializeField, Min(0f)] private float arduinoResetDelay = 2f;
    [SerializeField, Min(1)] private int maxLinesPerFrame = 4;
    [SerializeField] private bool logSerialStatus = true;
    [SerializeField] private bool logReceivedLines;
    [SerializeField] private bool logSentServoCommands = true;

    [Header("Input Mapping")]
    [SerializeField] private bool joystickUsesMappedRange = true;
    [SerializeField] private int joystickCenterValue = 512;
    [SerializeField] private int joystickMaxOffset = 512;
    [SerializeField, Range(0f, 1f)] private float joystickDigitalThreshold = 0.25f;
    [SerializeField] private float joystickMappedRange = 100f;
    [SerializeField] private float potentiometerRange = 100f;
    [SerializeField] private bool invertJoystick;
    [SerializeField] private bool invertPotentiometer;

    [Header("Servo Output")]
    [SerializeField] private float fullFuelAngle = 0f;
    [SerializeField] private float emptyFuelAngle = 180f;
    [SerializeField, Min(0.1f)] private float servoUpdateInterval = 1f;
    [SerializeField] private bool sendServoOnlyWhenAngleChanges = false;
    [SerializeField] private bool testServoSweep;
    [SerializeField, Min(0.1f)] private float testServoSweepDuration = 2f;

    public event Action<int, float> OnArduinoRawInput;
    public event Action<float, float> OnArduinoMappedInput;
    public event Action<int> OnFuelServoAngleChanged;
    public event Action<string> OnSerialLineReceived;
    public event Action<string> OnSerialStatusChanged;

#if ARDUINO_SERIAL_PORTS && (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
    private WindowsSerialConnection serialConnection;
#endif
    private bool serialReady;
    private Coroutine serialReadyRoutine;
    private float nextServoUpdateTime;
    private int lastSentServoAngle = -1;

    private void Awake()
    {
        if (helicopterController == null)
            helicopterController = FindFirstObjectByType<HelicopterController>();

        if (gameManager == null)
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
    }

    private void OnEnable()
    {
        if (helicopterController != null)
            helicopterController.SetInputSourceMode(HelicopterController.InputSourceMode.ExternalOnly);

        if (gameManager != null)
            gameManager.OnFuelChanged += HandleFuelChanged;

        OpenSerialPort();
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.OnFuelChanged -= HandleFuelChanged;

        if (serialReadyRoutine != null)
        {
            StopCoroutine(serialReadyRoutine);
            serialReadyRoutine = null;
        }

        serialReady = false;
        CloseSerialPort();
    }

    private void Update()
    {
        ReadArduinoInput();

        if (testServoSweep)
            SendTestServoSweep();
        else
            SendCurrentFuelToServo();
    }

    private void ReadArduinoInput()
    {
#if ARDUINO_SERIAL_PORTS && (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        if (!serialReady || serialConnection == null || !serialConnection.IsOpen || helicopterController == null)
            return;

        try
        {
            string latestLine = null;
            for (int i = 0; i < maxLinesPerFrame; i++)
            {
                string line = serialConnection.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    break;

                latestLine = line;
            }

            if (!string.IsNullOrWhiteSpace(latestLine))
                HandleArduinoLine(latestLine);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"ArduinoActionMapper: Input read failed: {exception.Message}");
        }
#endif
    }

    private void HandleArduinoLine(string line)
    {
        if (logReceivedLines)
            Debug.Log($"ArduinoActionMapper: RX {line}");

        OnSerialLineReceived?.Invoke(line);

        if (line.StartsWith("S:", StringComparison.Ordinal))
            return;

        string[] values = line.Split(',');
        if (values.Length < 2)
        {
            Debug.LogWarning($"ArduinoActionMapper: Expected joystick,potentiometer but received '{line}'.");
            return;
        }

        if (!int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int joystickRaw))
        {
            Debug.LogWarning($"ArduinoActionMapper: Could not parse joystick value from '{line}'.");
            return;
        }

        if (!float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float potentiometerValue))
        {
            Debug.LogWarning($"ArduinoActionMapper: Could not parse potentiometer value from '{line}'.");
            return;
        }

        float horizontalInput = MapJoystickToHorizontal(joystickRaw);
        float verticalSpeed = MapPotentiometerToVerticalSpeed(potentiometerValue);

        OnArduinoRawInput?.Invoke(joystickRaw, potentiometerValue);
        OnArduinoMappedInput?.Invoke(horizontalInput, verticalSpeed);

        helicopterController.SetExternalInput(horizontalInput, verticalSpeed);
    }

    private float MapJoystickToHorizontal(int joystickRaw)
    {
        float value;

        if (joystickUsesMappedRange)
        {
            float safeRange = Mathf.Max(1f, joystickMappedRange);
            value = Mathf.Clamp(joystickRaw / safeRange, -1f, 1f);
        }
        else
        {
            int safeMaxOffset = Mathf.Max(1, joystickMaxOffset);
            value = Mathf.Clamp((joystickRaw - joystickCenterValue) / (float)safeMaxOffset, -1f, 1f);
        }

        if (invertJoystick)
            value *= -1f;

        if (value <= -joystickDigitalThreshold)
            return -1f;

        if (value >= joystickDigitalThreshold)
            return 1f;

        return 0f;
    }

    private float MapPotentiometerToVerticalSpeed(float potentiometerValue)
    {
        float safeRange = Mathf.Max(1f, potentiometerRange);
        float value = Mathf.Clamp(potentiometerValue / safeRange, -1f, 1f);
        if (invertPotentiometer)
            value *= -1f;

        return helicopterController != null ? value * helicopterController.maxVerticalSpeed : 0f;
    }

    private void HandleFuelChanged(float current, float max)
    {
        SendFuelToServo(current, max);
    }

    private void SendCurrentFuelToServo()
    {
        if (gameManager == null || Time.unscaledTime < nextServoUpdateTime)
            return;

        SendFuelToServo(gameManager.CurrentFuel, gameManager.MaxFuel);
    }

    private void SendTestServoSweep()
    {
        if (Time.unscaledTime < nextServoUpdateTime)
            return;

        float t = Mathf.PingPong(Time.unscaledTime / testServoSweepDuration, 1f);
        int servoAngle = Mathf.RoundToInt(Mathf.Lerp(emptyFuelAngle, fullFuelAngle, t));
        SendServoAngle(servoAngle, $"testSweep={t:0.00}");
    }

    private void SendFuelToServo(float current, float max, bool force = false)
    {
#if ARDUINO_SERIAL_PORTS && (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        if (!serialReady || serialConnection == null || !serialConnection.IsOpen)
            return;

        if (!force && Time.unscaledTime < nextServoUpdateTime)
            return;

        float fuelPercent = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        int servoAngle = Mathf.RoundToInt(Mathf.Lerp(emptyFuelAngle, fullFuelAngle, fuelPercent));
        SendServoAngle(servoAngle, $"fuel={current:0.##}/{max:0.##} ({fuelPercent:P0})");
#endif
    }

    private void SendServoAngle(int servoAngle, string debugContext)
    {
#if ARDUINO_SERIAL_PORTS && (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        if (!serialReady || serialConnection == null || !serialConnection.IsOpen)
            return;

        if (sendServoOnlyWhenAngleChanges && servoAngle == lastSentServoAngle)
            return;

        OnFuelServoAngleChanged?.Invoke(servoAngle);
        // Arduino expects fuel servo commands as F:angle, for example F:90.
        serialConnection.WriteLine($"F:{servoAngle}");
        lastSentServoAngle = servoAngle;

        if (logSentServoCommands)
            Debug.Log($"ArduinoActionMapper: TX F:{servoAngle} {debugContext}");

        nextServoUpdateTime = Time.unscaledTime + servoUpdateInterval;
#endif
    }

    private void OpenSerialPort()
    {
#if ARDUINO_SERIAL_PORTS && (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        if (serialConnection != null && serialConnection.IsOpen)
            return;

        try
        {
            serialConnection = new WindowsSerialConnection(portName, baudRate);
            serialConnection.Open();
            LogSerialStatus($"Opened {portName} at {baudRate} baud.");
            serialReadyRoutine = StartCoroutine(WaitForArduinoReset());
        }
        catch (Exception exception)
        {
            LogSerialStatus($"Could not open {portName}: {exception.Message}", true);
        }
#elif ARDUINO_SERIAL_PORTS
        Debug.LogWarning("ArduinoActionMapper: Serial is only implemented for Windows in this prototype.");
#else
        Debug.LogWarning("ArduinoActionMapper: Serial is disabled. Add ARDUINO_SERIAL_PORTS to Scripting Define Symbols.");
#endif
    }

    private void CloseSerialPort()
    {
#if ARDUINO_SERIAL_PORTS && (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        if (serialConnection == null)
            return;

        serialConnection.Close();
        serialConnection = null;
#endif
    }

    private IEnumerator WaitForArduinoReset()
    {
        serialReady = false;

        if (arduinoResetDelay > 0f)
            yield return new WaitForSecondsRealtime(arduinoResetDelay);

        serialReady = true;
        LogSerialStatus("Serial ready.");

        if (gameManager != null)
            SendFuelToServo(gameManager.CurrentFuel, gameManager.MaxFuel, true);

        serialReadyRoutine = null;
    }

    private void LogSerialStatus(string message, bool warning = false)
    {
        OnSerialStatusChanged?.Invoke(message);

        if (!logSerialStatus)
            return;

        if (warning)
            Debug.LogWarning($"ArduinoActionMapper: {message}");
        else
            Debug.Log($"ArduinoActionMapper: {message}");
    }
}

#if ARDUINO_SERIAL_PORTS && (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
public sealed class WindowsSerialConnection
{
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint OpenExisting = 3;
    private const int SetDtr = 5;
    private const int PurgeRxClear = 0x0008;
    private const int PurgeTxClear = 0x0004;
    private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

    private readonly string portName;
    private readonly int baudRate;
    private readonly StringBuilder lineBuffer = new StringBuilder();
    private readonly Queue<string> completedLines = new Queue<string>();
    private IntPtr handle = InvalidHandleValue;

    public bool IsOpen => handle != InvalidHandleValue;

    public WindowsSerialConnection(string portName, int baudRate)
    {
        this.portName = portName;
        this.baudRate = baudRate;
    }

    public void Open()
    {
        string deviceName = portName.StartsWith(@"\\.\", StringComparison.Ordinal) ? portName : @"\\.\" + portName;
        handle = CreateFile(deviceName, GenericRead | GenericWrite, 0, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);

        if (handle == InvalidHandleValue)
            throw new InvalidOperationException($"Win32 error {Marshal.GetLastWin32Error()}. Close Arduino Serial Monitor and check the port name.");

        Dcb dcb = new Dcb { DCBlength = (uint)Marshal.SizeOf<Dcb>() };
        if (!BuildCommDCB($"baud={baudRate} parity=N data=8 stop=1", ref dcb))
            throw new InvalidOperationException($"Could not build serial config for {portName}. Win32 error {Marshal.GetLastWin32Error()}.");

        if (!SetCommState(handle, ref dcb))
            throw new InvalidOperationException($"Could not apply serial config for {portName}. Win32 error {Marshal.GetLastWin32Error()}.");

        CommTimeouts timeouts = new CommTimeouts
        {
            ReadIntervalTimeout = 1,
            ReadTotalTimeoutConstant = 1,
            ReadTotalTimeoutMultiplier = 1,
            WriteTotalTimeoutConstant = 50,
            WriteTotalTimeoutMultiplier = 1
        };

        if (!SetCommTimeouts(handle, ref timeouts))
            throw new InvalidOperationException($"Could not set serial timeouts for {portName}. Win32 error {Marshal.GetLastWin32Error()}.");

        EscapeCommFunction(handle, SetDtr);
        PurgeComm(handle, PurgeRxClear | PurgeTxClear);
    }

    public string ReadLine()
    {
        if (!IsOpen)
            return null;

        if (completedLines.Count > 0)
            return completedLines.Dequeue();

        byte[] buffer = new byte[64];
        if (!ReadFile(handle, buffer, buffer.Length, out uint bytesRead, IntPtr.Zero) || bytesRead == 0)
            return null;

        string chunk = Encoding.ASCII.GetString(buffer, 0, (int)bytesRead);
        foreach (char character in chunk)
        {
            if (character == '\n')
            {
                string line = lineBuffer.ToString().Trim();
                lineBuffer.Length = 0;

                if (!string.IsNullOrWhiteSpace(line))
                    completedLines.Enqueue(line);

                continue;
            }

            if (character != '\r')
                lineBuffer.Append(character);
        }

        return completedLines.Count > 0 ? completedLines.Dequeue() : null;
    }

    public void WriteLine(string line)
    {
        if (!IsOpen)
            return;

        byte[] bytes = Encoding.ASCII.GetBytes(line + "\n");
        WriteFile(handle, bytes, bytes.Length, out _, IntPtr.Zero);
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        CloseHandle(handle);
        handle = InvalidHandleValue;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadFile(IntPtr file, byte[] buffer, int bytesToRead, out uint bytesRead, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteFile(IntPtr file, byte[] buffer, int bytesToWrite, out uint bytesWritten, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool BuildCommDCB(string definition, ref Dcb dcb);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetCommState(IntPtr file, ref Dcb dcb);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetCommTimeouts(IntPtr file, ref CommTimeouts timeouts);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool EscapeCommFunction(IntPtr file, int function);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool PurgeComm(IntPtr file, int flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct Dcb
    {
        public uint DCBlength;
        public uint BaudRate;
        public uint Flags;
        public ushort wReserved;
        public ushort XonLim;
        public ushort XoffLim;
        public byte ByteSize;
        public byte Parity;
        public byte StopBits;
        public sbyte XonChar;
        public sbyte XoffChar;
        public sbyte ErrorChar;
        public sbyte EofChar;
        public sbyte EvtChar;
        public ushort wReserved1;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CommTimeouts
    {
        public uint ReadIntervalTimeout;
        public uint ReadTotalTimeoutMultiplier;
        public uint ReadTotalTimeoutConstant;
        public uint WriteTotalTimeoutMultiplier;
        public uint WriteTotalTimeoutConstant;
    }
}
#endif
