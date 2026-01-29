using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircuitManager : MonoBehaviour
{
    public static CircuitManager Instance;

    [Header("Circuit State")]
    public bool circuitPowered = false;
    public float circuitVoltage = 5f;
    public List<CircuitComponent> allComponents = new List<CircuitComponent>();

    [Header("Visual Feedback")]
    public Color poweredColor = Color.green;
    public Color unpoweredColor = Color.gray;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Поиск всех компонентов в сцене
        if (Time.frameCount % 60 == 0)
        {
            UpdateComponentList();
        }

        // Проверка целостности схемы
        if (circuitPowered)
        {
            UpdateCircuitState();
        }

        // Тестовое управление для отладки
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TestLED();
        }
    }

    void TestLED()
    {
        Debug.Log("=== LED TEST ===");

        // Найти все светодиоды
        VirtualLED[] leds = FindObjectsOfType<VirtualLED>();
        Debug.Log($"Found {leds.Length} LEDs");

        foreach (VirtualLED led in leds)
        {
            Debug.Log($"LED: {led.name}, Active: {led.isActive}");

            // Тест: мигнуть светодиодом
            StartCoroutine(BlinkLED(led));
        }
    }

    IEnumerator BlinkLED(VirtualLED led)
    {
        float originalIntensity = led.intensity;

        // Включить
        led.OnVoltageChanged(5f);
        yield return new WaitForSeconds(0.5f);

        // Выключить
        led.OnVoltageChanged(0f);
        yield return new WaitForSeconds(0.5f);

        // Вернуть исходное состояние
        led.OnVoltageChanged(originalIntensity > 0 ? 5f : 0f);
    }

    void UpdateComponentList()
    {
        CircuitComponent[] components = FindObjectsOfType<CircuitComponent>();
        allComponents = new List<CircuitComponent>(components);
    }

    void UpdateCircuitState()
    {
        foreach (CircuitComponent component in allComponents)
        {
            // Если компонент подключен к Arduino
            if (component.connectedArduino != null)
            {
                // Проверяем, есть ли напряжение на подключенном пине
                if (component.positivePin != -1)
                {
                    VirtualArduino arduino = component.connectedArduino;

                    // Получаем состояние пина
                    if (arduino.digitalPins.Count > component.positivePin)
                    {
                        var pin = arduino.digitalPins[component.positivePin];
                        if (pin.voltage > 0)
                        {
                            // Передаем напряжение компоненту
                            component.OnVoltageChanged(pin.voltage);
                        }
                    }
                }
            }
        }
    }

    public void PowerOnCircuit()
    {
        circuitPowered = true;
        Debug.Log("Circuit powered ON");

        // Включаем Arduino
        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        if (arduino != null)
        {
            // Устанавливаем напряжение на всех пинах, которые в режиме OUTPUT
            for (int i = 0; i < arduino.digitalPins.Count; i++)
            {
                if (arduino.digitalPins[i].mode == PinMode.Output)
                {
                    arduino.DigitalWrite(i, 1); // Включаем все выходы
                }
            }
        }
    }

    public void PowerOffCircuit()
    {
        circuitPowered = false;
        Debug.Log("Circuit powered OFF");

        // Выключаем Arduino
        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        if (arduino != null)
        {
            // Отключаем все пины
            for (int i = 0; i < arduino.digitalPins.Count; i++)
            {
                arduino.DigitalWrite(i, 0);
            }
        }

        // Выключаем все компоненты
        foreach (CircuitComponent component in allComponents)
        {
            component.OnVoltageChanged(0);
        }
    }

    public void CheckCircuitIntegrity()
    {
        Debug.Log("=== CIRCUIT INTEGRITY CHECK ===");

        int connectedComponents = 0;
        int poweredComponents = 0;

        foreach (CircuitComponent component in allComponents)
        {
            if (component.positivePin != -1)
            {
                connectedComponents++;
                Debug.Log($"✓ {component.name} connected to pin {component.positivePin}");

                if (component.connectedArduino != null)
                {
                    poweredComponents++;
                }
            }
            else
            {
                Debug.Log($"✗ {component.name} NOT connected");
            }
        }

        Debug.Log($"Connected: {connectedComponents}/{allComponents.Count}");
        Debug.Log($"Powered: {poweredComponents}/{connectedComponents}");

        if (connectedComponents == 0)
        {
            Debug.LogWarning("No components connected!");
        }
    }

    public void SimulateButtonPress(int pin)
    {
        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        if (arduino != null)
        {
            // Эмулируем нажатие кнопки (переключаем состояние)
            int currentState = arduino.DigitalRead(pin);
            int newState = currentState == 1 ? 0 : 1;

            arduino.DigitalWrite(pin, newState);
            Debug.Log($"Button press simulated on pin {pin}: {newState}");
        }
    }
}