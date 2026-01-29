using UnityEngine;

public class CurrentTest : MonoBehaviour
{
    public VirtualLED led;
    public VirtualArduino arduino;
    public VirtualGround ground;

    void Start()
    {
        Invoke("RunTest", 1f);
    }

    void RunTest()
    {
        Debug.Log("=== CURRENT FLOW TEST ===");

        if (led == null || arduino == null)
        {
            Debug.LogError("Missing components!");
            return;
        }

        // Тест 1: Проверка сопротивления
        Debug.Log($"LED Resistance: {led.resistance}Ω");

        // Тест 2: Расчет тока при 5V
        float calculatedCurrent = 5f / led.resistance;
        Debug.Log($"Theoretical current at 5V: {calculatedCurrent * 1000:F1}mA");

        // Тест 3: Проверка максимального тока LED
        Debug.Log($"LED Max Current: {led.maxOperatingCurrent * 1000:F1}mA");

        // Тест 4: Нужен ли резистор?
        float neededResistance = (5f - led.forwardVoltage) / led.maxOperatingCurrent;
        Debug.Log($"Needed resistor: {neededResistance:F0}Ω");

        // Тест 5: Мощность
        float power = calculatedCurrent * 5f;
        Debug.Log($"Power consumption: {power * 1000:F1}mW");
    }

    void Update()
    {
        // Показываем текущие параметры
        if (led != null && Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log($"LED - Voltage: {led.currentVoltage:F2}V, " +
                     $"Current: {led.currentCurrent * 1000:F1}mA, " +
                     $"Intensity: {led.intensity:F2}");
        }
    }
}