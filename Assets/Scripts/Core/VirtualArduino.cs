using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualArduino : MonoBehaviour, IConnectable
{
    [Serializable]
    public class Pin
    {
        public int number;
        public PinMode mode = PinMode.Input;
        public float voltage = 0f;
        public float current = 0f;
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

    // ✅ НЕ WireConnection! чтобы не конфликтовать с твоим WireConnection для визуалов
    [Serializable]
    public class ArduinoWireLink
    {
        public int arduinoPin;               // D0..D12 или A0..A5 (300..305)
        public IConnectable targetComponent; // LED/Resistor/Button/etc
        public int targetPin;                // пин на компоненте (LED 1/2)
    }

    [Header("Ground Pins")]
    public List<ArduinoGroundPin> groundPins = new List<ArduinoGroundPin>();
    public bool autoCreateGroundPins = true;

    [Header("Pins data")]
    public List<Pin> digitalPins = new List<Pin>();   // D0..D12
    public List<Pin> analogPins = new List<Pin>();    // A0..A5 хранится как 0..5
    public Dictionary<string, string> variables = new Dictionary<string, string>();

    // ключ: нормализованный pin:
    //  - digital: 0..12
    //  - analog: 300..305
    private Dictionary<int, List<ArduinoWireLink>> links = new Dictionary<int, List<ArduinoWireLink>>();

    public event Action<int, int> OnDigitalWrite;
    public event Action<int, float> OnAnalogWrite;
    public event Action<string> OnSerialData;

    [Header("Quick Test")]
    [SerializeField] private int testPin = 9; // PWM пин: 3/5/6/9/10/11
    private bool testState = false;

    void Start()
    {
        InitializePins();
        if (autoCreateGroundPins) CreateGroundPins();
    }

    // ================== IConnectable ==================

    public string GetName() => name;

    public void OnConnected(int pin, IConnectable otherComponent, int otherPin)
    {
        Debug.Log($"Arduino {name}: pin {pin} connected to {otherComponent.GetName()}:{otherPin}");

        // регистрируем digital и analog
        if (IsDigitalPin(pin) || IsAnalogPinNumber(pin))
        {
            AddLink(pin, otherComponent, otherPin);
        }
    }

    public void OnDisconnected(int pin)
    {
        Debug.Log($"Arduino {name}: pin {pin} disconnected");

        int key = NormalizePinKey(pin);
        links.Remove(key);

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
        foreach (var p in ultraPins)
            if (p != null && p.pinNumber == pin)
                return p.transform.position;

        foreach (var g in groundPins)
            if (g != null && g.pinNumber == pin)
                return g.transform.position;

        return transform.position + new Vector3(0.1f * (pin % 10), 0f, 0.1f * (pin / 10));
    }

    public bool CanConnectTo(int pin, IConnectable otherComponent, int otherPin) => true;

    // ================== Pin helpers ==================

    private bool IsDigitalPin(int pin) => (pin >= 0 && pin < digitalPins.Count);

    private bool IsAnalogPinNumber(int pinNumber)
        => (pinNumber >= 0 && pinNumber <= 5) || (pinNumber >= 300 && pinNumber <= 305);

    private bool TryMapAnalogPinNumber(int pinNumber, out int idx)
    {
        if (pinNumber >= 0 && pinNumber <= 5) { idx = pinNumber; return true; }
        if (pinNumber >= 300 && pinNumber <= 305) { idx = pinNumber - 300; return true; }
        idx = -1; return false;
    }

    private int NormalizePinKey(int pinNumber)
    {
        if (IsDigitalPin(pinNumber)) return pinNumber;
        if (TryMapAnalogPinNumber(pinNumber, out int aIdx)) return 300 + aIdx;
        return pinNumber;
    }

    // ================== Ground creation ==================

    void CreateGroundPins()
    {
        if (groundPins.Count != 0) return;

        CreateGroundPin(100, "GND1", new Vector3(-0.5f, 0.1f, 0));
        CreateGroundPin(101, "GND2", new Vector3(-0.5f, -0.1f, 0));
    }

    void CreateGroundPin(int pinNumber, string pinName, Vector3 localPosition)
    {
        GameObject go = new GameObject($"Arduino_GND_{pinNumber}");
        go.transform.SetParent(transform);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;

        ArduinoGroundPin gp = go.AddComponent<ArduinoGroundPin>();
        gp.pinNumber = pinNumber;
        gp.pinName = pinName;

        groundPins.Add(gp);
    }

    // ================== Initialize pins ==================

    void InitializePins()
    {
        digitalPins.Clear();
        analogPins.Clear();

        for (int i = 0; i <= 12; i++)
            digitalPins.Add(new Pin(i));

        for (int i = 0; i < 6; i++)
            analogPins.Add(new Pin(i));
    }

    // ================== Digital ==================

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

    public bool DigitalWrite(int pin, int value)
    {
        if (!IsDigitalPin(pin))
        {
            Debug.LogError($"Invalid digital pin number: {pin}");
            return false;
        }

        float voltage = (value > 0) ? Pin.OPERATING_VOLTAGE : 0f;

        if (!CheckCurrentLimit(pin, value))
        {
            Debug.LogWarning($"Current limit exceeded on pin {pin}, limiting output");
            return false;
        }

        digitalPins[pin].voltage = voltage;

        OnDigitalWrite?.Invoke(pin, value);

        // ✅ передаем конкретное напряжение
        PropagateVoltage(pin, voltage);
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

    // ================== PWM (AnalogWrite) ==================
    // value: 0..255 -> напряжение 0..5V (упрощенная модель среднего)
    public bool AnalogWrite(int pin, float value)
    {
        if (!IsDigitalPin(pin))
        {
            Debug.LogError($"Invalid digital pin number: {pin}");
            return false;
        }

        var p = digitalPins[pin];
        if (!p.isPWM)
        {
            Debug.LogError($"Pin {pin} does not support PWM");
            return false;
        }

        float voltage = Mathf.Clamp(value, 0f, 255f) * Pin.OPERATING_VOLTAGE / 255f;

        p.voltage = voltage;
        OnAnalogWrite?.Invoke(pin, voltage);

        // ✅ КЛЮЧ: распространяем именно voltage, а не 0/5
        PropagateVoltage(pin, voltage);
        return true;
    }

    // ================== Analog In ==================

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
        int idx = analogPinNumber;

        if (analogPinNumber >= 300 && analogPinNumber <= 305)
            idx = analogPinNumber - 300;

        if (idx < 0 || idx >= analogPins.Count)
        {
            Debug.LogError($"Invalid analog pin: {analogPinNumber}");
            return;
        }

        analogPins[idx].voltage = Mathf.Clamp(voltage, 0f, Pin.OPERATING_VOLTAGE);
    }

    // ================== Links / Propagate ==================

    private void AddLink(int arduinoPin, IConnectable target, int targetPin)
    {
        int key = NormalizePinKey(arduinoPin);

        if (!links.ContainsKey(key))
            links[key] = new List<ArduinoWireLink>();

        links[key].Add(new ArduinoWireLink
        {
            arduinoPin = arduinoPin,
            targetComponent = target,
            targetPin = targetPin
        });

        if (IsDigitalPin(arduinoPin))
        {
            digitalPins[arduinoPin].isConnected = true;
            digitalPins[arduinoPin].connectedComponent = target;
        }

        Debug.Log($"Link created: Arduino {arduinoPin} -> {target.GetName()}:{targetPin}");
    }

    private void PropagateVoltage(int arduinoPin, float voltage)
    {
        int key = NormalizePinKey(arduinoPin);
        if (!links.TryGetValue(key, out var list)) return;

        foreach (var l in list)
        {
            if (l.targetComponent == null) continue;

            // ✅ Спец-обработка LED: нужно знать на какой pin LED подали напряжение
            if (l.targetComponent is VirtualLED led)
            {
                led.OnArduinoVoltage(l.targetPin, voltage);
                continue;
            }

            // остальные компоненты
            if (l.targetComponent is CircuitComponent cc)
            {
                if (!cc.isActive) cc.isActive = true;
                cc.OnVoltageChanged(voltage);
            }
        }
    }

    // ================== Current limit ==================

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
        // пока упрощенно: суммируем resistance у CircuitComponent на этом пине
        float totalR = 0f;

        int key = NormalizePinKey(pin);
        if (links.TryGetValue(key, out var list))
        {
            foreach (var l in list)
            {
                if (l.targetComponent is CircuitComponent cc)
                {
                    if (cc.resistance > 0) totalR += cc.resistance;
                }
            }
        }

        if (totalR <= 0.0001f) return 0f;
        return Pin.OPERATING_VOLTAGE / totalR;
    }

    // ================== Serial ==================

    public void SendSerialData(string data)
    {
        OnSerialData?.Invoke(data);
        ArduinoSimulator.Instance?.SerialPrint(data);
    }

    // ================== Debug ==================

    public void DebugPinInfo(int pin)
    {
        if (IsDigitalPin(pin))
        {
            var p = digitalPins[pin];
            Debug.Log($"D{pin}: mode={p.mode}, V={p.voltage:0.00}V, PWM={p.isPWM}");
            return;
        }

        if (TryMapAnalogPinNumber(pin, out int idx))
        {
            Debug.Log($"A{idx}: V={analogPins[idx].voltage:0.00}V (pin={pin})");
            return;
        }

        Debug.LogWarning($"Unknown pin: {pin}");
    }

    // ================== Quick test ==================

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SetPinMode(testPin, PinMode.Output);
            testState = !testState;
            DigitalWrite(testPin, testState ? 1 : 0);
            Debug.Log($"TEST: D{testPin} => {(testState ? "HIGH" : "LOW")}");
        }

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

    public float GetAnalogVoltage(int analogPinNumber)
    {
        int idx = analogPinNumber;

        // поддержка A0..A5 = 300..305
        if (analogPinNumber >= 300 && analogPinNumber <= 305)
            idx = analogPinNumber - 300;

        if (idx < 0 || idx >= analogPins.Count)
            return 0f;

        return analogPins[idx].voltage;
    }

    public ArduinoGroundPin GetGroundPin(int index = 0)
    {
        if (index >= 0 && index < groundPins.Count)
            return groundPins[index];
        return null;
    }


}
