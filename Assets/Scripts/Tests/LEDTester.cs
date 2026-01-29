using UnityEngine;
using System.Collections;

public class LEDTester : MonoBehaviour
{
    [Header("Test Settings")]
    public int testPin = 13;
    public float testInterval = 2f;
    public bool autoTest = false;
    public bool forceConnection = true;

    private VirtualArduino arduino;
    private VirtualLED led;
    private PinHighlighter ledPin;
    private bool ledState = false;

    void Start()
    {
        Invoke("InitializeComponents", 1f);

        if (autoTest)
        {
            Invoke("StartLEDTest", 1.5f);
        }
    }

    void Update()
    {
        // Ручное управление (автотест отключен)
        if (Input.GetKeyDown(KeyCode.T))
        {
            DebugTest();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            TurnOnLED();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            TurnOffLED();
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            BlinkLED();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            CreateConnection();
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            DirectLEDTest();
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            TestUpdateWithCurrentFlow();
        }
    }

    void InitializeComponents()
    {
        arduino = FindObjectOfType<VirtualArduino>();
        led = FindObjectOfType<VirtualLED>();

        if (arduino == null)
        {
            Debug.LogError("Arduino not found!");
            return;
        }

        if (led == null)
        {
            Debug.LogError("LED not found!");
            return;
        }

        // Находим пины светодиода
        PinHighlighter[] pins = led.GetComponentsInChildren<PinHighlighter>();
        foreach (var pin in pins)
        {
            if (pin.pinMode == PinMode.Input || pin.pinMode == PinMode.Output)
            {
                ledPin = pin;
                Debug.Log($"Found LED pin: {pin.pinNumber}, Mode: {pin.pinMode}");
                break;
            }
        }

        Debug.Log($"Initialized: Arduino={arduino.name}, LED={led.name}, LED Pin={(ledPin != null ? ledPin.pinNumber.ToString() : "null")}");
    }

    void StartLEDTest()
    {
        Debug.Log("=== STARTING LED TEST ===");

        if (arduino == null || led == null)
        {
            Debug.LogError("Missing components!");
            return;
        }

        // Устанавливаем пин в OUTPUT режим
        arduino.SetPinMode(testPin, PinMode.Output);
        Debug.Log($"Pin {testPin} set to OUTPUT");

        // Если нужно, создаем соединение
        if (forceConnection && (led.positivePin == -1 || led.connectedArduino == null))
        {
            CreateConnection();
        }

        // Тестовое включение
        TurnOnLED();
        Invoke("TurnOffLED", 1f);
    }

    void CreateConnection()
    {
        if (arduino == null || led == null)
        {
            Debug.LogError("Cannot create connection - missing components!");
            return;
        }

        Debug.Log("Creating connection between Arduino and LED...");

        // Находим компоненты IConnectable
        IConnectable arduinoConnectable = arduino as IConnectable;
        IConnectable ledConnectable = led as IConnectable;

        // Используем правильное сравнение объектов
        if (arduinoConnectable == null || ledConnectable == null)
        {
            Debug.LogError("Components don't implement IConnectable!");
            return;
        }

        // Создаем соединение через ConnectionManager
        if (ConnectionManager.Instance != null)
        {
            // Если уже есть соединение, удаляем его
            var connections = ConnectionManager.Instance.GetConnections();
            foreach (var conn in connections)
            {
                // Используем правильное сравнение
                if (conn.sourceComponent == ledConnectable ||
                    conn.targetComponent == ledConnectable)
                {
                    // Разрываем старое соединение
                    ConnectionManager.Instance.DisconnectAll();
                    break;
                }
            }

            // Создаем новое соединение
            ConnectionManager.Instance.StartConnection(arduinoConnectable, testPin);
            ConnectionManager.Instance.CompleteConnection(ledConnectable, 1);

            Debug.Log($"Connection created: Arduino pin {testPin} -> LED pin 1");

            // Обновляем состояние LED
            led.positivePin = testPin;
            led.connectedArduino = arduino;
        }
        else
        {
            Debug.LogError("ConnectionManager not found!");
        }
    }

    void DebugTest()
    {
        Debug.Log("=== DEBUG TEST ===");

        if (arduino != null)
        {
            // Проверяем состояние пина
            var pin = arduino.digitalPins.Count > testPin ? arduino.digitalPins[testPin] : null;
            if (pin != null)
            {
                Debug.Log($"Pin {testPin}: Mode={pin.mode}, Voltage={pin.voltage}V, Connected={pin.isConnected}");
            }

            // Проверяем чтение/запись
            int readValue = arduino.DigitalRead(testPin);
            Debug.Log($"DigitalRead({testPin}) = {readValue}");
        }

        if (led != null)
        {
            Debug.Log($"LED: Intensity={led.intensity}, Voltage={led.currentVoltage}");
            Debug.Log($"LED Material Initialized: {led.IsMaterialInitialized()}");
        }
    }

