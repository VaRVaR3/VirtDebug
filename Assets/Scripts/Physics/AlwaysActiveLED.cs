using UnityEngine;

[RequireComponent(typeof(VirtualLED))]
public class AlwaysActiveLED : MonoBehaviour
{
    private VirtualLED led;

    void Start()
    {
        led = GetComponent<VirtualLED>();
        if (led == null)
        {
            Debug.LogError("AlwaysActiveLED requires VirtualLED component!");
            return;
        }

        // Запускаем проверку каждую секунду
        InvokeRepeating("ForceActive", 0f, 1f);
    }

    void ForceActive()
    {
        if (led != null)
        {
            led.isActive = true; // Принудительно делаем активным

            // Также принудительно обновляем визуализацию
            if (led.intensity > 0.1f && led.currentVoltage < 0.1f)
            {
                Debug.LogWarning($"LED {name} has intensity {led.intensity} but no voltage!");
                led.OnVoltageChanged(5f); // Принудительно подаем напряжение
            }
        }
    }

    void Update()
    {
        // На всякий случай проверяем каждый кадр
        if (led != null && !led.isActive)
        {
            led.isActive = true;
        }
    }
}