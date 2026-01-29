using UnityEngine;

public class LEDProtector : MonoBehaviour
{
    [Header("Protection Settings")]
    public VirtualLED led;
    public float maxSafeVoltage = 3.3f;
    public float maxSafeCurrent = 0.02f;
    public bool autoAdjustResistance = true;

    [Header("Warning Indicators")]
    public GameObject warningLight;
    public AudioClip warningSound;

    private float originalResistance;
    private bool isOverloaded = false;

    void Start()
    {
        if (led == null)
        {
            led = GetComponent<VirtualLED>();
            if (led == null)
            {
                led = GetComponentInChildren<VirtualLED>();
            }
        }

        if (led != null)
        {
            originalResistance = led.resistance;
            Debug.Log($"LED Protector initialized for {led.name}");
        }

        if (warningLight != null)
        {
            warningLight.SetActive(false);
        }
    }

    void Update()
    {
        if (led == null) return;

        // Проверяем безопасность
        CheckSafety();

        // Автоматическая регулировка сопротивления
        if (autoAdjustResistance && led.currentVoltage > 0)
        {
            AdjustResistanceForSafety();
        }
    }

    void CheckSafety()
    {
        bool wasOverloaded = isOverloaded;

        // Проверка напряжения
        if (led.currentVoltage > maxSafeVoltage)
        {
            isOverloaded = true;
            if (!wasOverloaded)
            {
                Debug.LogWarning($"LED {led.name}: Voltage overload! {led.currentVoltage:F2}V > {maxSafeVoltage}V");
                ShowWarning();
            }
        }
        // Проверка тока
        else if (led.currentCurrent > maxSafeCurrent)
        {
            isOverloaded = true;
            if (!wasOverloaded)
            {
                Debug.LogWarning($"LED {led.name}: Current overload! {led.currentCurrent:F3}A > {maxSafeCurrent}A");
                ShowWarning();
            }
        }
        else
        {
            isOverloaded = false;
            if (wasOverloaded)
            {
                Debug.Log($"LED {led.name}: Back to safe operation");
                HideWarning();
            }
        }
    }

    void AdjustResistanceForSafety()
    {
        float targetCurrent = maxSafeCurrent * 0.8f; // 80% от максимального
        float neededResistance = (led.currentVoltage - led.forwardVoltage) / targetCurrent;

        if (neededResistance > 0 && neededResistance > originalResistance)
        {
            led.resistance = neededResistance;
        }
        else
        {
            led.resistance = originalResistance;
        }
    }

    void ShowWarning()
    {
        if (warningLight != null)
        {
            warningLight.SetActive(true);
        }

        if (warningSound != null)
        {
            AudioSource.PlayClipAtPoint(warningSound, transform.position);
        }
    }

    void HideWarning()
    {
        if (warningLight != null)
        {
            warningLight.SetActive(false);
        }
    }

    // Публичные методы для управления
    public void EnableProtection()
    {
        autoAdjustResistance = true;
        Debug.Log($"Protection enabled for {led.name}");
    }

    public void DisableProtection()
    {
        autoAdjustResistance = false;
        led.resistance = originalResistance;
        Debug.Log($"Protection disabled for {led.name}");
    }

    public void SetSafeLimits(float voltage, float current)
    {
        maxSafeVoltage = voltage;
        maxSafeCurrent = current;
        Debug.Log($"Safe limits set: {voltage}V, {current}A");
    }
}