    void TurnOnLED()
    {
        if (arduino == null) return;

        Debug.Log($"Turning ON LED on pin {testPin}...");

        // Убедимся, что пин в OUTPUT режиме
        arduino.SetPinMode(testPin, PinMode.Output);

        // Включаем пин
        bool success = arduino.DigitalWrite(testPin, 1);
        Debug.Log($"DigitalWrite result: {success}");

        if (success)
        {
            ledState = true;
            Debug.Log("LED should be ON now");

            // Прямой вызов для теста
            if (led != null)
            {
                led.OnVoltageChanged(5f);
                Debug.Log($"Direct call: LED intensity = {led.intensity}");
            }
        }
    }

    void TurnOffLED()
    {
        if (arduino == null) return;

        Debug.Log($"Turning OFF LED on pin {testPin}...");
        bool success = arduino.DigitalWrite(testPin, 0);

        if (success)
        {
            ledState = false;
            Debug.Log("LED should be OFF now");

            if (led != null)
            {
                led.OnVoltageChanged(0f);
            }
        }
    }

    void TestUpdateWithCurrentFlow()
    {
        if (led != null)
        {
            Debug.Log("Testing UpdateWithCurrentFlow...");
            // Тестируем новый метод с разными напряжениями
            led.UpdateWithCurrentFlow(5f, 0f); // 5V, земля 0V
            Invoke("TestCurrentFlowOff", 1f);
        }
    }

    void TestCurrentFlowOff()
    {
        if (led != null)
        {
            led.UpdateWithCurrentFlow(0f, 0f);
            Debug.Log("UpdateWithCurrentFlow test complete");
        }
    }

    void BlinkLED()
    {
        if (arduino == null) return;

        Debug.Log("Starting LED blink sequence...");
        StartCoroutine(BlinkCoroutine());
    }

    IEnumerator BlinkCoroutine()
    {
        for (int i = 0; i < 5; i++)
        {
            TurnOnLED();
            yield return new WaitForSeconds(0.5f);
            TurnOffLED();
            yield return new WaitForSeconds(0.5f);
        }
        Debug.Log("Blink sequence complete");
    }

    void DirectLEDTest()
    {
        Debug.Log("=== DIRECT LED TEST ===");

        if (led != null)
        {
            led.OnVoltageChanged(5f);
            Debug.Log($"LED direct ON - Intensity: {led.intensity}");

            Invoke("DirectLEDOff", 1f);
        }
    }

    void DirectLEDOff()
    {
        if (led != null)
        {
            led.OnVoltageChanged(0f);
            Debug.Log("LED direct OFF");
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 16;
        style.normal.textColor = Color.white;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 14;
        buttonStyle.normal.textColor = Color.white;

        int y = 10;
        int width = 300;
        int height = 30;
        int spacing = 5;

        // Панель управления
        GUI.Box(new Rect(10, y, width + 20, 320), "LED TESTER");
        y += 30;

        if (GUILayout.Button("Initialize Components", buttonStyle, GUILayout.Width(width), GUILayout.Height(height)))
        {
            InitializeComponents();
        }
        y += height + spacing;

        if (GUILayout.Button("Create Connection (C)", buttonStyle, GUILayout.Width(width), GUILayout.Height(height)))
        {
            CreateConnection();
        }
        y += height + spacing;

        if (GUILayout.Button("Turn ON (O)", buttonStyle, GUILayout.Width(width), GUILayout.Height(height)))
        {
            TurnOnLED();
        }
        y += height + spacing;

        if (GUILayout.Button("Turn OFF (P)", buttonStyle, GUILayout.Width(width), GUILayout.Height(height)))
        {
            TurnOffLED();
        }
        y += height + spacing;

        if (GUILayout.Button("Blink (B)", buttonStyle, GUILayout.Width(width), GUILayout.Height(height)))
        {
            BlinkLED();
        }
        y += height + spacing;

        if (GUILayout.Button("Debug Test (T)", buttonStyle, GUILayout.Width(width), GUILayout.Height(height)))
        {
            DebugTest();
        }
        y += height + spacing;

        if (GUILayout.Button("Direct Test (D)", buttonStyle, GUILayout.Width(width), GUILayout.Height(height)))
        {
            DirectLEDTest();
        }
        y += height + spacing;

        if (GUILayout.Button("Test Current Flow (U)", buttonStyle, GUILayout.Width(width), GUILayout.Height(height)))
        {
            TestUpdateWithCurrentFlow();
        }
        y += height + spacing;

        // Информация
        y += 10;
        GUI.Label(new Rect(10, y, width, height), $"Arduino: {(arduino != null ? arduino.name : "NOT FOUND")}", style);
        y += height;

        GUI.Label(new Rect(10, y, width, height), $"LED: {(led != null ? led.name : "NOT FOUND")}", style);
        y += height;

        if (led != null)
        {
            GUI.Label(new Rect(10, y, width, height), $"LED Intensity: {led.intensity:F2}", style);
            y += height;

            GUI.Label(new Rect(10, y, width, height), $"LED State: {(ledState ? "ON" : "OFF")}", style);
            y += height;

            string connectedTo = led.positivePin != -1 ?
                $"Pin {led.positivePin}" : "NOT CONNECTED";
            GUI.Label(new Rect(10, y, width, height), $"Connected to: {connectedTo}", style);
            y += height;

            GUI.Label(new Rect(10, y, width, height),
                $"Material: {(led.IsMaterialInitialized() ? "OK" : "NOT INITIALIZED")}", style);
        }
    }
}