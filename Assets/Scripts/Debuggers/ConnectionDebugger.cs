using UnityEngine;

public class ConnectionDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DebugAllConnections();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            DebugLEDState();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            DebugArduinoState();
        }
    }

    void DebugAllConnections()
    {
        Debug.Log("=== ALL CONNECTIONS DEBUG ===");

        // 1. ConnectionManager
        if (ConnectionManager.Instance == null)
        {
            Debug.LogError("ConnectionManager is NULL!");
        }
        else
        {
            ConnectionManager.Instance.DebugState();
        }

        // 2. Все CircuitComponents
        CircuitComponent[] allComps = FindObjectsOfType<CircuitComponent>();
        Debug.Log($"Found {allComps.Length} CircuitComponents");

        foreach (var comp in allComps)
        {
            Debug.Log($"- {comp.GetName()}: PositivePin={comp.positivePin}, Arduino={comp.connectedArduino?.GetName() ?? "null"}");

            // Проверяем пины компонента
            PinHighlighter[] pins = comp.GetComponentsInChildren<PinHighlighter>();
            foreach (var pin in pins)
            {
                Debug.Log($"  Pin {pin.pinNumber}: Connected={pin.isConnected}, Parent={pin.parentComponent?.GetName() ?? "null"}");
            }
        }

        // 3. Arduino
        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        if (arduino != null)
        {
            Debug.Log($"Arduino: {arduino.GetName()}");
            for (int i = 0; i < arduino.digitalPins.Count; i++)
            {
                var pin = arduino.digitalPins[i];
                if (pin.isConnected)
                {
                    Debug.Log($"  Pin {i}: Connected={pin.isConnected}, Mode={pin.mode}, Voltage={pin.voltage}");
                }
            }
        }
    }

    void DebugLEDState()
    {
        VirtualLED led = FindObjectOfType<VirtualLED>();
        if (led == null)
        {
            Debug.LogError("No LED found in scene!");
            return;
        }

        Debug.Log("=== LED STATE ===");
        Debug.Log($"Name: {led.GetName()}");
        Debug.Log($"Positive Pin: {led.positivePin}");
        Debug.Log($"Negative Pin: {led.negativePin}");
        Debug.Log($"Connected Arduino: {led.connectedArduino?.GetName() ?? "null"}");
        Debug.Log($"Is Active: {led.isActive}");
        Debug.Log($"Current Voltage: {led.currentVoltage}");

        // Проверяем физические соединения
        if (ConnectionManager.Instance != null)
        {
            bool foundConnection = false;
            foreach (var conn in ConnectionManager.Instance.GetConnections())
            {
                if (conn.sourceComponent == (IConnectable)led || conn.targetComponent == (IConnectable)led)
                {
                    Debug.Log($"Found wire connection: {conn.sourceComponent?.GetName()}:{conn.sourcePin} -> {conn.targetComponent?.GetName()}:{conn.targetPin}");
                    foundConnection = true;
                }
            }

            if (!foundConnection)
            {
                Debug.LogWarning("No wire connections found for LED!");
            }
        }
    }

    void DebugArduinoState()
    {
        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        if (arduino == null)
        {
            Debug.LogError("No Arduino found in scene!");
            return;
        }

        Debug.Log("=== ARDUINO STATE ===");
        Debug.Log($"Name: {arduino.GetName()}");

        for (int i = 0; i < arduino.digitalPins.Count; i++)
        {
            var pin = arduino.digitalPins[i];
            if (pin.isConnected || pin.mode == PinMode.Output)
            {
                Debug.Log($"Pin {i}: Mode={pin.mode}, Connected={pin.isConnected}, Voltage={pin.voltage}V");
            }
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.white;

        int y = 10;
        GUI.Label(new Rect(Screen.width - 300, y, 290, 25), "DEBUG CONTROLS", style); y += 25;
        GUI.Label(new Rect(Screen.width - 300, y, 290, 25), "F1 - Debug All Connections", style); y += 25;
        GUI.Label(new Rect(Screen.width - 300, y, 290, 25), "F2 - Debug LED State", style); y += 25;
        GUI.Label(new Rect(Screen.width - 300, y, 290, 25), "F3 - Debug Arduino State", style);
    }
}