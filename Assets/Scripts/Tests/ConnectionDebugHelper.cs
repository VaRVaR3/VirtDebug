using UnityEngine;

public class ConnectionDebugHelper : MonoBehaviour
{
    void Update()
    {
        // Тестирование системы соединений
        if (Input.GetKeyDown(KeyCode.F5))
        {
            TestConnectionSystem();
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            ForceConnectLED();
        }

        if (Input.GetKeyDown(KeyCode.F7))
        {
            ShowConnectionStatus();
        }
    }

    void TestConnectionSystem()
    {
        Debug.Log("=== CONNECTION SYSTEM TEST ===");

        // Проверяем основные компоненты
        if (ConnectionManager.Instance == null)
        {
            Debug.LogError("❌ ConnectionManager not found!");
            return;
        }
        else
        {
            Debug.Log("✅ ConnectionManager found");
        }

        // Проверяем, есть ли компоненты для соединения
        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        VirtualLED led = FindObjectOfType<VirtualLED>();

        if (arduino == null)
        {
            Debug.LogError("❌ Arduino not found!");
        }
        else
        {
            Debug.Log($"✅ Arduino found: {arduino.name}");
        }

        if (led == null)
        {
            Debug.LogError("❌ LED not found!");
        }
        else
        {
            Debug.Log($"✅ LED found: {led.name}");
        }

        // Проверяем пины
        if (arduino != null)
        {
            PinHighlighter[] arduinoPins = arduino.GetComponentsInChildren<PinHighlighter>();
            Debug.Log($"Arduino has {arduinoPins.Length} pins");
        }

        if (led != null)
        {
            PinHighlighter[] ledPins = led.GetComponentsInChildren<PinHighlighter>();
            Debug.Log($"LED has {ledPins.Length} pins");
        }

        Debug.Log("=== TEST COMPLETE ===");
    }

    void ForceConnectLED()
    {
        Debug.Log("=== FORCE CONNECT LED ===");

        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        VirtualLED led = FindObjectOfType<VirtualLED>();

        if (arduino == null || led == null)
        {
            Debug.LogError("❌ Need both Arduino and LED!");
            return;
        }

        // Принудительно начинаем соединение
        ConnectionManager.Instance.StartConnection(arduino, 13);
        Debug.Log("✅ Started connection from Arduino pin 13");

        // Принудительно завершаем соединение
        ConnectionManager.Instance.CompleteConnection(led, 1);
        Debug.Log("✅ Completed connection to LED pin 1");

        // Включаем LED
        arduino.SetPinMode(13, PinMode.Output);
        arduino.DigitalWrite(13, 1);
        Debug.Log("✅ Turned on LED");
    }

    void ShowConnectionStatus()
    {
        if (ConnectionManager.Instance == null)
        {
            Debug.Log("ConnectionManager is NULL");
            return;
        }

        Debug.Log($"=== CONNECTION STATUS ===");
        Debug.Log($"Is connecting: {ConnectionManager.Instance.isConnecting}");
        Debug.Log($"Active connections: {ConnectionManager.Instance.GetConnections().Count}");

        if (ConnectionManager.Instance.isConnecting)
        {
            var component = ConnectionManager.Instance.GetConnectingComponent();
            var pin = ConnectionManager.Instance.GetConnectingPin();
            Debug.Log($"Currently connecting: {component?.GetName()} pin {pin}");
        }

        // Показываем все соединения
        var connections = ConnectionManager.Instance.GetConnections();
        for (int i = 0; i < connections.Count; i++)
        {
            var conn = connections[i];
            Debug.Log($"  {i}: {conn.sourceComponent?.GetName()}:{conn.sourcePin} -> {conn.targetComponent?.GetName()}:{conn.targetPin}");
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.white;

        int y = 10;
        GUI.Label(new Rect(10, y, 300, 25), "F5 - Test Connection System", style); y += 25;
        GUI.Label(new Rect(10, y, 300, 25), "F6 - Force Connect LED", style); y += 25;
        GUI.Label(new Rect(10, y, 300, 25), "F7 - Show Connection Status", style); y += 25;

        if (ConnectionManager.Instance != null)
        {
            string status = ConnectionManager.Instance.isConnecting ? "CONNECTING" : "IDLE";
            GUI.Label(new Rect(10, y, 300, 25), $"Status: {status}", style); y += 25;

            if (ConnectionManager.Instance.isConnecting)
            {
                var component = ConnectionManager.Instance.GetConnectingComponent();
                var pin = ConnectionManager.Instance.GetConnectingPin();
                GUI.Label(new Rect(10, y, 300, 25),
                    $"From: {component?.GetName()} pin {pin}", style);
            }
        }
    }
}