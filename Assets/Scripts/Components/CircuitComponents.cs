using System.Collections.Generic;
using UnityEngine;

public class CircuitComponent : MonoBehaviour, IConnectable
{
    [Header("Basic Properties")]
    public bool isActive = true;
    public float resistance = 100f;
    public float maxVoltage = 5.0f;
    public float maxCurrent = 0.1f;
    public bool requiresPolarity = false;
    public bool canBurnOut = false;

    [Header("Connection Info")]
    public int positivePin = -1;
    public int negativePin = -1;
    public VirtualArduino connectedArduino;

    [Header("Current State")]
    public float currentVoltage = 0f;
    public float currentDraw = 0f;
    public float currentCurrent = 0f;
    public float voltageDrop = 0f;

    // ====== NEW: Pin cache ======
    private Dictionary<int, UltraSimplePin> pinMap = new Dictionary<int, UltraSimplePin>();

    protected virtual void Awake()
    {
        BuildPinMap();
    }

    [ContextMenu("Refresh Pins")]
    public void RefreshPins()
    {
        BuildPinMap();
        Debug.Log($"🔄 {name}: Pin map refreshed. Pins found: {pinMap.Count}");
    }

    private void BuildPinMap()
    {
        pinMap.Clear();

        UltraSimplePin[] pins = GetComponentsInChildren<UltraSimplePin>(true);
        foreach (var p in pins)
        {
            if (p == null) continue;

            if (!pinMap.ContainsKey(p.pinNumber))
            {
                pinMap.Add(p.pinNumber, p);
            }
            else
            {
                Debug.LogWarning($"⚠️ {name}: Duplicate pinNumber={p.pinNumber}. Check your pins!");
            }
        }
    }

    // ========== IConnectable ==========

    public virtual string GetName()
    {
        return name;
    }

    public virtual void OnConnected(int pin, IConnectable otherComponent, int otherPin)
    {
        Debug.Log($"CircuitComponent {name}: pin {pin} connected to {otherComponent.GetName()}:{otherPin}");
    }

    public virtual void OnDisconnected(int pin)
    {
        Debug.Log($"CircuitComponent {name}: pin {pin} disconnected");
    }

    public virtual Vector3 GetPinPosition(int pin)
    {
        // NEW: return real pin position
        if (pinMap == null || pinMap.Count == 0)
        {
            BuildPinMap();
        }

        if (pinMap.TryGetValue(pin, out UltraSimplePin foundPin) && foundPin != null)
        {
            return foundPin.transform.position;
        }

        // fallback (чтобы хоть что-то работало)
        Debug.LogWarning($"⚠️ {name}: GetPinPosition({pin}) not found, using component position.");
        return transform.position;
    }

    public virtual bool CanConnectTo(int pin, IConnectable otherComponent, int otherPin)
    {
        return true;
    }

    // ========== Additional ==========

    public virtual void Initialize(VirtualArduino arduino)
    {
        connectedArduino = arduino;
    }

    public virtual void OnVoltageChanged(float voltage)
    {
        currentVoltage = voltage;
    }

    public virtual void UpdateComponent()
    {
        // базовое обновление
    }

    public virtual void ConnectToPins(int posPin, int negPin)
    {
        positivePin = posPin;
        negativePin = negPin;
    }

    public void SetActive(bool active)
    {
        isActive = active;
    }

    public bool IsActive()
    {
        return isActive;
    }

    [ContextMenu("Reset Component")]
    public virtual void ResetComponent()
    {
        isActive = true;
        currentVoltage = 0f;
        currentDraw = 0f;
        currentCurrent = 0f;
        voltageDrop = 0f;
        positivePin = -1;
        negativePin = -1;
    }
}
