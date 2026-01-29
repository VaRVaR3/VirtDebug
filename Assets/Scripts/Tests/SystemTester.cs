using UnityEngine;
using UnityEngine.EventSystems;

public class SystemTester : MonoBehaviour
{
    void Start()
    {
        Invoke("TestAllSystems", 1f);
    }

    void TestAllSystems()
    {
        Debug.Log("=== SYSTEM TEST START ===");

        // 1. Проверка ConnectionManager
        if (ConnectionManager.Instance == null)
        {
            Debug.LogError("? ConnectionManager.Instance is NULL!");
        }
        else
        {
            Debug.Log("? ConnectionManager.Instance exists");
            ConnectionManager.Instance.DebugState();
        }

        // 2. Проверка EventSystem
        if (EventSystem.current == null)
        {
            Debug.LogError("? EventSystem.current is NULL!");
        }
        else
        {
            Debug.Log("? EventSystem.current exists");
        }

        // 3. Проверка камеры
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("? Main Camera not found!");
        }
        else
        {
            Debug.Log("? Main Camera found");

            // Проверка Physics Raycaster
            var raycaster = cam.GetComponent<PhysicsRaycaster>();
            if (raycaster == null)
            {
                Debug.LogError("? PhysicsRaycaster not found on Main Camera!");
            }
            else
            {
                Debug.Log("? PhysicsRaycaster found on Main Camera");
            }
        }

        // 4. Проверка компонентов
        TestComponents();

        Debug.Log("=== SYSTEM TEST COMPLETE ===");
    }

    void TestComponents()
    {
        // Найти все компоненты с CircuitComponent
        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>();
        Debug.Log($"Found {allComponents.Length} CircuitComponents");

        foreach (var comp in allComponents)
        {
            Debug.Log($"Component: {comp.name}, Type: {comp.GetType().Name}");

            // Проверить пины
            PinHighlighter[] pins = comp.GetComponentsInChildren<PinHighlighter>();
            Debug.Log($"  Has {pins.Length} pins");

            foreach (var pin in pins)
            {
                Debug.Log($"    Pin #{pin.pinNumber}, Mode: {pin.pinMode}");
            }
        }
    }

    void Update()
    {
        // Быстрые тесты по клавишам
        if (Input.GetKeyDown(KeyCode.F1))
        {
            TestAllSystems();
        }

        if (Input.GetKeyDown(KeyCode.F2) && ConnectionManager.Instance != null)
        {
            ConnectionManager.Instance.DebugState();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            TestRaycast();
        }
    }

    void TestRaycast()
    {
        Debug.Log("=== RAYCAST TEST ===");

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            Debug.Log($"Hit: {hit.collider.name}");
            Debug.Log($"Position: {hit.point}");

            // Проверить компоненты
            PinHighlighter pin = hit.collider.GetComponent<PinHighlighter>();
            ComponentDragger dragger = hit.collider.GetComponent<ComponentDragger>();
            CircuitComponent circuit = hit.collider.GetComponent<CircuitComponent>();

            if (pin != null) Debug.Log("  This is a PIN!");
            if (dragger != null) Debug.Log("  This is draggable!");
            if (circuit != null) Debug.Log($"  This is circuit: {circuit.name}");
        }
        else
        {
            Debug.Log("No hit detected (maybe hitting UI?)");

            // Проверить UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("Mouse is over UI element");
            }
        }
    }
}