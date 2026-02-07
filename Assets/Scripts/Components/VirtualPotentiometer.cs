using UnityEngine;

public class VirtualPotentiometer : MonoBehaviour, IConnectable
{
    [Header("Pins")]
    public int pinLeft = 1;
    public int pinWiper = 2;
    public int pinRight = 3;

    [Header("Value")]
    [Range(0f, 1f)]
    public float value01 = 0.5f;

    public float vRef = 5f;

    [Header("Interaction")]
    public bool allowMouseWheel = true;
    public float wheelSpeed = 0.1f;

    [Header("Visual")]
    public Transform knob;
    public float maxRotation = 270f;

    private IConnectable leftTo;
    private int leftPin;

    private IConnectable wiperTo;
    private int wiperPin;

    private IConnectable rightTo;
    private int rightPin;

    void Awake()
    {
        if (knob == null)
        {
            Transform t = transform.Find("Cylinder");
            if (t == null) t = transform.Find("Knob");
            if (t != null) knob = t;
        }
    }

    // ===== IConnectable =====

    public string GetName() => name;

    public void OnConnected(int pin, IConnectable otherComponent, int otherPin)
    {
        if (pin == pinLeft) { leftTo = otherComponent; leftPin = otherPin; }
        if (pin == pinWiper) { wiperTo = otherComponent; wiperPin = otherPin; }
        if (pin == pinRight) { rightTo = otherComponent; rightPin = otherPin; }

        TryApply();
    }

    public void OnDisconnected(int pin)
    {
        if (pin == pinLeft) leftTo = null;
        if (pin == pinWiper) wiperTo = null;
        if (pin == pinRight) rightTo = null;
    }

    public Vector3 GetPinPosition(int pin)
    {
        var pins = GetComponentsInChildren<UltraSimplePin>();
        foreach (var p in pins)
            if (p.pinNumber == pin)
                return p.transform.position;

        return transform.position;
    }

    public bool CanConnectTo(int pin, IConnectable otherComponent, int otherPin) => true;

    // ===== Update =====

    void Update()
    {
        if (!allowMouseWheel) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.0001f) return;

        var cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if(!Physics.Raycast(ray, out RaycastHit hit, 500f, ~0, QueryTriggerInteraction.Collide))
            return;


        if (!hit.transform.IsChildOf(transform))
            return;

        value01 = Mathf.Clamp01(value01 + scroll * wheelSpeed);

        UpdateKnobVisual();
        TryApply();
    }

    // ===== Visual =====

    private void UpdateKnobVisual()
    {
        if (knob == null) return;

        float angle = Mathf.Lerp(0f, maxRotation, value01);
        knob.localRotation = Quaternion.Euler(0f, angle, 0f);
    }

    // ===== Logic =====

    private void TryApply()
    {
        if (wiperTo == null) return;

        VirtualArduino arduino = wiperTo as VirtualArduino;
        if (arduino == null) return;

        bool powerOK = (leftPin == 200);
        bool groundOK = (rightPin == 100 || rightPin == 101);

        if (!powerOK || !groundOK)
        {
            arduino.SetAnalogVoltage(wiperPin, 0f);
            return;
        }

        if (wiperPin < 300 || wiperPin > 305)
            return;

        float voltage = value01 * vRef;
        arduino.SetAnalogVoltage(wiperPin, voltage);

        Debug.Log($"Pot {name}: A{wiperPin - 300} = {voltage:0.00}V");
    }
}
