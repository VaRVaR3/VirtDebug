using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

            // PWM пины Arduino Uno (D3, D5, D6, D9, D10, D11)
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

    [Header("Pins data")]
    public List<Pin> digitalPins = new List<Pin>();   // D0..D12
    public List<Pin> analogPins = new List<Pin>();    // A0..A5 храним как 0..5 (внутри)
    public Dictionary<string, string> variables = new Dictionary<string, string>();

    // Соединения:
    // ключ: нормализованный pin id:
    //  - digital: 0..12
    //  - analog: 300..305 (A0..A5)
    private Dictionary<int, List<WireConnection>> connections = new Dictionary<int, List<WireConnection>>();

    public event Action<int, int> OnDigitalWrite;
    public event Action<int, float> OnAnalogWrite;
    public event Action<string> OnSerialData;

    // ===== QUICK TEST =====
    [Header("Quick Test")]
    [SerializeField] private int testPin = 9; // лучше PWM пин (3/5/6/9/10/11)
    private bool testState = false;

    void Start()
    {
        InitializePins();

        if (autoCreateGroundPins)
            CreateGroundPins();
    }

    // ================== IConnectable ==================

    public string GetName() => name;

    public void OnConnected(int pin, IConnectable otherComponent, int otherPin)
    {
        Debug.Log($"Arduino {name}: pin {pin} connected to {otherComponent.GetName()}:{otherPin}");

        // Регистрируем соединение:
        // - digital 0..12
        // - analog 300..305 (A0..A5)
        if (IsDigitalPin(pin) || IsAnalogPinNumber(pin))
        {
            ConnectComponent(pin, otherComponent);
        }
    }

    public void OnDisconnected(int pin)
    {
        Debug.Log($"Arduino {name}: pin {pin} disconnected");

        int key = NormalizePinKey(pin);
        if (connections.ContainsKey(key))
            connections.Remove(key);

        if (IsDigitalPin(pin))
        {
            digitalPins[pin].isConnected = false;
            digitalPins[pin].connectedComponent = null;
            digitalPins[pin].voltage = 0f;
        }
    }

    public Vector3 GetPinPosition(int pin)
    {
        UltraSimplePin[] ultraPins = GetComponentsInChildren<UltraSimplePin>(true);
        foreach (var pinObj in ultraPins)
            if (pinObj.pinNumber == pin)
                return pinObj.transform.position;

        PinHighlighter[] oldPins = GetComponentsInChildren<PinHighlighter>(true);
        foreach (var pinObj in oldPins)
            if (pinObj.pinNumber == pin)
                return pinObj.transform.position;

        foreach (var groundPin in groundPins)
            if (groundPin.pinNumber == pin)
                return groundPin.transform.position;

        return transform.position + new Vector3(0.1f * (pin % 10), 0f, 0.1f * (pin / 10));
    }

    public bool CanConnectTo(int pin, IConnectable otherComponent, int otherPin) => true;

    // ================== Pin helpers ==================

    private bool IsDigitalPin(int pin) => (pin >= 0 && pin < digitalPins.Count);

    private bool IsAnalogPinNumber(int pinNumber)
    {
        // поддерживаем и 0..5 (если кто-то так вызвал), и 300..305 (визуальные A0..A5)
        return (pinNumber >= 0 && pinNumber <= 5) || (pinNumber >= 300 && pinNumber <= 305);
    }

    private bool TryMapAnalogPinNumber(int pinNumber, out int analogIndex)
    {
        if (pinNumber >= 0 && pinNumber <= 5)
        {
            analogIndex = pinNumber;
            return true;
        }

        if (pinNumber >= 300 && pinNumber <= 305)
        {
            analogIndex = pinNumber - 300;
            return true;
        }

        analogIndex = -1;
        return false;
    }

    // Ключ соединений:
    // digital: 0..12
    // analog: 300..305
    private int NormalizePinKey(int pinNumber)
    {
        if (IsDigitalPin(pinNumber))
            return pinNumber;

        if (TryMapAnalogPinNumber(pinNumber, out int aIdx))
            return 300 + aIdx;

        return pinNumber;
    }

    // ================== Ground creation ==================

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

        // Если хочешь полностью убрать PinHighlighter — удали это и используй UltraSimplePin
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

    // ================== Initialize pins ==================

    void InitializePins()
    {
        digitalPins.Clear();
        analogPins.Clear();

        // D0..D12
        for (int i = 0; i <= 12; i++)
            digitalPins.Add(new Pin(i));

        // A0..A5 (внутри 0..5)
        for (int i = 0; i < 6; i++)
            analogPins.Add(new Pin(i));
    }

    // ================== Digital ==================

    public bool DigitalWrite(int pin, int value)
    {
        if (!IsDigitalPin(pin))
        {
            Debug.LogError($"Invalid digital pin number: {pin}");
            return false;
        }

        float newVoltage = (value > 0) ? Pin.OPERATING_VOLTAGE : 0f;

        if (!CheckCurrentLimit(pin, value))
        {
            Debug.LogWarning($"Current limit exceeded on pin {pin}, limiting output");
            return false;
        }

        Pin targetPin = digitalPins[pin];
        targetPin.voltage = newVoltage;

        OnDigitalWrite?.Invoke(pin, value);

        // ✅ важно: передаем именно 0/5V
        UpdateConnectedComponentsVoltage(pin, newVoltage);
        return true;
    }

    public int DigitalRead(int pin)
    {
        if (!IsDigitalPin(pin))
        {
            Debug.LogError($"Invalid digital pin number: {pin}");
            return 0;
        }

        return (digitalPins[pin].voltage > 2.5f) ? 1 : 0;
    }

    public void SetPinMode(int pin, PinMode mode)
    {
        if (!IsDigitalPin(pin))
        {
            Debug.LogError($"Invalid digital pin number: {pin}");
            return;
        }

        digitalPins[pin].mode = mode;
        Debug.Log($"Pin {pin} set to {mode}");
    }

    // analogWrite() в Arduino — это PWM по цифровым пинам
    // value: 0..255
    public bool AnalogWrite(int pin, float value)
    {
        if (!IsDigitalPin(pin))
        {
            Debug.LogError($"Invalid digital pin number: {pin}");
            return false;
        }

        Pin targetPin = digitalPins[pin];

        if (!targetPin.isPWM)
        {
            Debug.LogError($"Pin {pin} does not support PWM");
            return false;
        }

        // PWM -> "среднее" напряжение 0..5V (упрощенная модель)
        float voltage = Mathf.Clamp(value, 0f, 255f) * Pin.OPERATING_VOLTAGE / 255f;

        targetPin.voltage = voltage;

        OnAnalogWrite?.Invoke(pin, voltage);

        // ✅ КЛЮЧЕВО: передаем реальное напряжение, а не 0/5
        UpdateConnectedComponentsVoltage(pin, voltage);
        return true;
    }

    // ================== Analog ==================

    public int AnalogRead(int pinNumber)
    {
        if (!TryMapAnalogPinNumber(pinNumber, out int idx))
        {
            Debug.LogError($"Invalid analog pin number: {pinNumber}");
            return 0;
        }

        float v = analogPins[idx].voltage;
        return Mathf.RoundToInt(v * 1023f / Pin.OPERATING_VOLTAGE);
    }

    public void SetAnalogVoltage(int analogPinNumber, float voltage)
    {
        int index = analogPinNumber;

        if (analogPinNumber >= 300 && analogPinNumber <= 305)
            index = analogPinNumber - 300;

        if (index < 0 || index >= analogPins.Count)
        {
            Debug.LogError($"Invalid analog pin: {analogPinNumber}");
            return;
        }

        analogPins[index].voltage = Mathf.Clamp(voltage, 0f, Pin.OPERATING_VOLTAGE);
    }

    public float GetAnalogVoltage(int analogPinNumber)
    {
        int index = analogPinNumber;

        if (analogPinNumber >= 300 && analogPinNumber <= 305)
            index = analogPinNumber - 300;

        if (index < 0 || index >= analogPins.Count)
            return 0f;

        return analogPins[index].voltage;
    }

    // ================== Current limit / resistance ==================

    private bool CheckCurrentLimit(int pin, int value)
    {
        if (value == 0) return true;

        float estimatedCurrent = CalculateCurrent(pin);

        if (estimatedCurrent > Pin.MAX_CURRENT)
        {
            Debug.LogWarning($"Current overload on pin {pin}! {estimatedCurrent:F3}A > {Pin.MAX_CURRENT}A");
            ArduinoSimulator.Instance?.SerialPrint($"WARNING: Current overload on pin {pin}!");
            return false;
        }

        digitalPins[pin].current = estimatedCurrent;
        return true;
    }

    private float CalculateCurrent(int pin)
    {
        float totalResistance = 0f;

        int key = NormalizePinKey(pin);
        if (connections.ContainsKey(key))
        {
            foreach (var connection in connections[key])
            {
                CircuitComponent circuitComp = connection.targetComponent as CircuitComponent;
                if (circuitComp != null && circuitComp.resistance > 0)
                    totalResistance += circuitComp.resistance;
            }
        }

        if (totalResistance <= 0.0001f) return 0f;
        return Pin.OPERATING_VOLTAGE / totalResistance;
    }

    // ================== Update connected components ==================

    // ✅ Новый метод: обновляем компоненты реальным напряжением
    private void UpdateConnectedComponentsVoltage(int pin, float voltage)
    {
        int key = NormalizePinKey(pin);

        if (!connections.ContainsKey(key)) return;

        foreach (var connection in connections[key])
        {
            CircuitComponent circuitComp = connection.targetComponent as CircuitComponent;
            if (circuitComp == null) continue;

            if (!circuitComp.isActive)
            {
                Debug.LogWarning($"Component {circuitComp.name} is not active, forcing update");
                circuitComp.isActive = true;
            }

            circuitComp.OnVoltageChanged(voltage);
        }
    }

    // (Опционально) Старый стиль: если где-то в будущем пригодится
    private void UpdateConnectedComponentsDigital(int pin, int value)
    {
        float voltage = (value > 0) ? Pin.OPERATING_VOLTAGE : 0f;
        UpdateConnectedComponentsVoltage(pin, voltage);
    }

    public void ConnectComponent(int pin, IConnectable component)
    {
        int key = NormalizePinKey(pin);

        if (!connections.ContainsKey(key))
            connections[key] = new List<WireConnection>();

        WireConnection newConnection = new WireConnection
        {
            sourcePin = pin,
            targetComponent = component
        };

        connections[key].Add(newConnection);

        if (IsDigitalPin(pin))
        {
            digitalPins[pin].isConnected = true;
            digitalPins[pin].connectedComponent = component;
        }

        Debug.Log($"Connected {component.GetName()} to Arduino pin {pin} (key={key})");
    }

    public void SendSerialData(string data)
    {
        OnSerialData?.Invoke(data);
        ArduinoSimulator.Instance?.SerialPrint(data);
    }

    [System.Serializable]
    public class WireConnection
    {
        public int sourcePin;
        public IConnectable targetComponent;
    }

    public void DebugPinInfo(int pin)
    {
        if (IsDigitalPin(pin))
        {
            var p = digitalPins[pin];
            Debug.Log($"=== Digital Pin D{pin} === mode={p.mode}, V={p.voltage:0.00}V, I={p.current:0.000}A, PWM={p.isPWM}");
            return;
        }

        if (TryMapAnalogPinNumber(pin, out int idx))
        {
            var a = analogPins[idx];
            Debug.Log($"=== Analog Pin A{idx} (pin={pin}) === V={a.voltage:0.00}V");
            return;
        }

        Debug.LogWarning($"Unknown pin: {pin}");
    }

    // ================== LED helper (не обязательно, но пусть останется) ==================

    public bool SafeDigitalWriteForLED(int pin, int value)
    {
        if (!IsDigitalPin(pin))
        {
            Debug.LogError($"Invalid digital pin number: {pin}");
            return false;
        }

        if (digitalPins[pin].mode != PinMode.Output)
            SetPinMode(pin, PinMode.Output);

        float safeVoltage = (value > 0) ? 3.3f : 0f;

        digitalPins[pin].voltage = safeVoltage;
        UpdateConnectedComponentsVoltage(pin, safeVoltage);

        return true;
    }

    // ================== Quick test keys ==================

    void Update()
    {
        // F5 — toggle D(testPin)
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SetPinMode(testPin, PinMode.Output);
            testState = !testState;
            DigitalWrite(testPin, testState ? 1 : 0);
            Debug.Log($"TEST: D{testPin} => {(testState ? "HIGH" : "LOW")}");
        }

        // F6 — плавно прогнать PWM (наглядно)
        if (Input.GetKeyDown(KeyCode.F6))
        {
            StartCoroutine(TestPwmSweep());
        }
    }

    private IEnumerator TestPwmSweep()
    {
        if (!IsDigitalPin(testPin) || !digitalPins[testPin].isPWM)
        {
            Debug.LogWarning($"TEST PWM: pin {testPin} is not PWM. Use 3,5,6,9,10,11");
            yield break;
        }

        SetPinMode(testPin, PinMode.Output);

        for (int v = 0; v <= 255; v += 10)
        {
            AnalogWrite(testPin, v);
            yield return new WaitForSeconds(0.05f);
        }

        for (int v = 255; v >= 0; v -= 10)
        {
            AnalogWrite(testPin, v);
            yield return new WaitForSeconds(0.05f);
        }

        AnalogWrite(testPin, 0);
        Debug.Log("TEST PWM: sweep done");
    }
}
