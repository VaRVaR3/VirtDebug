using UnityEngine;
using System.Collections.Generic;

public class CurrentVisualizer : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color lowCurrentColor = Color.blue;
    public Color mediumCurrentColor = Color.green;
    public Color highCurrentColor = Color.red;
    public float lineWidth = 0.05f;
    public float arrowSize = 0.2f;

    [Header("References")]
    public SuperSimpleConnectionManager supersimpleconnectionManager;

    private Dictionary<string, LineRenderer> currentLines = new Dictionary<string, LineRenderer>();
    private Transform visualizationParent;

    void Start()
    {
        // Создаем родительский объект для визуализации
        visualizationParent = new GameObject("CurrentVisualization").transform;

        if (supersimpleconnectionManager == null)
            supersimpleconnectionManager = FindObjectOfType<SuperSimpleConnectionManager>();
    }

    void Update()
    {
        if (supersimpleconnectionManager == null) return;

        ClearVisualization();
        VisualizeAllConnections();
    }

    void ClearVisualization()
    {
        // Удаляем старые линии
        foreach (var line in currentLines.Values)
        {
            if (line != null)
                Destroy(line.gameObject);
        }
        currentLines.Clear();
    }

    void VisualizeAllConnections()
    {
        var connections = supersimpleconnectionManager.GetConnections();

        foreach (var connection in connections)
        {
            if (connection.sourceComponent != null && connection.targetComponent != null)
            {
                VisualizeConnection(connection);
            }
        }
    }

    void VisualizeConnection(SuperSimpleConnectionManager.WireConnection connection)
    {
        Vector3 startPos = connection.sourceComponent.GetPinPosition(connection.sourcePin);
        Vector3 endPos = connection.targetComponent.GetPinPosition(connection.targetPin);

        // Определяем "ток" (условно)
        float simulatedCurrent = 0.05f;

        // Создаем или получаем LineRenderer для этого соединения
        string lineKey = $"{connection.sourcePin}_{connection.targetPin}";
        if (!currentLines.ContainsKey(lineKey))
        {
            CreateCurrentLine(lineKey);
        }

        // Визуализируем линией с направлением
        DrawCurrentLine(currentLines[lineKey], startPos, endPos, simulatedCurrent);

        // Рисуем стрелку направления
        DrawDirectionArrow(startPos, endPos, simulatedCurrent);
    }

    void CreateCurrentLine(string key)
    {
        GameObject lineObj = new GameObject($"CurrentLine_{key}");
        lineObj.transform.SetParent(visualizationParent);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.positionCount = 2;
        lr.useWorldSpace = true;

        currentLines[key] = lr;
    }

    void DrawCurrentLine(LineRenderer lr, Vector3 start, Vector3 end, float current)
    {
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        Color lineColor = GetCurrentColor(current);
        lr.startColor = lineColor;
        lr.endColor = lineColor;

        // Пульсация для эффекта движения тока
        float pulse = Mathf.PingPong(Time.time, 0.5f) + 0.5f;
        lr.startWidth = lineWidth * pulse;
        lr.endWidth = lineWidth * pulse;
    }

    void DrawDirectionArrow(Vector3 start, Vector3 end, float current)
    {
        Vector3 direction = (end - start).normalized;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized * 0.1f;
        Vector3 midPoint = Vector3.Lerp(start, end, 0.5f);

        // Рисуем стрелку (треугольник)
        Debug.DrawLine(midPoint, midPoint - direction * arrowSize + perpendicular, GetCurrentColor(current));
        Debug.DrawLine(midPoint, midPoint - direction * arrowSize - perpendicular, GetCurrentColor(current));
        Debug.DrawLine(midPoint - direction * arrowSize + perpendicular,
                      midPoint - direction * arrowSize - perpendicular, GetCurrentColor(current));
    }

    Color GetCurrentColor(float current)
    {
        if (current < 0.01f) return Color.gray;
        if (current < 0.02f) return Color.Lerp(lowCurrentColor, mediumCurrentColor, current * 50);
        return Color.Lerp(mediumCurrentColor, highCurrentColor, (current - 0.02f) * 20);
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = Color.cyan;

        GUI.Label(new Rect(Screen.width - 200, Screen.height - 50, 190, 30),
            $"Current Lines: {currentLines.Count}", style);
    }
}