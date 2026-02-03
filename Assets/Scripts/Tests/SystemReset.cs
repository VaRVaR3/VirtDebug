using UnityEngine;

public class SystemReset : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) ResetEverything();
        if (Input.GetKeyDown(KeyCode.T)) TestBasicRaycast();
        if (Input.GetKeyDown(KeyCode.Y)) CreateTestEnvironment();
    }

    void ResetEverything()
    {
        Debug.Log("=== RESET EVERYTHING ===");

        // Удаляем провода
        WireVisual[] wires = FindObjectsOfType<WireVisual>();
        foreach (var w in wires) Destroy(w.gameObject);
        Debug.Log($"Удалено WireVisual: {wires.Length}");

        // Сбрасываем ConnectionManager
        if (SuperSimpleConnectionManager.Instance != null)
        {
            SuperSimpleConnectionManager.Instance.DisconnectAll();
            SuperSimpleConnectionManager.Instance.ClearAllWires();
            Debug.Log("✅ ConnectionManager очищен");
        }

        // Сбрасываем LED
        foreach (var led in FindObjectsOfType<VirtualLED>())
        {
            led.positivePin = -1;
            led.negativePin = -1;
            led.connectedArduino = null;
            led.isActive = true;
        }

        // Сбрасываем Arduino pin voltages
        foreach (var arduino in FindObjectsOfType<VirtualArduino>())
        {
            for (int i = 0; i < arduino.digitalPins.Count; i++)
            {
                arduino.digitalPins[i].isConnected = false;
                arduino.digitalPins[i].connectedComponent = null;
                arduino.digitalPins[i].voltage = 0f;
                arduino.digitalPins[i].current = 0f;
            }
        }

        Debug.Log("=== RESET DONE ===");
    }

    void TestBasicRaycast()
    {
        if (Camera.main == null)
        {
            Debug.LogError("Camera.main == null");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 50, Color.red, 1f);

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Collide))
        {
            Debug.Log($"Mouse hit: {hit.collider.name} layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}");

            var pin = hit.collider.GetComponentInParent<UltraSimplePin>();
            var drag = hit.collider.GetComponentInParent<ComponentDragger>();

            Debug.Log($"UltraSimplePin? {(pin != null)} | ComponentDragger? {(drag != null)}");
            if (pin != null) Debug.Log($"PinNumber={pin.pinNumber}");
        }
        else
        {
            Debug.Log("Mouse hit: nothing");
        }
    }

    void CreateTestEnvironment()
    {
        Debug.Log("=== CREATE TEST ENV ===");

        // Test cube for drag
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "TestCube";
        cube.transform.position = new Vector3(0, 0.5f, 0);
        cube.AddComponent<ComponentDragger>(); // метка
        Debug.Log("✅ TestCube создан");

        // Test pin (кликабельный)
        GameObject pinObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pinObj.name = "TestPin";
        pinObj.transform.position = new Vector3(2, 0.5f, 0);
        var usp = pinObj.AddComponent<UltraSimplePin>();
        usp.pinNumber = 99;
        pinObj.AddComponent<TestConnectable>(); // parent IConnectable — на этом же объекте (ok)

        Debug.Log("✅ TestPin создан");

        if (SuperSimpleConnectionManager.Instance == null)
        {
            GameObject cm = new GameObject("ConnectionManager");
            cm.AddComponent<SuperSimpleConnectionManager>();
            Debug.Log("✅ Создан ConnectionManager");
        }

        if (FindObjectOfType<InteractionManager>() == null)
        {
            GameObject im = new GameObject("InteractionManager");
            im.AddComponent<InteractionManager>();
            Debug.Log("✅ Создан InteractionManager");
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle { fontSize = 18 };
        style.normal.textColor = Color.white;

        int y = 10;
        GUI.Label(new Rect(10, y, 600, 28), "R - Reset everything", style); y += 24;
        GUI.Label(new Rect(10, y, 600, 28), "T - Test mouse raycast", style); y += 24;
        GUI.Label(new Rect(10, y, 600, 28), "Y - Create test objects", style); y += 24;
    }
}

// Тестовый IConnectable
public class TestConnectable : MonoBehaviour, IConnectable
{
    public string GetName() => name;
    public void OnConnected(int pin, IConnectable otherComponent, int otherPin) => Debug.Log($"TestConnectable: {pin} -> {otherComponent.GetName()}:{otherPin}");
    public void OnDisconnected(int pin) => Debug.Log($"TestConnectable: disconnected {pin}");
    public Vector3 GetPinPosition(int pin) => transform.position;
    public bool CanConnectTo(int pin, IConnectable otherComponent, int otherPin) => true;
}
