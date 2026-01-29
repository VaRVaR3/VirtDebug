using UnityEngine;

public class SystemReset : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetEverything();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            TestBasicInteraction();
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            CreateTestEnvironment();
        }
    }

    void ResetEverything()
    {
        Debug.Log("=== ПОЛНЫЙ СБРОС СИСТЕМЫ ===");

        // Удаляем все провода
        WireVisual[] wires = FindObjectsOfType<WireVisual>();
        foreach (var wire in wires)
        {
            Destroy(wire.gameObject);
        }
        Debug.Log($"Удалено проводов: {wires.Length}");

        // Сбрасываем ConnectionManager
        if (ConnectionManager.Instance != null)
        {
            ConnectionManager.Instance.DisconnectAll();
            Debug.Log("ConnectionManager сброшен");
        }

        // Сбрасываем все пины
        PinHighlighter[] pins = FindObjectsOfType<PinHighlighter>();
        foreach (var pin in pins)
        {
            pin.isConnected = false;
            // Обновляем цвет
            if (pin.GetComponent<Renderer>() != null)
                pin.GetComponent<Renderer>().material.color = Color.white;
        }
        Debug.Log($"Сброшено пинов: {pins.Length}");

        // Сбрасываем компоненты
        VirtualLED[] leds = FindObjectsOfType<VirtualLED>();
        foreach (var led in leds)
        {
            led.positivePin = -1;
            led.negativePin = -1;
            led.connectedArduino = null;
            led.isActive = true;
        }

        VirtualArduino[] arduinos = FindObjectsOfType<VirtualArduino>();
        foreach (var arduino in arduinos)
        {
            for (int i = 0; i < arduino.digitalPins.Count; i++)
            {
                arduino.digitalPins[i].isConnected = false;
                arduino.digitalPins[i].connectedComponent = null;
                arduino.digitalPins[i].voltage = 0f;
            }
        }

        Debug.Log("=== СБРОС ЗАВЕРШЕН ===");
    }

    void TestBasicInteraction()
    {
        Debug.Log("=== ТЕСТ БАЗОВОГО ВЗАИМОДЕЙСТВИЯ ===");

        // Тест 1: Проверка коллайдеров
        Debug.Log("1. Тест коллайдеров:");

        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        VirtualLED led = FindObjectOfType<VirtualLED>();

        if (arduino != null)
        {
            Collider arduinoCollider = arduino.GetComponent<Collider>();
            Debug.Log($"Arduino коллайдер: {arduinoCollider != null}");
        }

        if (led != null)
        {
            Collider ledCollider = led.GetComponent<Collider>();
            Debug.Log($"LED коллайдер: {ledCollider != null}");
        }

        // Тест 2: Raycast от камеры
        Debug.Log("\n2. Тест Raycast:");

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 2f);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log($"Raycast попал в: {hit.collider.name}");
            Debug.Log($"Тэг: {hit.collider.tag}, Слой: {hit.collider.gameObject.layer}");
        }
        else
        {
            Debug.LogError("❌ Raycast НЕ попадает ни во что!");
        }

        // Тест 3: Проверка мыши
        Debug.Log("\n3. Тест мыши:");
        Debug.Log($"Позиция мыши: {Input.mousePosition}");

        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(mouseRay, out RaycastHit mouseHit))
        {
            Debug.Log($"Мышь указывает на: {mouseHit.collider.name}");

            // Проверяем есть ли компоненты
            ComponentDragger dragger = mouseHit.collider.GetComponent<ComponentDragger>();
            PinHighlighter pin = mouseHit.collider.GetComponent<PinHighlighter>();

            Debug.Log($"ComponentDragger: {dragger != null}");
            Debug.Log($"PinHighlighter: {pin != null}");

            if (pin != null)
            {
                Debug.Log($"Пин {pin.pinNumber}: parentComponent={pin.parentComponent != null}");
            }
        }

        Debug.Log("=== ТЕСТ ЗАВЕРШЕН ===");
    }

    void CreateTestEnvironment()
    {
        Debug.Log("=== СОЗДАНИЕ ТЕСТОВОГО ОКРУЖЕНИЯ ===");

        // Создаем простой куб для теста перетаскивания
        GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testCube.name = "TestCube";
        testCube.transform.position = new Vector3(0, 0.5f, 0);
        testCube.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        testCube.GetComponent<Renderer>().material.color = Color.blue;

        // Добавляем ComponentDragger
        ComponentDragger dragger = testCube.AddComponent<ComponentDragger>();
        dragger.dragSpeed = 15f;
        dragger.useGrid = false;

        // Добавляем простой коллайдер если нет
        if (testCube.GetComponent<Collider>() == null)
        {
            testCube.AddComponent<BoxCollider>();
        }

        Debug.Log("✅ Создан TestCube с ComponentDragger");

        // Создаем простой пин для теста
        GameObject testPin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        testPin.name = "TestPin";
        testPin.transform.position = new Vector3(2, 0.5f, 0);
        testPin.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        testPin.GetComponent<Renderer>().material.color = Color.green;

        // Добавляем PinHighlighter
        PinHighlighter pinHighlighter = testPin.AddComponent<PinHighlighter>();
        pinHighlighter.pinNumber = 99;
        pinHighlighter.pinMode = PinMode.Output;

        // Настраиваем тестовый parentComponent
        testPin.AddComponent<TestConnectable>();

        Debug.Log("✅ Создан TestPin с PinHighlighter");

        // Создаем ConnectionManager если нет
        if (ConnectionManager.Instance == null)
        {
            GameObject cmObj = new GameObject("ConnectionManager");
            cmObj.AddComponent<ConnectionManager>();
            Debug.Log("✅ Создан ConnectionManager");
        }

        Debug.Log("=== ТЕСТОВОЕ ОКРУЖЕНИЕ СОЗДАНО ===");
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;

        int y = 50;
        GUI.Label(new Rect(10, y, 400, 30), "R - Полный сброс системы", style); y += 30;
        GUI.Label(new Rect(10, y, 400, 30), "T - Тест базового взаимодействия", style); y += 30;
        GUI.Label(new Rect(10, y, 400, 30), "Y - Создать тестовое окружение", style); y += 30;

        y += 20;
        GUI.Label(new Rect(10, y, 400, 30), "=== ТЕКУЩИЙ СТАТУС ===", style); y += 30;

        // Статус мыши
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(mouseRay, out RaycastHit hit))
        {
            GUI.Label(new Rect(10, y, 400, 30), $"Мышь на: {hit.collider.name}", style);
        }
        else
        {
            GUI.Label(new Rect(10, y, 400, 30), "Мышь: ничего не указывает", style);
        }
    }
}

// Простой тестовый класс IConnectable
public class TestConnectable : MonoBehaviour, IConnectable
{
    public string GetName()
    {
        return name;
    }

    public void OnConnected(int pin, IConnectable otherComponent, int otherPin)
    {
        Debug.Log($"TestConnectable {name} подключен: пин {pin} -> {otherComponent.GetName()} пин {otherPin}");
    }

    public void OnDisconnected(int pin)
    {
        Debug.Log($"TestConnectable {name} отключен от пина {pin}");
    }

    public Vector3 GetPinPosition(int pin)
    {
        return transform.position;
    }

    public bool CanConnectTo(int pin, IConnectable otherComponent, int otherPin)
    {
        return true;
    }
}