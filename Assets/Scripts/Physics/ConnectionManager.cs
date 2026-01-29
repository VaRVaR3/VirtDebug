using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance;

    [Header("Connection Settings")]
    public Material wireMaterial;
    public Color wireColor = Color.red;
    public float wireThickness = 0.02f;

    [Header("Error Analysis")]
    public bool enableErrorChecking = true;
    public float checkInterval = 1f;

    [Header("Debug")]
    public bool showConnectionHints = true;

    private List<WireConnection> connections = new List<WireConnection>();
    private IConnectable selectedComponent;
    private int selectedPin;
    public bool isConnecting { get; private set; }
    private GameObject tempWire;
    private float checkTimer = 0f;

    // Статистика ошибок
    private Dictionary<CircuitComponent, List<string>> componentErrors =
        new Dictionary<CircuitComponent, List<string>>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (isConnecting && tempWire != null && selectedComponent != null)
        {
            UpdateTempWire();

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelConnection();
            }
        }

        if (enableErrorChecking)
        {
            checkTimer += Time.deltaTime;
            if (checkTimer >= checkInterval)
            {
                CheckAllConnections();
                checkTimer = 0f;
            }
        }
    }

    public void UpdateAllWires()
    {
        // Обновляем все провода в соединениях
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

    // Также можно добавить метод для обновления конкретного провода
    public void UpdateWireForComponent(IConnectable component)
    {
        var componentConnections = GetConnectionsForComponent(component);
        foreach (var conn in componentConnections)
        {
            if (conn.wireVisual != null)
            {
                WireVisual wire = conn.wireVisual.GetComponent<WireVisual>();
                if (wire != null)
                {
                    wire.ForceUpdateWire();
                }
            }
        }
    }

    void CheckAllConnections()
    {
        componentErrors.Clear();

        foreach (var conn in connections)
        {
            CheckConnection(conn);
        }

        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>();
        foreach (var component in allComponents)
        {
            if (!HasConnections(component))
            {
                AddComponentError(component, "Компонент не подключен к цепи");
            }
        }

        LogErrors();
    }

    void CheckConnection(WireConnection conn)
    {
        MonoBehaviour sourceMono = conn.sourceComponent as MonoBehaviour;
        MonoBehaviour targetMono = conn.targetComponent as MonoBehaviour;
        CircuitComponent sourceComp = sourceMono != null ? sourceMono.GetComponent<CircuitComponent>() : null;
        CircuitComponent targetComp = targetMono != null ? targetMono.GetComponent<CircuitComponent>() : null;

        if (sourceComp != null)
        {
            if (sourceComp is VirtualLED led)
            {
                CheckLEDConnection(led, conn);
            }

            if (sourceComp is VirtualResistor resistor)
            {
                CheckResistorConnection(resistor, conn);
            }

            if (sourceComp is VirtualButton button)
            {
                CheckButtonConnection(button, conn);
            }
        }
    }

    void CheckLEDConnection(VirtualLED led, WireConnection conn)
    {
        bool hasArduinoConnection = false;
        bool hasGroundConnection = false;

        foreach (var connection in GetConnectionsForComponent(led))
        {
            MonoBehaviour mono = connection.targetComponent as MonoBehaviour;
            if (mono != null)
            {
                if (mono.GetComponent<VirtualArduino>() != null)
                    hasArduinoConnection = true;

                if (mono.GetComponent<ArduinoGroundPin>() != null ||
                    mono.gameObject.name.ToUpper().Contains("GND"))
                    hasGroundConnection = true;
            }
        }

        if (!hasArduinoConnection)
        {
            AddComponentError(led, "LED не подключен к пину Arduino");
        }

        if (!hasGroundConnection)
        {
            AddComponentError(led, "LED не подключен к земле (GND)");
        }

        if (hasArduinoConnection && hasGroundConnection)
        {
            if (led.positivePin >= 100 && led.negativePin < 100)
            {
                AddComponentError(led, "LED подключен неправильно! Анод (+) должен быть к пину, катод (-) к GND");
            }

            if (led.resistance < 100f)
            {
                AddComponentWarning(led, "В цепи LED нет резистора или сопротивление слишком мало");
            }
        }
    }

    void CheckResistorConnection(VirtualResistor resistor, WireConnection conn)
    {
        int connectionCount = GetConnectionsForComponent(resistor).Count;

        if (connectionCount < 2)
        {
            AddComponentError(resistor, "Резистор подключен только с одной стороны");
        }

        if (resistor.resistance < 1f)
        {
            AddComponentWarning(resistor, "Сопротивление резистора слишком мало (возможно короткое замыкание)");
        }
    }

    void CheckButtonConnection(VirtualButton button, WireConnection conn)
    {
        if (button.positivePin == -1)
        {
            AddComponentError(button, "Кнопка не подключена к пину Arduino");
        }

        bool hasGround = false;
        foreach (var connection in GetConnectionsForComponent(button))
        {
            MonoBehaviour mono = connection.targetComponent as MonoBehaviour;
            if (mono != null &&
                (mono.GetComponent<ArduinoGroundPin>() != null ||
                 mono.gameObject.name.ToUpper().Contains("GND")))
            {
                hasGround = true;
                break;
            }
        }

        if (!hasGround)
        {
            AddComponentWarning(button, "Кнопка не подключена к земле (GND)");
        }
    }

    void AddComponentError(CircuitComponent component, string message)
    {
        if (!componentErrors.ContainsKey(component))
            componentErrors[component] = new List<string>();

        componentErrors[component].Add($"ОШИБКА: {message}");
    }

    void AddComponentWarning(CircuitComponent component, string message)
    {
        if (!componentErrors.ContainsKey(component))
            componentErrors[component] = new List<string>();

        componentErrors[component].Add($"ВНИМАНИЕ: {message}");
    }

    void LogErrors()
    {
        foreach (var kvp in componentErrors)
        {
            foreach (string error in kvp.Value)
            {
                Debug.LogWarning($"{kvp.Key.name}: {error}");
            }
        }
    }

    bool HasConnections(CircuitComponent component)
    {
        foreach (var conn in connections)
        {
            if (System.Object.ReferenceEquals(conn.sourceComponent, component) ||
                System.Object.ReferenceEquals(conn.targetComponent, component))
                return true;
        }
        return false;
    }

    // Метод IsCircuitComplete для внешнего использования
    public bool IsCircuitComplete(CircuitComponent component)
    {
        // Проверяем, есть ли соединения
        if (!HasConnections(component))
            return false;

        // Для разных типов компонентов разные проверки
        if (component is VirtualLED led)
        {
            return CheckLEDConnectionSimple(led);
        }
        else if (component is VirtualButton button)
        {
            return CheckButtonConnectionSimple(button);
        }

        return true;
    }

    bool CheckLEDConnectionSimple(VirtualLED led)
    {
        bool hasArduino = false;
        bool hasGround = false;

        foreach (var conn in GetConnectionsForComponent(led))
        {
            MonoBehaviour mono = conn.targetComponent as MonoBehaviour;
            if (mono != null)
            {
                if (mono.GetComponent<VirtualArduino>() != null)
                    hasArduino = true;
                if (mono.GetComponent<ArduinoGroundPin>() != null)
                    hasGround = true;
            }
        }

        return hasArduino && hasGround;
    }

    bool CheckButtonConnectionSimple(VirtualButton button)
    {
        return button.positivePin != -1;
    }

    public void ClearAllWires()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Wire") || obj.GetComponent<WireVisual>() != null)
            {
                Destroy(obj);
            }
        }

        connections.Clear();
        componentErrors.Clear();
    }

    private GameObject CreateWire(IConnectable source, int sourcePin, IConnectable target, int targetPin)
    {
        GameObject wire = new GameObject($"Wire_{source.GetName()}_{sourcePin}_to_{target.GetName()}_{targetPin}");

        WireVisual wireVisual = wire.AddComponent<WireVisual>();
        wireVisual.sourceComponent = source;
        wireVisual.sourcePin = sourcePin;
        wireVisual.targetComponent = target;
        wireVisual.targetPin = targetPin;
        wireVisual.wireColor = wireColor;

        return wire;
    }

    private void UpdateTempWire()
    {
        Vector3 startPos = selectedComponent.GetPinPosition(selectedPin);
        Vector3 mousePos = GetMouseWorldPosition();

        if (tempWire == null)
        {
            CreateTempWire(startPos);
        }

        LineRenderer lr = tempWire.GetComponent<LineRenderer>();
        if (lr != null)
        {
            Vector3[] tempPositions = new Vector3[2];
            tempPositions[0] = startPos;
            tempPositions[1] = mousePos;
            lr.SetPositions(tempPositions);
        }
    }

    public void CompleteConnection(IConnectable targetComponent, int targetPin)
    {
        Debug.Log($"CompleteConnection: {selectedComponent.GetName()}:{selectedPin} -> {targetComponent.GetName()}:{targetPin}");

        if (!isConnecting || selectedComponent == null)
        {
            ShowConnectionError("Нет активного подключения для завершения");
            return;
        }

        if (targetComponent == null)
        {
            ShowConnectionError("Целевой компонент не найден");
            CancelConnection();
            return;
        }

        if (System.Object.ReferenceEquals(selectedComponent, targetComponent))
        {
            ShowConnectionError("Нельзя подключить компонент к самому себе");
            CancelConnection();
            return;
        }

        if (!selectedComponent.CanConnectTo(selectedPin, targetComponent, targetPin) ||
            !targetComponent.CanConnectTo(targetPin, selectedComponent, selectedPin))
        {
            ShowConnectionError("Эти компоненты не могут быть соединены");
            CancelConnection();
            return;
        }

        if (tempWire != null)
        {
            Destroy(tempWire);
            tempWire = null;
        }

        GameObject wire = CreateWire(selectedComponent, selectedPin, targetComponent, targetPin);

        WireConnection connection = new WireConnection
        {
            sourceComponent = selectedComponent,
            sourcePin = selectedPin,
            targetComponent = targetComponent,
            targetPin = targetPin,
            wireVisual = wire
        };

        connections.Add(connection);

        selectedComponent.OnConnected(selectedPin, targetComponent, targetPin);
        targetComponent.OnConnected(targetPin, selectedComponent, selectedPin);

        UpdatePinVisualState(selectedComponent, selectedPin, true);
        UpdatePinVisualState(targetComponent, targetPin, true);

        GiveConnectionHint(selectedComponent, targetComponent);
        CleanupTempConnection();

        if (enableErrorChecking)
        {
            CheckConnection(connection);
        }
    }

    void ShowConnectionError(string message)
    {
        Debug.LogError($"Connection Error: {message}");

        if (tempWire != null)
        {
            LineRenderer lr = tempWire.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.material.color = Color.red;
                StartCoroutine(ResetTempWireColor(lr, 0.5f));
            }
        }
    }

    IEnumerator ResetTempWireColor(LineRenderer lr, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (lr != null)
        {
            lr.material.color = Color.yellow;
        }
    }

    void GiveConnectionHint(IConnectable source, IConnectable target)
    {
        if (!showConnectionHints) return;

        MonoBehaviour sourceMono = source as MonoBehaviour;
        MonoBehaviour targetMono = target as MonoBehaviour;
        CircuitComponent sourceComp = sourceMono != null ? sourceMono.GetComponent<CircuitComponent>() : null;
        CircuitComponent targetComp = targetMono != null ? targetMono.GetComponent<CircuitComponent>() : null;

        if (sourceComp is VirtualLED && targetComp is VirtualArduino)
        {
            Debug.Log("Подсказка: Теперь подключите второй вывод LED к GND");
        }
        else if (sourceComp is VirtualButton && targetComp is VirtualArduino)
        {
            Debug.Log("Подсказка: Теперь подключите второй вывод кнопки к GND");
        }
        else if (sourceComp is VirtualResistor)
        {
            Debug.Log("Подсказка: Резистор должен быть подключен последовательно с LED");
        }
    }

    private void CreateTempWire(Vector3 startPosition)
    {
        tempWire = new GameObject("TempWire");

        LineRenderer lr = tempWire.AddComponent<LineRenderer>();
        lr.startWidth = wireThickness;
        lr.endWidth = wireThickness;
        lr.material = wireMaterial != null ? wireMaterial : new Material(Shader.Find("Standard"));
        lr.material.color = Color.yellow;
        lr.positionCount = 2;
        lr.useWorldSpace = true;

        lr.SetPosition(0, startPosition);
        lr.SetPosition(1, startPosition);
    }

    public void StartConnection(IConnectable component, int pin)
    {
        Debug.Log($"StartConnection: {component.GetName()} pin {pin}");

        if (component == null)
        {
            Debug.LogError("Component is null!");
            return;
        }

        if (isConnecting)
        {
            CancelConnection();
        }

        selectedComponent = component;
        selectedPin = pin;
        isConnecting = true;

        CreateTempWire(component.GetPinPosition(pin));
    }

    private void UpdatePinVisualState(IConnectable component, int pin, bool connected)
    {
        MonoBehaviour monoComponent = component as MonoBehaviour;
        if (monoComponent != null)
        {
            PinHighlighter[] pins = monoComponent.GetComponentsInChildren<PinHighlighter>();
            foreach (PinHighlighter pinHighlighter in pins)
            {
                if (pinHighlighter.pinNumber == pin)
                {
                    pinHighlighter.SetConnected(connected);
                }
            }
        }
    }

    public void CancelConnection()
    {
        if (!isConnecting) return;

        CleanupTempConnection();
    }

    private void CleanupTempConnection()
    {
        if (tempWire != null)
        {
            Destroy(tempWire);
            tempWire = null;
        }

        selectedComponent = null;
        selectedPin = -1;
        isConnecting = false;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            return hit.point;
        }

        Plane plane = new Plane(Vector3.up, Vector3.zero);
        float distance;

        if (plane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

    // НОВЫЙ МЕТОД: Получить подключенный компонент
    public IConnectable GetConnectingComponent()
    {
        return selectedComponent;
    }

    // НОВЫЙ МЕТОД: Получить подключенный пин
    public int GetConnectingPin()
    {
        return selectedPin;
    }

    public void DebugState()
    {
        Debug.Log("=== ConnectionManager State ===");
        Debug.Log($"isConnecting: {isConnecting}");
        Debug.Log($"selectedComponent: {selectedComponent?.GetName() ?? "null"}");
        Debug.Log($"selectedPin: {selectedPin}");
        Debug.Log($"Total connections: {connections.Count}");

        for (int i = 0; i < connections.Count; i++)
        {
            var conn = connections[i];
            Debug.Log($"  Connection {i}: {conn.sourceComponent?.GetName() ?? "null"}:{conn.sourcePin} -> {conn.targetComponent?.GetName() ?? "null"}:{conn.targetPin}");
        }
    }

    public void DisconnectAll()
    {
        foreach (var connection in connections)
        {
            connection.sourceComponent?.OnDisconnected(connection.sourcePin);
            connection.targetComponent?.OnDisconnected(connection.targetPin);

            if (connection.wireVisual != null)
                Destroy(connection.wireVisual);
        }

        connections.Clear();
    }

    public List<WireConnection> GetConnections()
    {
        return new List<WireConnection>(connections);
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

    [System.Serializable]
    public class WireConnection
    {
        public IConnectable sourceComponent;
        public int sourcePin;
        public IConnectable targetComponent;
        public int targetPin;
        public GameObject wireVisual;
    }
}