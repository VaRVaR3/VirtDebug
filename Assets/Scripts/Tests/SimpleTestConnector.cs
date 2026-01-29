using UnityEngine;

public class SimpleTestConnector : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TestConnection();
        }
    }

    void TestConnection()
    {
        Debug.Log("=== SIMPLE CONNECTION TEST ===");

        // Найти Arduino
        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        if (arduino == null)
        {
            Debug.LogError("Arduino not found!");
            return;
        }

        // Найти LED
        VirtualLED led = FindObjectOfType<VirtualLED>();
        if (led == null)
        {
            Debug.LogError("LED not found!");
            return;
        }

        Debug.Log($"Found Arduino: {arduino.name}");
        Debug.Log($"Found LED: {led.name}");

        // Подключить LED к пину 13
        led.ConnectToPins(13, -1);
        arduino.SetPinMode(13, PinMode.Output);

        Debug.Log("Connected LED to pin 13");

        // Включить LED
        arduino.DigitalWrite(13, 1);
        Debug.Log("Turned LED ON");

        // Выключить через 2 секунды
        Invoke("TurnOffLED", 2f);
    }

    void TurnOffLED()
    {
        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        if (arduino != null)
        {
            arduino.DigitalWrite(13, 0);
            Debug.Log("Turned LED OFF");
        }
    }
}