using System.Collections.Generic;
using UnityEngine;
using System;

public class VirtualArduino : MonoBehaviour, IConnectable
{
    [System.Serializable]
    public class Pin
    {
        public int number;
        public PinMode mode = PinMode.Input;
        public float voltage = 0f;
        public float current = 0f;
        public float resistance = 0f;
        public bool isConnected = false;
        public IConnectable connectedComponent = null;
        public bool isPWM = false;

        public const float MAX_CURRENT = 0.04f;
        public const float OPERATING_VOLTAGE = 5.0f;

        public Pin(int pinNumber)
        {
            number = pinNumber;
            if (pinNumber == 3 || pinNumber == 5 || pinNumber == 6 ||
                pinNumber == 9 || pinNumber == 10 || pinNumber == 11)
            {
                isPWM = true;
            }
        }
    }

    [Header("Ground Pins")]
    public List<ArduinoGroundPin> groundPins = new List<ArduinoGroundPin>();
    public bool autoCreateGroundPins = true;

    public List<Pin> digitalPins = new List<Pin>();
    public List<Pin> analogPins = new List<Pin>();
    public Dictionary<string, string> variables = new Dictionary<string, string>();

    private Dictionary<int, List<WireConnection>> connections = new Dictionary<int, List<WireConnection>>();

    public event Action<int, int> OnDigitalWrite;
    public event Action<int, float> OnAnalogWrite;
    public event Action<string> OnSerialData;

    void Start()
    {
        InitializePins();

        if (autoCreateGroundPins)
        {
            CreateGroundPins();
        }
    }

    // ========== IConnectable IMPLEMENTATION ==========

    public string GetName()
    {
        return name;
    }

    public void OnConnected(int pin, IConnectable otherComponent, int otherPin)
    {
        Debug.Log($"Arduino {name}: pin {pin} connected to {otherComponent.GetName()}:{otherPin}");
    }

    public void OnDisconnected(int pin)
    {
        Debug.Log($"Arduino {name}: pin {pin} disconnected");
    }

    public Vector3 GetPinPosition(int pin)
    {
        // Пробуем найти UltraSimplePin
        UltraSimplePin[] ultraPins = GetComponentsInChildren<UltraSimplePin>();
        foreach (var pinObj in ultraPins)
        {
            if (pinObj.pinNumber == pin)
            {
                return pinObj.transform.position;
            }
        }

        // Если UltraSimplePin не найден, пробуем найти PinHighlighter
        PinHighlighter[] oldPins = GetComponentsInChildren<PinHighlighter>();
        foreach (var pinObj in oldPins)
        {
            if (pinObj.pinNumber == pin)
            {
                return pinObj.transform.position;
            }
        }

        // Для GND пинов
        foreach (var groundPin in groundPins)
        {
            if (groundPin.pinNumber == pin)
            {
                return groundPin.transform.position;
            }
        }

        return transform.position + new Vector3(0.1f * (pin % 10), 0, 0.1f * (pin / 10));
    }

    public bool CanConnectTo(int pin, IConnectable otherComponent, int otherPin)
    {
        return true;
    }

    // ========== ОСТАЛЬНЫЕ МЕТОДЫ (без изменений) ==========

    void CreateGroundPins()
    {
        if (groundPins.Count == 0)
        {
            CreateGroundPin(100, "GND1", new Vector3(-0.5f, 0.1f, 0));
            CreateGroundPin(101, "GND2", new Vector3(-0.5f, -0.1f, 0));
        }
    }

    void CreateGroundPin(int pinNumber, string pinName, Vector3 localPosition)
    {
        GameObject groundPinObj = new GameObject($"Arduino_GND_{pinNumber}");
        groundPinObj.transform.SetParent(transform);
        groundPinObj.transform.localPosition = localPosition;
        groundPinObj.transform.localRotation = Quaternion.identity;

        GameObject pinVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pinVisual.name = "Pin_Visual";
        pinVisual.transform.SetParent(groundPinObj.transform);
        pinVisual.transform.localPosition = Vector3.zero;
        pinVisual.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);

