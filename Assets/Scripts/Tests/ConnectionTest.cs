using UnityEngine;

public class ConnectionTest : MonoBehaviour
{
    void Update()
    {
        // Тестовая команда по нажатию клавиши
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestManualConnection();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearAllConnections();
        }
    }

    void TestManualConnection()
    {
        Debug.Log("=== Manual Connection Test ===");

        // Находим все компоненты
        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        VirtualLED led = FindObjectOfType<VirtualLED>();

        if (arduino == null || led == null)
        {
            Debug.LogError("Arduino or LED not found!");
            return;
        }

        // Вручную создаем соединение
        if (ConnectionManager.Instance != null)
        {
            // Имитируем клик на пин 13 Arduino
            PinHighlighter arduinoPin = FindPin(arduino.gameObject, 13);
            PinHighlighter ledPin = FindPin(led.gameObject, 0); // LED пин 0 (положительный)

            if (arduinoPin != null && ledPin != null)
            {
                Debug.Log("Found pins. Starting manual connection...");

                // Начинаем соединение
                ConnectionManager.Instance.StartConnection(arduino.GetComponent<CircuitComponent>(), 13);

                // Даем время для создания временного провода
                Invoke("CompleteTestConnection", 0.1f);
            }
            else
            {
                Debug.LogError("Could not find pins!");
            }
        }
    }

    void CompleteTestConnection()
    {
        VirtualLED led = FindObjectOfType<VirtualLED>();
        if (led != null)
        {
            ConnectionManager.Instance.CompleteConnection(led.GetComponent<CircuitComponent>(), 0);
        }
    }

    PinHighlighter FindPin(GameObject parent, int pinNumber)
    {
        PinHighlighter[] pins = parent.GetComponentsInChildren<PinHighlighter>();
        foreach (PinHighlighter pin in pins)
        {
            if (pin.pinNumber == pinNumber)
            {
                Debug.Log($"Found pin {pinNumber} on {parent.name}");
                return pin;
            }
        }

        Debug.LogWarning($"Pin {pinNumber} not found on {parent.name}");
        return null;
    }

    void ClearAllConnections()
    {
        if (ConnectionManager.Instance != null)
        {
            ConnectionManager.Instance.DisconnectAll();
            Debug.Log("All connections cleared");
        }
    }
}