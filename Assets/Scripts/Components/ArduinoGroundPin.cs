using UnityEngine;

public class ArduinoGroundPin : MonoBehaviour, IConnectable
{
    [Header("Ground Pin Settings")]
    public int pinNumber = 0; // 0 для GND
    public string pinName = "GND";
    public float groundVoltage = 0f;

    [Header("Visual Reference")]
    public PinHighlighter pinVisual;

    void Start()
    {
        if (pinVisual == null)
            pinVisual = GetComponent<PinHighlighter>();

        if (pinVisual != null)
        {
            pinVisual.isGroundPin = true;
            pinVisual.pinNumber = pinNumber;
            pinVisual.pinMode = PinMode.Ground;
        }

        Debug.Log($"Arduino GND pin initialized as pin {pinNumber}");
    }

    // Реализация IConnectable
    public string GetName()
    {
        return gameObject.name;
    }

    public void OnConnected(int pin, IConnectable otherComponent, int otherPin)
    {
        Debug.Log($"Arduino GND pin {pinNumber} connected to {otherComponent.GetName()} pin {otherPin}");

        MonoBehaviour mono = otherComponent as MonoBehaviour;
        if (mono != null)
        {
            CircuitComponent circuitComp = mono.GetComponent<CircuitComponent>();
            if (circuitComp != null)
            {
                circuitComp.OnVoltageChanged(0f);
                Debug.Log($"GND sent 0V to {circuitComp.name}");
            }
        }
    }

    public void OnDisconnected(int pin)
    {
        Debug.Log($"Arduino GND pin {pinNumber} disconnected");
    }

    public Vector3 GetPinPosition(int pin)
    {
        return transform.position;
    }

    public bool CanConnectTo(int pin, IConnectable otherComponent, int otherPin)
    {
        // GND можно подключить к любому компоненту
        return true;
    }

    public int[] GetAvailablePins()
    {
        return new int[] { pinNumber };
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }
}