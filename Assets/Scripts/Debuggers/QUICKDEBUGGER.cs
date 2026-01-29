using UnityEngine;
using UnityEngine.EventSystems;

public class QuickDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            TestAll();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            TestPins();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            TestConnection();
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            TestArduinoLED();
        }
    }

    void TestAll()
    {
        Debug.Log("=== QUICK TEST ===");

        // Проверка ConnectionManager
        if (ConnectionManager.Instance == null)
            Debug.LogError("❌ ConnectionManager missing!");
        else
        {
            Debug.Log("✅ ConnectionManager OK");
            ConnectionManager.Instance.DebugState();
        }

        // Проверка EventSystem
        if (FindObjectOfType<EventSystem>() == null)
            Debug.LogError("❌ EventSystem missing!");
        else
            Debug.Log("✅ EventSystem OK");

        // Проверка компонентов
        CircuitComponent[] comps = FindObjectsOfType<CircuitComponent>();
        Debug.Log($"Found {comps.Length} CircuitComponents");

        foreach (var comp in comps)
        {
            Debug.Log($"- {comp.GetName()} (Type: {comp.GetType().Name})");

            // Проверяем пины у компонента
            PinHighlighter[] pins = comp.GetComponentsInChildren<PinHighlighter>();
            Debug.Log($"  Has {pins.Length} pins");
        }
    }

    void TestPins()
    {
        Debug.Log("=== PIN TEST ===");

        PinHighlighter[] pins = FindObjectsOfType<PinHighlighter>();
        Debug.Log($"Found {pins.Length} pins");

        foreach (var pin in pins)
        {
            string parentName = pin.parentComponent?.GetName() ?? "NO PARENT";
            string parentType = pin.parentComponent?.GetType().Name ?? "null";
            Debug.Log($"Pin {pin.pinNumber}: Parent={parentName} ({parentType}), Connected={pin.isConnected}");
        }
    }

    void TestConnection()
    {
        Debug.Log("=== CONNECTION TEST ===");

        // Находим Arduino и LED
        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        VirtualLED led = FindObjectOfType<VirtualLED>();

        if (arduino == null)
        {
            Debug.LogError("❌ Arduino not found!");
            return;
        }

        if (led == null)
        {
            Debug.LogError("❌ LED not found!");
            return;
        }

        Debug.Log($"Arduino: {arduino.GetName()}");
        Debug.Log($"LED: {led.GetName()}");

        // Находим пины
        PinHighlighter[] allPins = FindObjectsOfType<PinHighlighter>();
        PinHighlighter arduinoPin = null;
        PinHighlighter ledPin = null;

        foreach (var pin in allPins)
        {
            if (pin.parentComponent == arduino && pin.pinNumber == 13)
                arduinoPin = pin;
            if (pin.parentComponent == led && pin.pinNumber == 1)
                ledPin = pin;
        }

        if (arduinoPin != null && ledPin != null)
        {
            Debug.Log("✅ Found pins 13 (Arduino) and 1 (LED)");

            // Тестовое соединение через ConnectionManager
            if (ConnectionManager.Instance != null)
            {
                ConnectionManager.Instance.StartConnection(arduino, 13);
                ConnectionManager.Instance.CompleteConnection(led, 1);
            }
            else
            {
                Debug.LogError("❌ ConnectionManager is null!");
            }
        }
        else
        {
            Debug.LogError("❌ Cannot find pins! Check pin numbers and parent assignments.");
        }
    }

    void TestArduinoLED()
    {
        Debug.Log("=== ARDUINO-LED TEST ===");

        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        VirtualLED led = FindObjectOfType<VirtualLED>();

        if (arduino == null || led == null)
        {
            Debug.LogError("❌ Need both Arduino and LED in scene!");
            return;
        }

        // Подключаем LED к пину 13
        led.ConnectToPins(13, 1); // pin 13 для положительного, pin 1 для отрицательного

        // Настраиваем пин как выход
        arduino.SetPinMode(13, PinMode.Output);

        // Включаем LED
        arduino.DigitalWrite(13, 1);
        Debug.Log("✅ LED should be ON (connected to pin 13)");

        // Выключаем через 2 секунды
        Invoke("TurnOffLED", 2f);
    }

    void TurnOffLED()
    {
        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        if (arduino != null)
        {
            arduino.DigitalWrite(13, 0);
            Debug.Log("✅ LED should be OFF");
        }
    }
}