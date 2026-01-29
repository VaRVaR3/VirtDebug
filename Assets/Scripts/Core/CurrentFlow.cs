using UnityEngine;
using System.Collections.Generic;

public class CurrentFlow : MonoBehaviour
{
    [System.Serializable]
    public class CircuitPath
    {
        public List<CircuitComponent> components = new List<CircuitComponent>();
        public float totalResistance = 0f;
        public float totalCurrent = 0f;
        public bool isClosed = false;
    }

    public static CurrentFlow Instance;

    private List<CircuitPath> circuitPaths = new List<CircuitPath>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    // Добавляем компонент в цепь
    public void AddToCircuit(CircuitComponent component, CircuitComponent previousComponent = null)
    {
        // Находим или создаем путь
        CircuitPath path = FindPathContaining(component);

        if (path == null)
        {
            path = new CircuitPath();
            circuitPaths.Add(path);
        }

        if (!path.components.Contains(component))
        {
            path.components.Add(component);

            // Обновляем общее сопротивление цепи
            path.totalResistance += component.resistance;

            Debug.Log($"Added {component.name} to circuit. Total R: {path.totalResistance}Ω");
        }
    }

    // Удаляем компонент из цепи
    public void RemoveFromCircuit(CircuitComponent component)
    {
        CircuitPath path = FindPathContaining(component);
        if (path != null)
        {
            path.components.Remove(component);
            path.totalResistance -= component.resistance;

            if (path.components.Count == 0)
                circuitPaths.Remove(path);
        }
    }

    // Рассчитываем ток в цепи
    public float CalculateCircuitCurrent(CircuitPath path, float sourceVoltage)
    {
        if (path.totalResistance <= 0) return 0f;

        // I = V / R_total
        float current = sourceVoltage / path.totalResistance;
        path.totalCurrent = current;
        path.isClosed = path.components.Count > 1; // Цепь замкнута если есть хотя бы 2 компонента

        // Распределяем ток по компонентам
        foreach (var component in path.components)
        {
            component.currentCurrent = current;

            // Рассчитываем падение напряжения на каждом компоненте
            float voltageDrop = current * component.resistance;
            component.voltageDrop = voltageDrop;

            Debug.Log($"{component.name}: I={current:F3}A, V_drop={voltageDrop:F2}V");
        }

        return current;
    }

    // Визуализация тока
    public void VisualizeCurrent(CircuitPath path)
    {
        if (path.components.Count < 2) return;

        for (int i = 0; i < path.components.Count - 1; i++)
        {
            CircuitComponent from = path.components[i];
            CircuitComponent to = path.components[i + 1];

            if (from != null && to != null)
            {
                DrawCurrentFlow(from.transform.position, to.transform.position, path.totalCurrent);
            }
        }
    }

    private void DrawCurrentFlow(Vector3 from, Vector3 to, float current)
    {
        // Визуализация линиями (в редакторе)
        Debug.DrawLine(from, to, GetCurrentColor(current), Time.deltaTime);

        // Можно добавить частицы для визуализации тока
        if (current > 0.01f)
        {
            Vector3 direction = (to - from).normalized;
            float distance = Vector3.Distance(from, to);

            // Визуализация "потока электронов"
            for (float t = 0; t < 1; t += 0.1f)
            {
                Vector3 point = Vector3.Lerp(from, to, t);
                Debug.DrawRay(point, direction * 0.1f, Color.yellow, 0.1f);
            }
        }
    }

    private Color GetCurrentColor(float current)
    {
        if (current < 0.01f) return Color.gray;
        if (current < 0.02f) return Color.blue;
        if (current < 0.05f) return Color.green;
        if (current < 0.1f) return Color.yellow;
        return Color.red;
    }

    private CircuitPath FindPathContaining(CircuitComponent component)
    {
        foreach (var path in circuitPaths)
        {
            if (path.components.Contains(component))
                return path;
        }
        return null;
    }
}