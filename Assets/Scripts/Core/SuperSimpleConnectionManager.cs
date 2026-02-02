using System.Collections.Generic;
using UnityEngine;

public class SuperSimpleConnectionManager : MonoBehaviour
{
    public static SuperSimpleConnectionManager Instance;

    [Header("Wire Settings")]
    public Color wireColor = Color.red;
    public float wireThickness = 0.02f;

    [Header("Temp Wire Cursor Surface")]
    [Tooltip("Слой поверхности (плата/стол) для курсора. Если не попали — используем плоскость на высоте пина.")]
    public LayerMask cursorSurfaceMask = ~0;

    [Header("Debug")]
    public bool showLogs = true;

    [System.Serializable]
    public class WireConnection
    {
        public IConnectable sourceComponent;
        public int sourcePin;
        public IConnectable targetComponent;
        public int targetPin;
        public GameObject wireVisual;
    }

    private class ActiveConnectionState
    {
        public IConnectable source;
        public int sourcePin;
        public bool isActive = false;

        public void Reset()
        {
            source = null;
            sourcePin = -1;
            isActive = false;
        }
    }

    private ActiveConnectionState currentState = new ActiveConnectionState();
    private List<WireConnection> connections = new List<WireConnection>();
    private GameObject tempWire;

    public bool isConnecting { get { return currentState.isActive; } }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("✅ SuperSimpleConnectionManager initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (currentState.isActive && currentState.source != null)
        {
            UpdateTempWire();

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelConnection();
            }
        }
    }

    public void StartConnection(IConnectable component, int pin)
    {
        if (showLogs) Debug.Log($"🔗 StartConnection: {component?.GetName() ?? "null"} pin {pin}");

        if (component == null)
        {
            Debug.LogError("❌ Component is null!");
            return;
        }

        if (currentState.isActive)
        {
            CancelConnection();
        }

        currentState.source = component;
        currentState.sourcePin = pin;
        currentState.isActive = true;

        CreateTempWire(component.GetPinPosition(pin));
    }

    public void CompleteConnection(IConnectable targetComponent, int targetPin)
    {
        if (showLogs) Debug.Log($"🔗 CompleteConnection: to {targetComponent?.GetName() ?? "null"} pin {targetPin}");

        if (!currentState.isActive || currentState.source == null)
        {
            Debug.LogError("❌ No active connection to complete!");
            return;
        }

        if (targetComponent == null)
        {
            Debug.LogError("❌ Target component is null!");
            CancelConnection();
            return;
        }

        if (System.Object.ReferenceEquals(currentState.source, targetComponent))
        {
            Debug.LogError("❌ Cannot connect component to itself!");
            CancelConnection();
            return;
        }

        if (tempWire != null)
        {
            Destroy(tempWire);
            tempWire = null;
        }

        GameObject wire = CreateWire(currentState.source, currentState.sourcePin, targetComponent, targetPin);

        WireConnection connection = new WireConnection
        {
            sourceComponent = currentState.source,
            sourcePin = currentState.sourcePin,
            targetComponent = targetComponent,
            targetPin = targetPin,
            wireVisual = wire
        };

        connections.Add(connection);

        currentState.source.OnConnected(currentState.sourcePin, targetComponent, targetPin);
        targetComponent.OnConnected(targetPin, currentState.source, currentState.sourcePin);

        UpdatePinVisualState(currentState.source, currentState.sourcePin, true);
        UpdatePinVisualState(targetComponent, targetPin, true);

        if (showLogs) Debug.Log($"✅ Connection created: {currentState.source.GetName()}:{currentState.sourcePin} -> {targetComponent.GetName()}:{targetPin}");

        currentState.Reset();
    }

    public void CancelConnection()
    {
        if (tempWire != null)
        {
            Destroy(tempWire);
            tempWire = null;
        }

        currentState.Reset();
        if (showLogs) Debug.Log("❌ Connection cancelled");
    }

    public void ClearAllWires()
    {
        foreach (var connection in connections)
        {
            if (connection.wireVisual != null)
            {
                Destroy(connection.wireVisual);
            }
        }
        connections.Clear();

        if (tempWire != null)
        {
            Destroy(tempWire);
            tempWire = null;
        }

        WireVisual[] allWires = FindObjectsOfType<WireVisual>();
        foreach (var wire in allWires)
        {
            if (wire != null && wire.gameObject != null)
            {
                Destroy(wire.gameObject);
            }
        }

        if (showLogs) Debug.Log("✅ All wires cleared");
    }

    public List<WireConnection> GetConnectionsForComponent(IConnectable component)
    {
        List<WireConnection> result = new List<WireConnection>();
        foreach (var conn in connections)
        {
            if (System.Object.ReferenceEquals(conn.sourceComponent, component) ||
                System.Object.ReferenceEquals(conn.targetComponent, component))
            {
                result.Add(conn);
            }
        }
        return result;
    }

    public void DisconnectAll()
    {
        foreach (var connection in connections)
        {
            connection.sourceComponent?.OnDisconnected(connection.sourcePin);
            connection.targetComponent?.OnDisconnected(connection.targetPin);

            if (connection.wireVisual != null)
            {
                Destroy(connection.wireVisual);
            }
        }
        connections.Clear();

        if (showLogs) Debug.Log("✅ All connections disconnected");
    }

    public List<WireConnection> GetConnections()
    {
        return new List<WireConnection>(connections);
    }

    public IConnectable GetConnectingComponent()
    {
        return currentState.source;
    }

    public int GetConnectingPin()
    {
        return currentState.sourcePin;
    }

    public bool IsCircuitComplete(CircuitComponent component)
    {
        if (component == null) return false;
        var componentConnections = GetConnectionsForComponent(component);
        return componentConnections.Count > 0;
    }

    public void UpdateAllWires()
    {
        foreach (var connection in connections)
        {
            if (connection.wireVisual != null)
            {
                WireVisual wire = connection.wireVisual.GetComponent<WireVisual>();
                if (wire != null)
                {
                    wire.ForceUpdateWire();
                }
            }
        }
    }

    public void DebugState()
    {
        Debug.Log("=== ConnectionManager State ===");
        Debug.Log($"isConnecting: {isConnecting}");
        Debug.Log($"Current source: {currentState.source?.GetName() ?? "null"} pin {currentState.sourcePin}");
        Debug.Log($"Total connections: {connections.Count}");

        for (int i = 0; i < connections.Count; i++)
        {
            var conn = connections[i];
            Debug.Log($"  Connection {i}: {conn.sourceComponent?.GetName() ?? "null"}:{conn.sourcePin} -> {conn.targetComponent?.GetName() ?? "null"}:{conn.targetPin}");
        }
    }

    private void UpdateTempWire()
    {
        if (!currentState.isActive || currentState.source == null) return;

        Vector3 startPos = currentState.source.GetPinPosition(currentState.sourcePin);
        Vector3 mousePos = GetMouseWorldPosition(startPos.y);

        if (tempWire == null)
        {
            CreateTempWire(startPos);
        }

        LineRenderer lr = tempWire.GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.SetPosition(0, startPos);
            lr.SetPosition(1, mousePos);
        }
    }

    private void CreateTempWire(Vector3 startPosition)
    {
        tempWire = new GameObject("TempWire");

        LineRenderer lr = tempWire.AddComponent<LineRenderer>();
        ConfigureLineRenderer(lr, wireThickness);

        lr.material = CreateLineMaterial(Color.yellow);

        lr.positionCount = 2;
        lr.useWorldSpace = true;

        lr.SetPosition(0, startPosition);
        lr.SetPosition(1, startPosition);
    }

    private GameObject CreateWire(IConnectable source, int sourcePin, IConnectable target, int targetPin)
    {
        GameObject wire = new GameObject($"Wire_{source.GetName()}_{sourcePin}_to_{target.GetName()}_{targetPin}");

        LineRenderer lr = wire.AddComponent<LineRenderer>();
        ConfigureLineRenderer(lr, wireThickness);
        lr.material = CreateLineMaterial(wireColor);

        WireVisual wireVisual = wire.AddComponent<WireVisual>();
        wireVisual.sourceComponent = source;
        wireVisual.sourcePin = sourcePin;
        wireVisual.targetComponent = target;
        wireVisual.targetPin = targetPin;
        wireVisual.wireColor = wireColor;
        wireVisual.wireThickness = wireThickness;

        return wire;
    }

    private void ConfigureLineRenderer(LineRenderer lr, float thickness)
    {
        lr.startWidth = thickness;
        lr.endWidth = thickness;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.numCapVertices = 6;
        lr.numCornerVertices = 6;
        lr.alignment = LineAlignment.View;
    }

    private Material CreateLineMaterial(Color color)
    {
        Shader shader =
            Shader.Find("Sprites/Default") ??
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Standard");

        if (shader == null)
        {
            Debug.LogError("❌ No suitable shader found for line material!");
            shader = Shader.Find("Hidden/InternalErrorShader");
        }

        Material mat = new Material(shader);
        mat.color = color;

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 0.5f);
        }

        return mat;
    }

    private void UpdatePinVisualState(IConnectable component, int pin, bool connected)
    {
        MonoBehaviour monoComponent = component as MonoBehaviour;
        if (monoComponent == null) return;

        UltraSimplePin[] ultraPins = monoComponent.GetComponentsInChildren<UltraSimplePin>();
        foreach (var pinObj in ultraPins)
        {
            if (pinObj.pinNumber == pin)
            {
                pinObj.SetConnected(connected);
                return;
            }
        }

        // лучше вообще убрать PinHighlighter из проекта,
        // но оставим на всякий случай совместимость
        PinHighlighter[] oldPins = monoComponent.GetComponentsInChildren<PinHighlighter>();
        foreach (var pinObj in oldPins)
        {
            if (pinObj.pinNumber == pin)
            {
                pinObj.SetConnected(connected);
                return;
            }
        }
    }

    private Vector3 GetMouseWorldPosition(float yPlane)
    {
        if (Camera.main == null) return Vector3.zero;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // ✅ NEW: сначала пробуем поверхность (плата/стол)
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, cursorSurfaceMask))
        {
            return hit.point;
        }

        // fallback: плоскость на высоте пина
        Plane plane = new Plane(Vector3.up, new Vector3(0f, yPlane, 0f));
        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }
}
