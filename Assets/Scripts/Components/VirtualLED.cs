using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VirtualLED : CircuitComponent
{
    [Header("LED Physical Properties")]
    public Color ledColor = Color.red;
    public float forwardVoltage = 2.1f;
    public float maxOperatingCurrent = 0.02f;

    [Header("Visual Settings")]
    public float maxEmissionIntensity = 3f;
    public float maxLightIntensity = 2f;

    [Header("Error Visualization")]
    public Color warningColor = Color.yellow;
    public Color errorColor = Color.red;
    public Color successColor = Color.green;
    public GameObject statusIndicator;

    private Renderer ledRenderer;
    private Light ledLight;
    private Material emissionMaterial;
    private float currentIntensity = 0f;

    private LEDStatus currentStatus = LEDStatus.NotConnected;
    private List<string> statusMessages = new List<string>();
    private Renderer statusRenderer;

    public float intensity { get { return currentIntensity; } }

    // ===== NEW: реальные связи =====
    // pin 1 = анод (+), pin 2 = катод (-) (зафиксируем это как правило)
    private IConnectable anodeConnectedTo;
    private int anodeConnectedPin = -1;

    private IConnectable cathodeConnectedTo;
    private int cathodeConnectedPin = -1;

    void Start()
    {
        InitializeLED();
    }

    void InitializeLED()
    {
        ledRenderer = GetComponentInChildren<Renderer>();
        ledLight = GetComponentInChildren<Light>();

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

        resistance = 220f;
        base.maxVoltage = 5.0f;
        requiresPolarity = true;
        canBurnOut = true;

        if (ledRenderer != null)
        {
            // NOTE: если URP и Standard = null, лучше поменять как для проводов,
            // но пока оставим, это не блокирует соединения.
            Shader s = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            emissionMaterial = new Material(s);
            emissionMaterial.color = ledColor;
            if (emissionMaterial.HasProperty("_EmissionColor"))
            {
                emissionMaterial.EnableKeyword("_EMISSION");
                emissionMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                emissionMaterial.SetColor("_EmissionColor", Color.black);
            }
            ledRenderer.material = emissionMaterial;
        }

        if (ledLight != null)
        {
            ledLight.color = ledColor;
            ledLight.intensity = 0f;
            ledLight.enabled = false;
        }

        AddStatusMessage($"LED {name} готов. Подключите анод (pin 1) к сигналу/питанию, катод (pin 2) к GND.", LEDStatus.Info);
    }

    void UpdateStatusIndicator()
    {
        if (statusRenderer == null) return;

        switch (currentStatus)
        {
            case LEDStatus.NotConnected: statusRenderer.material.color = Color.gray; break;
            case LEDStatus.PartialConnection: statusRenderer.material.color = Color.yellow; break;
            case LEDStatus.ReversePolarity: statusRenderer.material.color = Color.red; break;
            case LEDStatus.NoResistor: statusRenderer.material.color = new Color(1f, 0.5f, 0f); break;
            case LEDStatus.ShortCircuit:
                statusRenderer.material.color = Color.red;
                StartCoroutine(ErrorFlash());
                break;
            case LEDStatus.OK:
            case LEDStatus.Success: statusRenderer.material.color = Color.green; break;
            case LEDStatus.Warning: statusRenderer.material.color = Color.yellow; break;
            case LEDStatus.Error: statusRenderer.material.color = Color.red; break;
            case LEDStatus.Info: statusRenderer.material.color = Color.blue; break;
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
            AddStatusMessage("LED подключен только с одной стороны. Нужно подключить и анод, и катод.", LEDStatus.Warning);
            SetIntensity(0f);
            return;
        }

        if (currentStatus == LEDStatus.ReversePolarity)
        {
            AddStatusMessage("LED подключен наоборот! Анод должен быть к сигналу/питанию, катод к GND.", LEDStatus.Error);
            SetIntensity(0f);
            return;
        }

        if (currentStatus == LEDStatus.NoResistor)
        {
            AddStatusMessage("ВНИМАНИЕ: в цепи нет ограничивающего резистора! LED может перегореть.", LEDStatus.Warning);
        }

        if (currentStatus == LEDStatus.ShortCircuit)
        {
            AddStatusMessage("ОШИБКА: короткое замыкание! LED подключен напрямую без резистора.", LEDStatus.Error);
            BurnOut();
            return;
        }

        currentVoltage = voltage;

        if (requiresPolarity && voltage < 0)
        {
            AddStatusMessage("Обратное напряжение! LED не будет светиться.", LEDStatus.Warning);
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
        float calculatedCurrent = effectiveVoltage / Mathf.Max(0.0001f, resistance);

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
            AddStatusMessage($"LED работает: {voltage:F2}V, {calculatedCurrent:F3}A, яркость: {currentIntensity * 100f:F0}%", LEDStatus.Success);
        }
    }

    void AnalyzeConnection()
    {
        bool anodeConnected = (anodeConnectedTo != null && anodeConnectedPin >= 0);
        bool cathodeConnected = (cathodeConnectedTo != null && cathodeConnectedPin >= 0);

        if (!anodeConnected && !cathodeConnected)
        {
            currentStatus = LEDStatus.NotConnected;
            return;
        }

        if (!anodeConnected || !cathodeConnected)
        {
            currentStatus = LEDStatus.PartialConnection;
            return;
        }

        // ЛОГИКА ПОЛЯРНОСТИ:
        // норм: катод (pin2) на GND, анод (pin1) на сигнал/питание
        bool cathodeIsGround = IsGroundComponent(cathodeConnectedTo);
        bool anodeIsGround = IsGroundComponent(anodeConnectedTo);

        if (anodeIsGround && !cathodeIsGround)
        {
            currentStatus = LEDStatus.ReversePolarity;
            return;
        }

        // Проверка резистора - пока грубо, т.к. сопротивление может быть не только в LED
        if (resistance < 100f)
        {
            currentStatus = (resistance < 10f) ? LEDStatus.ShortCircuit : LEDStatus.NoResistor;
            return;
        }

        currentStatus = LEDStatus.OK;
    }

    private bool IsGroundComponent(IConnectable comp)
    {
        if (comp == null) return false;

        MonoBehaviour m = comp as MonoBehaviour;
        if (m == null) return false;

        if (m.GetComponent<ArduinoGroundPin>() != null) return true;
        if (m.name.ToUpper().Contains("GND")) return true;
        return false;
    }

    void SetIntensity(float intensity)
    {
        currentIntensity = Mathf.Clamp01(intensity);

        if (emissionMaterial != null && emissionMaterial.HasProperty("_EmissionColor"))
        {
            Color emissionColor = ledColor * currentIntensity * maxEmissionIntensity;
            emissionMaterial.SetColor("_EmissionColor", emissionColor);

            if (Application.isPlaying && ledRenderer != null)
            {
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
            case LEDStatus.Error: Debug.LogError($"LED {name}: {message}"); break;
            case LEDStatus.Warning: Debug.LogWarning($"LED {name}: {message}"); break;
            default: Debug.Log($"LED {name}: {message}"); break;
        }

        if (statusMessages.Count > 5)
            statusMessages.RemoveAt(0);

        if (status != LEDStatus.Error && status != LEDStatus.Warning)
            StartCoroutine(ClearStatusAfterDelay(5f));
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
            Shader s = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            Material burnedMat = new Material(s);
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

    // ===== IConnectable hooks =====
    public override void OnConnected(int pin, IConnectable otherComponent, int otherPin)
    {
        Debug.Log($"LED {name} pin {pin} connected to {otherComponent.GetName()} pin {otherPin}");

        if (pin == 1)
        {
            anodeConnectedTo = otherComponent;
            anodeConnectedPin = otherPin;

            // если подключились к Arduino, сохраним ссылку
            if (otherComponent is VirtualArduino arduino)
                connectedArduino = arduino;
        }
        else if (pin == 2)
        {
            cathodeConnectedTo = otherComponent;
            cathodeConnectedPin = otherPin;
        }

        // Обновим “старые” поля ради совместимости с остальным кодом
        positivePin = (anodeConnectedTo != null) ? 1 : -1;
        negativePin = (cathodeConnectedTo != null) ? 2 : -1;

        AnalyzeConnection();
        UpdateStatusIndicator();
    }

    public override void OnDisconnected(int pin)
    {
        Debug.Log($"LED {name} pin {pin} disconnected");

        if (pin == 1)
        {
            anodeConnectedTo = null;
            anodeConnectedPin = -1;
            connectedArduino = null;
        }
        else if (pin == 2)
        {
            cathodeConnectedTo = null;
            cathodeConnectedPin = -1;
        }

        positivePin = (anodeConnectedTo != null) ? 1 : -1;
        negativePin = (cathodeConnectedTo != null) ? 2 : -1;

        AnalyzeConnection();
        UpdateStatusIndicator();
    }

    public override Vector3 GetPinPosition(int pin)
    {
        // Держимся только UltraSimplePin, чтобы не было конфликтов систем
        UltraSimplePin[] ultraPins = GetComponentsInChildren<UltraSimplePin>(true);
        foreach (var pinObj in ultraPins)
        {
            if (pinObj != null && pinObj.pinNumber == pin)
                return pinObj.transform.position;
        }

        // fallback
        return transform.position + new Vector3((pin == 1 ? -0.1f : 0.1f), 0, 0);
    }

    public override bool CanConnectTo(int pin, IConnectable otherComponent, int otherPin)
    {
        return true;
    }

    // ===== Доп методы =====
    public void UpdateWithCurrentFlow(float positiveVoltage, float negativeVoltage)
    {
        float voltageDifference = positiveVoltage - negativeVoltage;
        OnVoltageChanged(voltageDifference);
    }

    public bool IsMaterialInitialized()
    {
        return emissionMaterial != null;
    }

    [ContextMenu("Сбросить LED")]
    public void ResetLED()
    {
        isActive = true;
        currentIntensity = 0f;
        ClearStatusMessages();
        currentStatus = LEDStatus.NotConnected;

        anodeConnectedTo = null;
        anodeConnectedPin = -1;
        cathodeConnectedTo = null;
        cathodeConnectedPin = -1;

        positivePin = -1;
        negativePin = -1;

        if (ledRenderer != null && emissionMaterial != null)
        {
            ledRenderer.material = emissionMaterial;
            if (emissionMaterial.HasProperty("_EmissionColor"))
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