        ArduinoGroundPin groundPin = groundPinObj.AddComponent<ArduinoGroundPin>();
        groundPin.pinNumber = pinNumber;
        groundPin.pinName = pinName;

        PinHighlighter pinHighlighter = groundPinObj.AddComponent<PinHighlighter>();
        pinHighlighter.pinNumber = pinNumber;
        pinHighlighter.isGroundPin = true;
        groundPin.pinVisual = pinHighlighter;

        groundPins.Add(groundPin);
    }

    public ArduinoGroundPin GetGroundPin(int index = 0)
    {
        if (index >= 0 && index < groundPins.Count)
            return groundPins[index];
        return null;
    }

    void InitializePins()
    {
        for (int i = 0; i < 14; i++)
        {
            digitalPins.Add(new Pin(i));
        }

        for (int i = 0; i < 6; i++)
        {
            analogPins.Add(new Pin(i));
        }
    }

    public bool DigitalWrite(int pin, int value)
    {
        if (pin < 0 || pin >= digitalPins.Count)
        {
            Debug.LogError($"Invalid pin number: {pin}");
            return false;
        }

        Pin targetPin = digitalPins[pin];

        float newVoltage = (value > 0) ? Pin.OPERATING_VOLTAGE : 0f;
        if (Mathf.Abs(targetPin.voltage - newVoltage) < 0.01f)
        {
            Debug.Log($"Pin {pin} already at value {value}, but processing anyway");
        }

        if (!CheckCurrentLimit(pin, value))
        {
            Debug.LogWarning($"Current limit exceeded on pin {pin}, limiting output");
            return false;
        }

        targetPin.voltage = newVoltage;
        OnDigitalWrite?.Invoke(pin, value);

        UpdateConnectedComponents(pin, value);
        return true;
    }

    public int DigitalRead(int pin)
    {
        if (pin < 0 || pin >= digitalPins.Count)
        {
            Debug.LogError($"Invalid pin number: {pin}");
            return 0;
        }

        Pin targetPin = digitalPins[pin];
        return (targetPin.voltage > 2.5f) ? 1 : 0;
    }

    public void SetPinMode(int pin, PinMode mode)
    {
        if (pin < 0 || pin >= digitalPins.Count)
        {
            Debug.LogError($"Invalid pin number: {pin}");
            return;
        }

        digitalPins[pin].mode = mode;
        Debug.Log($"Pin {pin} set to {mode}");
    }

    public bool AnalogWrite(int pin, float value)
    {
        if (pin < 0 || pin >= digitalPins.Count)
        {
            Debug.LogError($"Invalid pin number: {pin}");
            return false;
        }

        Pin targetPin = digitalPins[pin];

        if (!targetPin.isPWM)
        {
            Debug.LogError($"Pin {pin} does not support PWM");
            return false;
        }

        float voltage = Mathf.Clamp(value, 0f, 255f) * Pin.OPERATING_VOLTAGE / 255f;
        targetPin.voltage = voltage;
        OnAnalogWrite?.Invoke(pin, voltage);

        UpdateConnectedComponents(pin, voltage > 0 ? 1 : 0);
        return true;
    }

    public int AnalogRead(int pin)
    {
        if (pin < 0 || pin >= analogPins.Count)
        {
            Debug.LogError($"Invalid analog pin number: {pin}");
            return 0;
        }

        Pin targetPin = analogPins[pin];
        return Mathf.RoundToInt(targetPin.voltage * 1023f / Pin.OPERATING_VOLTAGE);
    }

    private bool CheckCurrentLimit(int pin, int value)
    {
        if (value == 0) return true;

        Pin targetPin = digitalPins[pin];
        float estimatedCurrent = CalculateCurrent(pin);

        if (estimatedCurrent > Pin.MAX_CURRENT)
        {
            Debug.LogWarning($"Current overload on pin {pin}! {estimatedCurrent:F3}A > {Pin.MAX_CURRENT}A");
            if (ArduinoSimulator.Instance != null)
                ArduinoSimulator.Instance.SerialPrint($"WARNING: Current overload on pin {pin}!");
            return false;
        }

        targetPin.current = estimatedCurrent;
        return true;
    }

    private float CalculateCurrent(int pin)
    {
        float totalResistance = 0f;
        int connectedCount = 0;

        if (connections.ContainsKey(pin))
        {
            foreach (var connection in connections[pin])
            {
                CircuitComponent circuitComp = connection.targetComponent as CircuitComponent;
                if (circuitComp != null && circuitComp.resistance > 0)
                {
                    totalResistance += circuitComp.resistance;
                    connectedCount++;
                }
            }
        }

        if (totalResistance == 0f) return 0f;
        return Pin.OPERATING_VOLTAGE / totalResistance;
    }

    private void UpdateConnectedComponents(int pin, int value)
    {
        if (connections.ContainsKey(pin))
        {
            foreach (var connection in connections[pin])
            {
                CircuitComponent circuitComp = connection.targetComponent as CircuitComponent;
                if (circuitComp != null)
                {
                    float voltage = (value > 0) ? Pin.OPERATING_VOLTAGE : 0f;

                    if (!circuitComp.isActive)
                    {
                        Debug.LogWarning($"Component {circuitComp.name} is not active, forcing update");
                        circuitComp.isActive = true;
                    }

                    circuitComp.OnVoltageChanged(voltage);
                }
            }
        }
    }

    public void ConnectComponent(int pin, IConnectable component)
    {
        if (!connections.ContainsKey(pin))
            connections[pin] = new List<WireConnection>();

        WireConnection newConnection = new WireConnection
        {
            sourcePin = pin,
            targetComponent = component
        };

        connections[pin].Add(newConnection);

        if (pin >= 0 && pin < digitalPins.Count)
        {
            digitalPins[pin].isConnected = true;
            digitalPins[pin].connectedComponent = component;
        }

        Debug.Log($"Connected {component.GetName()} to Arduino pin {pin}");
    }

    public void SendSerialData(string data)
    {
        OnSerialData?.Invoke(data);
        if (ArduinoSimulator.Instance != null)
            ArduinoSimulator.Instance.SerialPrint(data);
    }

    [System.Serializable]
    public class WireConnection
    {
        public int sourcePin;
        public IConnectable targetComponent;
    }

    public void DebugPinInfo(int pin)
    {
        if (pin >= 0 && pin < digitalPins.Count)
        {
            var pinInfo = digitalPins[pin];
            Debug.Log($"=== Pin {pin} Info ===");
            Debug.Log($"Mode: {pinInfo.mode}");
            Debug.Log($"Voltage: {pinInfo.voltage}V");
            Debug.Log($"Current: {pinInfo.current}A");
            Debug.Log($"Connected: {pinInfo.isConnected}");
            Debug.Log($"PWM capable: {pinInfo.isPWM}");
        }
    }

    public bool SafeDigitalWriteForLED(int pin, int value)
    {
        if (pin < 0 || pin >= digitalPins.Count)
        {
            Debug.LogError($"Invalid pin number: {pin}");
            return false;
        }

        Pin targetPin = digitalPins[pin];

        if (targetPin.mode != PinMode.Output)
        {
            Debug.LogWarning($"Pin {pin} is not in OUTPUT mode");
            SetPinMode(pin, PinMode.Output);
        }

        float safeVoltage = (value > 0) ? 3.3f : 0f;
        targetPin.voltage = safeVoltage;
        UpdateConnectedComponents(pin, value > 0 ? 1 : 0);

        return true;
    }

    public bool AnalogWriteForLED(int pin, float value)
    {
        if (pin < 0 || pin >= digitalPins.Count)
        {
            Debug.LogError($"Invalid pin number: {pin}");
            return false;
        }

        Pin targetPin = digitalPins[pin];

        if (!targetPin.isPWM)
        {
            Debug.LogError($"Pin {pin} does not support PWM");
            return false;
        }

        float maxLEDVoltage = 3.3f;
        float voltage = Mathf.Clamp(value, 0f, 255f) * maxLEDVoltage / 255f;

        targetPin.voltage = voltage;
        OnAnalogWrite?.Invoke(pin, voltage);

        UpdateConnectedComponents(pin, voltage > 0 ? 1 : 0);
        return true;
    }
}