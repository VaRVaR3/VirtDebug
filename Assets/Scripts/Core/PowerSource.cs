using UnityEngine;
using System.Collections.Generic;

public class PowerSource : MonoBehaviour
{
    [Header("Power Settings")]
    public float voltage = 5f; // Напряжение источника
    public float maxCurrent = 1f; // Максимальный ток
    public bool isOn = false;

    [Header("Connections")]
    public VirtualArduino arduino;
    public VirtualGround ground;
    public List<CircuitComponent> connectedComponents = new List<CircuitComponent>();

    private float totalCurrentDraw = 0f;

    void Update()
    {
        if (isOn)
        {
            CalculateTotalCurrent();
            UpdateConnectedComponents();
        }
    }

    void CalculateTotalCurrent()
    {
        totalCurrentDraw = 0f;

        foreach (var component in connectedComponents)
        {
            if (component.isActive)
            {
                // Для каждого компонента рассчитываем ток по закону Ома
                float componentCurrent = CalculateCurrentForComponent(component);

                totalCurrentDraw += componentCurrent;

                // Проверка перегрузки
                if (totalCurrentDraw > maxCurrent)
                {
                    Debug.LogError($"Power overload! {totalCurrentDraw:F2}A > {maxCurrent}A");
                    Shutdown();
                    return;
                }
            }
        }

        Debug.Log($"Total current draw: {totalCurrentDraw:F3}A");
    }

    float CalculateCurrentForComponent(CircuitComponent component)
    {
        if (component.resistance <= 0)
        {
            Debug.LogWarning($"Component {component.name} has invalid resistance: {component.resistance}");
            return 0f;
        }

        // Расчет тока по закону Ома: I = V / R
        float effectiveVoltage = voltage;
        if (ground != null)
        {
            effectiveVoltage = voltage - ground.voltage;
        }

        float current = effectiveVoltage / component.resistance;

        // Ограничение по максимальному току компонента
        if (current > component.maxCurrent)
        {
            current = component.maxCurrent;
            Debug.LogWarning($"Component {component.name} limited to max current: {component.maxCurrent}A");
        }

        // Сохраняем расчетные значения в компоненте
        component.currentCurrent = current;
        component.voltageDrop = effectiveVoltage;

        return current;
    }

    void UpdateConnectedComponents()
    {
        foreach (var component in connectedComponents)
        {
            if (component.isActive)
            {
                // Обновляем напряжение на компоненте
                component.OnVoltageChanged(voltage);

                // Для LED специальная обработка
                if (component is VirtualLED led)
                {
                    led.UpdateWithCurrentFlow(voltage, ground != null ? ground.voltage : 0f);
                }
            }
        }
    }

    public void ConnectComponent(CircuitComponent component)
    {
        if (!connectedComponents.Contains(component))
        {
            connectedComponents.Add(component);

            // Добавляем в систему тока
            if (CurrentFlow.Instance != null)
                CurrentFlow.Instance.AddToCircuit(component);

            Debug.Log($"Connected {component.name} to power source");
        }
    }

    public void DisconnectComponent(CircuitComponent component)
    {
        if (connectedComponents.Contains(component))
        {
            connectedComponents.Remove(component);

            if (CurrentFlow.Instance != null)
                CurrentFlow.Instance.RemoveFromCircuit(component);

            Debug.Log($"Disconnected {component.name} from power source");
        }
    }

    public void TurnOn()
    {
        isOn = true;
        Debug.Log("Power source ON");
    }

    public void TurnOff()
    {
        isOn = false;
        foreach (var component in connectedComponents)
        {
            component.OnVoltageChanged(0f);
        }
        Debug.Log("Power source OFF");
    }

    void Shutdown()
    {
        Debug.LogWarning("Power source shutting down due to overload");
        TurnOff();
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 16;

        GUI.Label(new Rect(Screen.width - 200, 10, 190, 30),
            $"Power: {(isOn ? "ON" : "OFF")}", style);
        GUI.Label(new Rect(Screen.width - 200, 40, 190, 30),
            $"Voltage: {voltage}V", style);
        GUI.Label(new Rect(Screen.width - 200, 70, 190, 30),
            $"Current: {totalCurrentDraw:F3}A", style);
        GUI.Label(new Rect(Screen.width - 200, 100, 190, 30),
            $"Components: {connectedComponents.Count}", style);
    }
}