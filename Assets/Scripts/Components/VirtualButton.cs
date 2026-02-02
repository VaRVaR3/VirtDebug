using UnityEngine;
using System.Collections;

public class VirtualButton : CircuitComponent, IConnectable
{
    [Header("Button Settings")]
    public float pressDepth = 0.1f;
    public Color normalColor = Color.gray;
    public Color pressedColor = Color.green;

    [Header("Error Visualization")]
    public Color notConnectedColor = Color.gray;
    public Color errorColor = Color.red;
    public GameObject statusIndicator;

    [Header("Electrical Properties")]
    public float contactResistance = 0.01f; // При нажатии
    public float openResistance = 1000000f; // При отпускании

    private bool isPressed = false;
    private Vector3 originalPosition;
    private Renderer buttonRenderer;
    private Collider buttonCollider;
    private Renderer statusRenderer;
    private ButtonStatus currentStatus = ButtonStatus.NotConnected;

    void Start()
    {
        InitializeButton();
    }

    void InitializeButton()
    {
        buttonRenderer = GetComponent<Renderer>();
        buttonCollider = GetComponent<Collider>();
        originalPosition = transform.position;

        if (statusIndicator == null)
        {
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.name = "ButtonStatus";
            indicator.transform.SetParent(transform);
            indicator.transform.localPosition = new Vector3(0, 0.5f, 0);
            indicator.transform.localScale = Vector3.one * 0.05f;
            statusIndicator = indicator;
        }

        statusRenderer = statusIndicator.GetComponent<Renderer>();
        UpdateStatusIndicator();

        resistance = openResistance;
        maxVoltage = 5.0f;
        maxCurrent = 0.1f;
        requiresPolarity = false;

        if (buttonRenderer != null)
            buttonRenderer.material.color = normalColor;
    }

    void OnMouseDown()
    {
        PressButton();
    }

    void OnMouseUp()
    {
        ReleaseButton();
    }

    public void PressButton()
    {
        if (!isActive)
        {
            ShowError("Кнопка не активна! Подключите её к цепи.");
            return;
        }

        if (positivePin == -1)
        {
            ShowError("Кнопка не подключена к пину Arduino! Подключите один вывод к цифровому пину.");
            return;
        }

        if (connectedArduino == null)
        {
            ShowError("Кнопка не подключена к Arduino!");
            return;
        }

        isPressed = true;

        if (buttonRenderer != null)
            buttonRenderer.material.color = pressedColor;

        transform.position = originalPosition - new Vector3(0, pressDepth, 0);

        connectedArduino.DigitalWrite(positivePin, 1);
        Debug.Log($"Button pressed, sending HIGH to pin {positivePin}");

        currentStatus = ButtonStatus.Pressed;
        UpdateStatusIndicator();
    }

    public void ReleaseButton()
    {
        if (!isActive) return;

        isPressed = false;

        if (buttonRenderer != null)
            buttonRenderer.material.color = normalColor;

        transform.position = originalPosition;

        if (positivePin >= 0 && connectedArduino != null)
        {
            connectedArduino.DigitalWrite(positivePin, 0);
            Debug.Log($"Button released, sending LOW to pin {positivePin}");
        }

        currentStatus = ButtonStatus.Ready;
        UpdateStatusIndicator();
    }

    void ShowError(string message)
    {
        Debug.LogError($"Button {name}: {message}");
        currentStatus = ButtonStatus.Error;
        UpdateStatusIndicator();

        StartCoroutine(ErrorFlash());
    }

    IEnumerator ErrorFlash()
    {
        if (buttonRenderer == null) yield break;

        Color originalColor = buttonRenderer.material.color;

        for (int i = 0; i < 3; i++)
        {
            buttonRenderer.material.color = errorColor;
            yield return new WaitForSeconds(0.2f);
            buttonRenderer.material.color = originalColor;
            yield return new WaitForSeconds(0.2f);
        }
    }

    void UpdateStatusIndicator()
    {
        if (statusRenderer == null) return;

        switch (currentStatus)
        {
            case ButtonStatus.NotConnected:
                statusRenderer.material.color = Color.gray;
                break;
            case ButtonStatus.Ready:
                statusRenderer.material.color = Color.green;
                break;
            case ButtonStatus.Pressed:
                statusRenderer.material.color = Color.yellow;
                break;
            case ButtonStatus.Error:
                statusRenderer.material.color = Color.red;
                break;
        }
    }

    public bool IsPressed()
    {
        return isPressed;
    }

    public override void OnVoltageChanged(float voltage)
    {
        currentVoltage = voltage;

        if (voltage > 2.5f && !isPressed)
        {
            Debug.Log($"Button {name}: Detected external voltage {voltage}V");
        }
    }

    public override void UpdateComponent()
    {
        if (Input.GetKeyDown(KeyCode.Space) && gameObject.activeInHierarchy)
        {
            PressButton();
        }
        else if (Input.GetKeyUp(KeyCode.Space) && gameObject.activeInHierarchy)
        {
            ReleaseButton();
        }

        if (connectedArduino == null || positivePin == -1)
        {
            currentStatus = ButtonStatus.NotConnected;
        }
        else if (currentStatus != ButtonStatus.Pressed && currentStatus != ButtonStatus.Error)
        {
            currentStatus = ButtonStatus.Ready;
        }

        UpdateStatusIndicator();
    }

    void OnDestroy()
    {
        if (isPressed && positivePin >= 0 && connectedArduino != null)
        {
            connectedArduino.DigitalWrite(positivePin, 0);
        }
    }

    // ========== IConnectable IMPLEMENTATION (с override) ==========

    public override string GetName()
    {
        return name;
    }

    public override void OnConnected(int pin, IConnectable otherComponent, int otherPin)
    {
        Debug.Log($"Button {name} pin {pin} connected to {otherComponent.GetName()}");
        positivePin = otherPin;
        if (otherComponent is VirtualArduino arduino)
            connectedArduino = arduino;
    }

    public override void OnDisconnected(int pin)
    {
        Debug.Log($"Button {name} pin {pin} disconnected");
        positivePin = -1;
        connectedArduino = null;
    }

    public override Vector3 GetPinPosition(int pin)
    {
        // Пробуем найти UltraSimplePin
        UltraSimplePin[] ultraPins = GetComponentsInChildren<UltraSimplePin>();
        foreach (var pinObj in ultraPins)
        {
            if (pinObj.pinNumber == pin)
                return pinObj.transform.position;
        }

        // Если UltraSimplePin не найден, пробуем найти PinHighlighter
        PinHighlighter[] oldPins = GetComponentsInChildren<PinHighlighter>();
        foreach (var pinObj in oldPins)
        {
            if (pinObj.pinNumber == pin)
                return pinObj.transform.position;
        }
        return transform.position;
    }

    public override bool CanConnectTo(int pin, IConnectable otherComponent, int otherPin)
    {
        return true;
    }

    [ContextMenu("Тест кнопки")]
    public void TestButton()
    {
        Debug.Log("=== Тест кнопки ===");

        if (connectedArduino == null)
        {
            Debug.LogError("Кнопка не подключена к Arduino!");
            return;
        }

        PressButton();
        StartCoroutine(TestRelease());
    }

    IEnumerator TestRelease()
    {
        yield return new WaitForSeconds(0.5f);
        ReleaseButton();
    }
}

public enum ButtonStatus
{
    NotConnected,
    Ready,
    Pressed,
    Error
}