using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VirtualLED : CircuitComponent, IConnectable
{
    [Header("LED Physical Properties")]
    public Color ledColor = Color.red;
    public float forwardVoltage = 2.1f;
    public float maxOperatingCurrent = 0.02f; // Переименовано из maxCurrent

    [Header("Visual Settings")]
    public float maxEmissionIntensity = 3f;
    public float maxLightIntensity = 2f;

    [Header("Error Visualization")]
    public Color warningColor = Color.yellow;
    public Color errorColor = Color.red;
    public Color successColor = Color.green;
    public GameObject statusIndicator;

    // Визуальные компоненты
    private Renderer ledRenderer;
    private Light ledLight;
    private Material emissionMaterial;
    private float currentIntensity = 0f;

    // Статус цепи
    private LEDStatus currentStatus = LEDStatus.NotConnected;
    private List<string> statusMessages = new List<string>();
    private Renderer statusRenderer;

    // Дополнительные свойства для доступа
    public float intensity { get { return currentIntensity; } }

    void Start()
    {
        InitializeLED();
    }

    void InitializeLED()
    {
        ledRenderer = GetComponentInChildren<Renderer>();
        ledLight = GetComponentInChildren<Light>();

        // Инициализируем индикатор статуса
        if (statusIndicator == null)
        {
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "StatusIndicator";
            indicator.transform.SetParent(transform);
            indicator.transform.localPosition = new Vector3(0, 0.3f, 0);
            indicator.transform.localScale = Vector3.one * 0.1f;
            statusIndicator = indicator;
        }

        statusRenderer = statusIndicator.GetComponent<Renderer>();
        UpdateStatusIndicator();

        // Настройки компонента
        resistance = 220f;
        base.maxVoltage = 5.0f; // Используем base для доступа к родительскому
        requiresPolarity = true;
        canBurnOut = true;

        // Создаём материал со свечением
        if (ledRenderer != null)
        {
            emissionMaterial = new Material(Shader.Find("Standard"));
            emissionMaterial.color = ledColor;
            emissionMaterial.EnableKeyword("_EMISSION");
            emissionMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            ledRenderer.material = emissionMaterial;
            emissionMaterial.SetColor("_EmissionColor", Color.black);
        }

        if (ledLight != null)
        {
            ledLight.color = ledColor;
            ledLight.intensity = 0f;
            ledLight.enabled = false;
        }

        AddStatusMessage($"LED {name} готов к работе. Подключите анод (+) к цифровому пину, катод (-) к GND.", LEDStatus.Info);
    }

    void UpdateStatusIndicator()
    {
        if (statusRenderer == null) return;

        switch (currentStatus)
        {
            case LEDStatus.NotConnected:
                statusRenderer.material.color = Color.gray;
                break;
            case LEDStatus.PartialConnection:
                statusRenderer.material.color = Color.yellow;
                break;
            case LEDStatus.ReversePolarity:
                statusRenderer.material.color = Color.red;
                break;
            case LEDStatus.NoResistor:
                statusRenderer.material.color = new Color(1f, 0.5f, 0f);
                break;
            case LEDStatus.ShortCircuit:
                statusRenderer.material.color = Color.red;
                StartCoroutine(ErrorFlash());
                break;
            case LEDStatus.OK:
            case LEDStatus.Success:
                statusRenderer.material.color = Color.green;
                break;
            case LEDStatus.Warning:
                statusRenderer.material.color = Color.yellow;
                break;
            case LEDStatus.Error:
                statusRenderer.material.color = Color.red;
                break;
            case LEDStatus.Info:
                statusRenderer.material.color = Color.blue;
                break;
        }
    }

    IEnumerator ErrorFlash()
    {
        for (int i = 0; i < 3; i++)
        {
            if (statusRenderer != null) statusRenderer.enabled = false;
            yield return new WaitForSeconds(0.2f);
            if (statusRenderer != null) statusRenderer.enabled = true;
            yield return new WaitForSeconds(0.2f);
        }
    }

    public override void OnVoltageChanged(float voltage)
    {
        ClearStatusMessages();
        AnalyzeConnection();

        if (currentStatus == LEDStatus.NotConnected)
        {
            AddStatusMessage("LED не подключен к цепи!", LEDStatus.Error);
            SetIntensity(0f);
            return;
        }

        if (currentStatus == LEDStatus.PartialConnection)
        {
            AddStatusMessage("LED подключен только с одной стороны. Нужно подключить и к пину, и к GND.", LEDStatus.Warning);
            SetIntensity(0f);
            return;
        }

        if (currentStatus == LEDStatus.ReversePolarity)
        {
            AddStatusMessage("LED подключен неправильно! Анод (+) должен быть к пину Arduino, катод (-) к GND.", LEDStatus.Error);
            SetIntensity(0f);
            return;
        }

        if (currentStatus == LEDStatus.NoResistor)
        {
            AddStatusMessage("ВНИМАНИЕ: В цепи нет ограничивающего резистора! LED может перегореть.", LEDStatus.Warning);
        }

        if (currentStatus == LEDStatus.ShortCircuit)
        {
            AddStatusMessage("ОШИБКА: Короткое замыкание! LED подключен напрямую к питанию без резистора.", LEDStatus.Error);
            BurnOut();
            return;
        }

        currentVoltage = voltage;

        if (requiresPolarity && voltage < 0)
        {
            AddStatusMessage("Обратное напряжение! LED защищён, но не будет светиться.", LEDStatus.Warning);
            SetIntensity(0f);
            return;
        }

        if (voltage < forwardVoltage)
        {
            AddStatusMessage($"Напряжение {voltage:F2}V недостаточно. Нужно минимум {forwardVoltage}V.", LEDStatus.Info);
            SetIntensity(0f);
            return;
        }

        float effectiveVoltage = voltage - forwardVoltage;
        float calculatedCurrent = effectiveVoltage / resistance;

        if (calculatedCurrent > maxOperatingCurrent * 1.5f)
        {
            AddStatusMessage($"СЛИШКОМ ВЫСОКИЙ ТОК: {calculatedCurrent:F3}A > {maxOperatingCurrent}A! LED перегорит.", LEDStatus.Error);
            BurnOut();
            return;
        }
        else if (calculatedCurrent > maxOperatingCurrent)
        {
            AddStatusMessage($"Ток близок к максимальному: {calculatedCurrent:F3}A. Увеличьте сопротивление.", LEDStatus.Warning);
        }

        float normalizedCurrent = Mathf.Clamp01(calculatedCurrent / maxOperatingCurrent);
        currentIntensity = Mathf.Pow(normalizedCurrent, 0.7f);

        SetIntensity(currentIntensity);

        if (currentIntensity > 0.1f)
        {
            AddStatusMessage($"LED работает: {voltage:F2}V, {calculatedCurrent:F3}A, яркость: {currentIntensity:F1}%", LEDStatus.Success);
        }
    }

    void AnalyzeConnection()
    {
        if (positivePin == -1 && negativePin == -1)
        {
            currentStatus = LEDStatus.NotConnected;
            return;
        }

        if (positivePin == -1 || negativePin == -1)
        {
            currentStatus = LEDStatus.PartialConnection;
            return;
        }

        if (connectedArduino != null)
        {
            if (positivePin >= 100 && negativePin < 100)
            {
                currentStatus = LEDStatus.ReversePolarity;
                return;
            }
        }

        if (resistance < 100f)
        {
            if (resistance < 10f)
            {
                currentStatus = LEDStatus.ShortCircuit;
            }
            else
            {
                currentStatus = LEDStatus.NoResistor;
            }
            return;
        }

        currentStatus = LEDStatus.OK;
    }

    void SetIntensity(float intensity)
    {
        currentIntensity = Mathf.Clamp01(intensity);

        if (emissionMaterial != null)
        {
            Color emissionColor = ledColor * currentIntensity * maxEmissionIntensity;
            emissionMaterial.SetColor("_EmissionColor", emissionColor);

            if (Application.isPlaying)
            {
                // Используем новый API вместо старого DynamicGI
                ledRenderer.UpdateGIMaterials();
            }
        }

        if (ledLight != null)
        {
            if (currentIntensity > 0.05f)
            {
                if (!ledLight.enabled) ledLight.enabled = true;
                ledLight.intensity = currentIntensity * maxLightIntensity;
                ledLight.range = 1f + currentIntensity * 2f;
            }
            else
            {
                if (ledLight.enabled) ledLight.enabled = false;
            }
        }

        UpdateStatusIndicator();
    }

    void AddStatusMessage(string message, LEDStatus status)
    {
        statusMessages.Add(message);

        switch (status)
        {
            case LEDStatus.Error:
                Debug.LogError($"LED {name}: {message}");
                break;
            case LEDStatus.Warning:
                Debug.LogWarning($"LED {name}: {message}");
                break;
            default:
                Debug.Log($"LED {name}: {message}");
                break;
        }

        if (statusMessages.Count > 5)
        {
            statusMessages.RemoveAt(0);
        }

        if (status != LEDStatus.Error && status != LEDStatus.Warning)
        {
            StartCoroutine(ClearStatusAfterDelay(5f));
        }
    }

    IEnumerator ClearStatusAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (statusMessages.Count > 0 && currentStatus != LEDStatus.Error)
        {
            statusMessages.Clear();
            currentStatus = LEDStatus.OK;
            UpdateStatusIndicator();
        }
    }

    void ClearStatusMessages()
    {
        statusMessages.Clear();
    }

    void BurnOut()
    {
        isActive = false;
        AddStatusMessage("LED ПЕРЕГОРЕЛ! Слишком высокий ток или короткое замыкание.", LEDStatus.Error);
        StartCoroutine(BurnOutEffect());

        if (ledRenderer != null)
        {
            Material burnedMat = new Material(Shader.Find("Standard"));
            burnedMat.color = new Color(0.3f, 0.3f, 0.3f);
            ledRenderer.material = burnedMat;
        }
    }

    IEnumerator BurnOutEffect()
    {
        if (ledLight != null)
        {
            for (int i = 0; i < 3; i++)
            {
                ledLight.enabled = !ledLight.enabled;
                yield return new WaitForSeconds(0.15f);
            }

            float startIntensity = ledLight.intensity;
            for (float t = 0; t < 1f; t += Time.deltaTime * 2f)
            {
                if (ledLight != null)
                {
                    ledLight.intensity = Mathf.Lerp(startIntensity, 0f, t);
                    yield return null;
                }
            }

            if (ledLight != null) ledLight.enabled = false;
        }
    }

    public override void UpdateComponent()
    {
        if (!isActive) return;

        if (currentIntensity > 0.05f && ledLight != null)
        {
            ledLight.intensity = Mathf.Lerp(
                ledLight.intensity,
                currentIntensity * maxLightIntensity,
                Time.deltaTime * 10f
            );
        }
    }

    // Реализация IConnectable
    public string GetName()
    {
        return name;
    }

    public void OnConnected(int pin, IConnectable otherComponent, int otherPin)
    {
        Debug.Log($"LED {name} pin {pin} connected to {otherComponent.GetName()} pin {otherPin}");

        if (pin == 1) // положительный пин
        {
            positivePin = otherPin;
            if (otherComponent is VirtualArduino arduino)
                connectedArduino = arduino;
        }
        else if (pin == 2) // отрицательный пин
        {
            negativePin = otherPin;
        }
    }

    public void OnDisconnected(int pin)
    {
        Debug.Log($"LED {name} pin {pin} disconnected");

        if (pin == 1)
        {
            positivePin = -1;
            connectedArduino = null;
        }
        else if (pin == 2)
        {
            negativePin = -1;
        }
    }

    public Vector3 GetPinPosition(int pin)
    {
        PinHighlighter[] pins = GetComponentsInChildren<PinHighlighter>();
        foreach (PinHighlighter pinObj in pins)
        {
            if (pinObj.pinNumber == pin)
            {
                return pinObj.transform.position;
            }
        }

        return transform.position + new Vector3((pin == 1 ? -0.1f : 0.1f), 0, 0);
    }

    public bool CanConnectTo(int pin, IConnectable otherComponent, int otherPin)
    {
        return true;
    }

    // Новый метод для тестов
    public void UpdateWithCurrentFlow(float positiveVoltage, float negativeVoltage)
    {
        float voltageDifference = positiveVoltage - negativeVoltage;
        OnVoltageChanged(voltageDifference);
    }

    public bool IsMaterialInitialized()
    {
        return emissionMaterial != null;
    }

    public void ConnectToPins(int posPin, int negPin)
    {
        positivePin = posPin;
        negativePin = negPin;
    }

    [ContextMenu("Тест: Правильное подключение")]
    public void TestCorrectConnection()
    {
        Debug.Log("=== Тест правильного подключения LED ===");
        connectedArduino = FindObjectOfType<VirtualArduino>();
        positivePin = 13;
        negativePin = 100;
        resistance = 220f;

        OnVoltageChanged(5f);
    }

    [ContextMenu("Тест: Без резистора")]
    public void TestNoResistor()
    {
        Debug.Log("=== Тест подключения без резистора ===");
        connectedArduino = FindObjectOfType<VirtualArduino>();
        positivePin = 13;
        negativePin = 100;
        resistance = 10f;

        OnVoltageChanged(5f);
    }

    [ContextMenu("Тест: Обратная полярность")]
    public void TestReversePolarity()
    {
        Debug.Log("=== Тест обратной полярности ===");
        connectedArduino = FindObjectOfType<VirtualArduino>();
        positivePin = 100;
        negativePin = 13;
        resistance = 220f;

        OnVoltageChanged(5f);
    }

    [ContextMenu("Сбросить LED")]
    public void ResetLED()
    {
        isActive = true;
        currentIntensity = 0f;
        ClearStatusMessages();
        currentStatus = LEDStatus.NotConnected;

        if (ledRenderer != null && emissionMaterial != null)
        {
            ledRenderer.material = emissionMaterial;
            emissionMaterial.SetColor("_EmissionColor", Color.black);
        }

        if (ledLight != null) ledLight.enabled = false;
        UpdateStatusIndicator();
    }

    public List<string> GetStatusMessages()
    {
        return new List<string>(statusMessages);
    }

    public LEDStatus GetCurrentStatus()
    {
        return currentStatus;
    }
}