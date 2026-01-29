using UnityEngine;
using System.Collections;

public class RobustLEDTester : MonoBehaviour
{
    [Header("LED Reference")]
    public VirtualLED targetLED;

    [Header("Control Settings")]
    public int arduinoPin = 13;
    public bool useArduino = true;
    public float manualVoltage = 5f;

    private VirtualArduino arduino;
    private bool isOn = false;

    void Start()
    {
        // Находим компоненты
        if (targetLED == null)
            targetLED = FindObjectOfType<VirtualLED>();

        if (useArduino)
            arduino = FindObjectOfType<VirtualArduino>();

        // Принудительно активируем LED
        if (targetLED != null)
        {
            targetLED.isActive = true;
            Debug.Log($"LED {targetLED.name} forced to active state");
        }
    }

    void Update()
    {
        // Простое управление
        if (Input.GetKeyDown(KeyCode.O))
        {
            TurnOnLED();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            TurnOffLED();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleLED();
        }
    }

    void TurnOnLED()
    {
        if (targetLED == null) return;

        Debug.Log("Turning LED ON (forced)");

        // Принудительно активируем
        targetLED.isActive = true;

        if (useArduino && arduino != null)
        {
            // Устанавливаем пин в OUTPUT
            arduino.SetPinMode(arduinoPin, PinMode.Output);
            // Включаем
            arduino.DigitalWrite(arduinoPin, 1);
        }
        else
        {
            // Прямое управление
            targetLED.OnVoltageChanged(manualVoltage);
        }

        isOn = true;
    }

    void TurnOffLED()
    {
        if (targetLED == null) return;

        Debug.Log("Turning LED OFF");

        if (useArduino && arduino != null)
        {
            arduino.DigitalWrite(arduinoPin, 0);
        }
        else
        {
            targetLED.OnVoltageChanged(0f);
        }

        isOn = false;
    }

    void ToggleLED()
    {
        if (isOn)
            TurnOffLED();
        else
            TurnOnLED();
    }

    // Принудительный сброс состояния LED
    public void ResetLED()
    {
        if (targetLED != null)
        {
            targetLED.isActive = true;
            targetLED.OnVoltageChanged(0f);
            isOn = false;
            Debug.Log("LED reset to default state");
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(10, 10, 300, 30), "Robust LED Tester", style);
        GUI.Label(new Rect(10, 40, 300, 30), "O - Turn ON", style);
        GUI.Label(new Rect(10, 70, 300, 30), "P - Turn OFF", style);
        GUI.Label(new Rect(10, 100, 300, 30), "SPACE - Toggle", style);

        if (targetLED != null)
        {
            string status = isOn ? "ON" : "OFF";
            Color statusColor = isOn ? Color.green : Color.red;

            GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
            statusStyle.fontSize = 24;
            statusStyle.normal.textColor = statusColor;

            GUI.Label(new Rect(10, 130, 300, 30), $"Status: {status}", statusStyle);
            GUI.Label(new Rect(10, 160, 300, 30),
                $"Intensity: {targetLED.intensity:F2}", style);
            GUI.Label(new Rect(10, 190, 300, 30),
                $"Active: {targetLED.isActive}", style);
        }
    }
}