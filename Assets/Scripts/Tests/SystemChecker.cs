using UnityEngine;

public class SystemChecker : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            CheckSystem();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            TestPinConnection();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            TestDragging();
        }
    }

    void CheckSystem()
    {
        Debug.Log("=== SYSTEM CHECK ===");

        // Проверка ConnectionManager
        if (SuperSimpleConnectionManager.Instance == null)
        {
            Debug.LogError("❌ ConnectionManager.Instance is NULL");
            // Пытаемся найти
            SuperSimpleConnectionManager cm = FindObjectOfType<SuperSimpleConnectionManager>();
            if (cm != null)
                Debug.Log("⚠️ ConnectionManager exists but Instance not set");
        }
        else
        {
            Debug.Log("✅ ConnectionManager.Instance exists");
        }

        // Проверка компонентов
        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        VirtualLED led = FindObjectOfType<VirtualLED>();

        Debug.Log($"Arduino: {(arduino != null ? "✅" : "❌")}");
        Debug.Log($"LED: {(led != null ? "✅" : "❌")}");

        // Проверка пинов
        PinHighlighter[] pins = FindObjectsOfType<PinHighlighter>();
        Debug.Log($"Total pins: {pins.Length}");

        foreach (var pin in pins)
        {
            Debug.Log($"  Pin {pin.pinNumber}: parent={pin.parentComponent != null}, collider={pin.GetComponent<Collider>() != null}");
        }

        // Проверка ComponentDragger
        ComponentDragger[] draggers = FindObjectsOfType<ComponentDragger>();
        Debug.Log($"ComponentDraggers: {draggers.Length}");

        Debug.Log("=== CHECK COMPLETE ===");
    }

    void TestPinConnection()
    {
        Debug.Log("=== TEST PIN CONNECTION ===");

        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        VirtualLED led = FindObjectOfType<VirtualLED>();

        if (arduino == null || led == null)
        {
            Debug.LogError("Need both Arduino and LED in scene!");
            return;
        }

        // Находим пины
        PinHighlighter arduinoPin = null;
        PinHighlighter ledPin = null;

        PinHighlighter[] allPins = FindObjectsOfType<PinHighlighter>();
        foreach (var pin in allPins)
        {
            if (pin.transform.IsChildOf(arduino.transform) && pin.pinNumber == 13)
                arduinoPin = pin;
            if (pin.transform.IsChildOf(led.transform) && pin.pinNumber == 1)
                ledPin = pin;
        }

        if (arduinoPin == null || ledPin == null)
        {
            Debug.LogError($"Cannot find pins! Arduino: {arduinoPin != null}, LED: {ledPin != null}");
            return;
        }

        Debug.Log($"Found pins: Arduino pin {arduinoPin.pinNumber}, LED pin {ledPin.pinNumber}");

        // Проверяем parentComponent
        Debug.Log($"Arduino pin parent: {arduinoPin.parentComponent != null}");
        Debug.Log($"LED pin parent: {ledPin.parentComponent != null}");

        // Пытаемся соединить
        if (SuperSimpleConnectionManager.Instance != null)
        {
            Debug.Log("Attempting connection...");
            SuperSimpleConnectionManager.Instance.StartConnection(arduinoPin.parentComponent, arduinoPin.pinNumber);
            SuperSimpleConnectionManager.Instance.CompleteConnection(ledPin.parentComponent, ledPin.pinNumber);
        }
    }

    void TestDragging()
    {
        Debug.Log("=== TEST DRAGGING ===");

        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        if (arduino != null)
        {
            ComponentDragger dragger = arduino.GetComponent<ComponentDragger>();
            if (dragger != null)
            {
                Debug.Log($"Arduino has ComponentDragger: {dragger.isDragging}");
            }
            else
            {
                Debug.Log("Arduino doesn't have ComponentDragger!");
            }
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;

        int y = 10;
        GUI.Label(new Rect(10, y, 400, 30), "F1 - Check System", style); y += 30;
        GUI.Label(new Rect(10, y, 400, 30), "F2 - Test Pin Connection", style); y += 30;
        GUI.Label(new Rect(10, y, 400, 30), "F3 - Test Dragging", style); y += 30;

        if (SuperSimpleConnectionManager.Instance != null)
        {
            string status = SuperSimpleConnectionManager.Instance.isConnecting ?
                $"CONNECTING (pin {SuperSimpleConnectionManager.Instance.GetConnectingPin()})" : "IDLE";
            GUI.Label(new Rect(10, y, 400, 30), $"ConnectionManager: {status}", style);
        }
    }
}