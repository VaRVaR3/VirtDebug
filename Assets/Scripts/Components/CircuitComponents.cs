using UnityEngine;

public abstract class CircuitComponent : MonoBehaviour, IConnectable
{
    [Header("Component Settings")]
    public bool isActive = true;
    public float resistance = 0f;
    public float currentVoltage = 0f;
    public float currentCurrent = 0f;

    [Header("Protection Settings")]
    public bool canBurnOut = true;
    public float maxVoltage = 5f;
    public float maxCurrent = 0.03f;
    public bool requiresPolarity = false;

    public VirtualArduino connectedArduino = null;
    public int positivePin = -1;
    public int negativePin = -1;

    public float voltageDrop = 0f;
    public float powerConsumption = 0f;

    // Реализация IConnectable.GetName()
    public string GetName()
    {
        return name;
    }

    public virtual void OnConnected(int pin, IConnectable otherComponent, int otherPin)
    {
        Debug.Log($"{name} connected at pin {pin} to {otherComponent.GetName()} pin {otherPin}");

        MonoBehaviour mono = otherComponent as MonoBehaviour;
        if (mono != null)
        {
            VirtualArduino arduino = mono.GetComponent<VirtualArduino>();
            if (arduino != null)
            {
                connectedArduino = arduino;
                positivePin = otherPin;
                Debug.Log($"{name} now connected to Arduino pin {positivePin}");
                ConnectToPins(positivePin, negativePin);
            }
            else
            {
                if (pin == 2)
                {
                    negativePin = otherPin;
                }
                else
                {
                    positivePin = otherPin;
                }
            }
        }
    }

    public virtual void OnDisconnected(int pin)
    {
        Debug.Log($"{name} disconnected from pin {pin}");
        if (pin == positivePin) positivePin = -1;
        if (pin == negativePin) negativePin = -1;

        if (connectedArduino != null && (pin == positivePin || pin == negativePin))
        {
            connectedArduino = null;
        }
    }

    public virtual Vector3 GetPinPosition(int pin)
    {
        PinHighlighter[] pins = GetComponentsInChildren<PinHighlighter>();
        foreach (PinHighlighter pinObj in pins)
        {
            if (pinObj != null && pinObj.pinNumber == pin)
            {
                return pinObj.transform.position;
            }
        }

        Debug.LogWarning($"Pin {pin} not found on {name}, returning component position");
        return transform.position;
    }

    public virtual bool CanConnectTo(int pin, IConnectable otherComponent, int otherPin)
    {
        if (requiresPolarity)
        {
            if (pin == 2)
            {
                return true;
            }
        }

        return true;
    }

    public virtual void Initialize(VirtualArduino arduino)
    {
        this.connectedArduino = arduino;
        Debug.Log($"{name} initialized with Arduino");
    }

    public virtual bool ConnectToPins(int positivePin, int negativePin)
    {
        if (requiresPolarity && negativePin >= 0 && negativePin != 1)
        {
            Debug.LogError($"Component {name} requires correct polarity! Negative pin should be GND");
            return false;
        }

        this.positivePin = positivePin;
        this.negativePin = negativePin;

        if (positivePin >= 0 && connectedArduino != null)
            connectedArduino.ConnectComponent(positivePin, this);

        Debug.Log($"{name} connected to pins +:{positivePin}, -:{negativePin}");
        return true;
    }

    public virtual void OnVoltageChanged(float voltage)
    {
        if (!isActive) return;

        currentVoltage = voltage;

        if (resistance > 0)
        {
            currentCurrent = voltage / resistance;

            if (canBurnOut && currentCurrent > maxCurrent)
            {
                Debug.LogWarning($"{name}: Current too high! {currentCurrent:F3}A > {maxCurrent}A");
                currentCurrent = maxCurrent;
            }
        }

        if (canBurnOut && voltage > maxVoltage)
        {
            Debug.LogWarning($"{name}: Voltage too high! {voltage:F2}V > {maxVoltage}V");
        }
    }

    public virtual float CalculateCurrent(float sourceVoltage, float groundVoltage = 0f)
    {
        if (!isActive || resistance <= 0) return 0f;

        float voltageDifference = sourceVoltage - groundVoltage;
        float current = voltageDifference / resistance;

        currentCurrent = current;
        powerConsumption = current * current * resistance;
        voltageDrop = current * resistance;

        return current;
    }

    protected virtual void BurnComponent()
    {
        isActive = false;

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Material burnedMaterial = new Material(Shader.Find("Standard"));
            burnedMaterial.color = Color.black;
            renderer.material = burnedMaterial;
        }

        Debug.LogWarning($"{name} burned out!");
    }

    public abstract void UpdateComponent();
}