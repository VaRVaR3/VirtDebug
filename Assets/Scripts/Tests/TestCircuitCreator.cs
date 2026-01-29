using UnityEngine;

public class TestCircuitCreator : MonoBehaviour
{
    public VirtualArduino arduino;
    public VirtualLED led;
    public VirtualGround ground;
    public int testPin = 13;

    void Start()
    {
        Invoke("CreateTestCircuit", 1f);
    }

    void CreateTestCircuit()
    {
        if (arduino == null || led == null || ground == null)
        {
            Debug.LogError("Missing components for test circuit!");
            return;
        }

        Debug.Log("=== CREATING TEST CIRCUIT ===");

        // 1. Подключаем LED к Arduino
        led.ConnectToPins(testPin, 0);
        led.connectedArduino = arduino;

        // 2. Устанавливаем пин в OUTPUT
        arduino.SetPinMode(testPin, PinMode.Output);

        // 3. Создаем соединения через ConnectionManager
        if (ConnectionManager.Instance != null)
        {
            IConnectable arduinoConn = arduino as IConnectable;
            IConnectable ledConn = led as IConnectable;
            IConnectable groundConn = ground as IConnectable;

            // Arduino → LED (анод)
            ConnectionManager.Instance.StartConnection(arduinoConn, testPin);
            ConnectionManager.Instance.CompleteConnection(ledConn, 1); // пин 1 LED

            // LED → Ground (катод)
            ConnectionManager.Instance.StartConnection(ledConn, 2); // пин 2 LED
            ConnectionManager.Instance.CompleteConnection(groundConn, 0);
        }

        Debug.Log("Test circuit created. LED should work only when circuit is complete.");
    }
}