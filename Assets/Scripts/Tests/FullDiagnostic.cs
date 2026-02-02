using UnityEngine;
using UnityEngine.EventSystems;

public class FullDiagnostic : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            RunFullDiagnostic();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            FixAllProblems();
        }
    }

    void RunFullDiagnostic()
    {
        Debug.Log("=== ПОЛНАЯ ДИАГНОСТИКА СИСТЕМЫ ===");

        // 1. Проверка базовых систем
        Debug.Log("1. Проверка базовых систем:");

        if (Camera.main == null)
            Debug.LogError("❌ Основная камера не найдена!");
        else
            Debug.Log("✅ Основная камера найдена");

        if (FindObjectOfType<EventSystem>() == null)
            Debug.LogWarning("⚠️ EventSystem не найден (нужен для UI)");
        else
            Debug.Log("✅ EventSystem найден");

        // 2. Проверка ConnectionManager
        Debug.Log("\n2. Проверка ConnectionManager:");

        if (SuperSimpleConnectionManager.Instance == null)
            Debug.LogError("❌ ConnectionManager.Instance = NULL!");
        else
        {
            Debug.Log("✅ ConnectionManager.Instance существует");
            Debug.Log($"   isConnecting: {SuperSimpleConnectionManager.Instance.isConnecting}");
        }

        // 3. Проверка компонентов
        Debug.Log("\n3. Проверка компонентов:");

        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        VirtualLED led = FindObjectOfType<VirtualLED>();

        if (arduino == null)
            Debug.LogError("❌ Arduino не найден!");
        else
        {
            Debug.Log($"✅ Arduino найден: {arduino.name}");
            // Проверяем пины Arduino
            PinHighlighter[] arduinoPins = arduino.GetComponentsInChildren<PinHighlighter>();
            Debug.Log($"   Пины Arduino: {arduinoPins.Length} шт");
        }

        if (led == null)
            Debug.LogError("❌ LED не найден!");
        else
        {
            Debug.Log($"✅ LED найден: {led.name}");
            // Проверяем пины LED
            PinHighlighter[] ledPins = led.GetComponentsInChildren<PinHighlighter>();
            Debug.Log($"   Пины LED: {ledPins.Length} шт");
        }

        // 4. Проверка PinHighlighter
        Debug.Log("\n4. Проверка PinHighlighter:");

        PinHighlighter[] allPins = FindObjectsOfType<PinHighlighter>();
        Debug.Log($"Всего пинов в сцене: {allPins.Length}");

        foreach (var pin in allPins)
        {
            Debug.Log($"   Пин {pin.pinNumber} на {pin.transform.parent?.name}: " +
                     $"parentComponent={(pin.parentComponent != null ? "да" : "нет")}, " +
                     $"isConnected={pin.isConnected}");
        }

        // 5. Проверка ComponentDragger
        Debug.Log("\n5. Проверка ComponentDragger:");

        ComponentDragger[] draggers = FindObjectsOfType<ComponentDragger>();
        Debug.Log($"ComponentDragger объектов: {draggers.Length}");

        foreach (var dragger in draggers)
        {
            Debug.Log($"   {dragger.gameObject.name}: isDragging={dragger.isDragging}");
        }

        // 6. Проверка коллайдеров
        Debug.Log("\n6. Проверка коллайдеров:");

        Collider[] colliders = FindObjectsOfType<Collider>();
        Debug.Log($"Коллайдеров в сцене: {colliders.Length}");

        // Проверяем коллайдеры на пинах
        int pinsWithColliders = 0;
        foreach (var pin in allPins)
        {
            if (pin.GetComponent<Collider>() != null)
                pinsWithColliders++;
        }
        Debug.Log($"Пины с коллайдерами: {pinsWithColliders}/{allPins.Length}");

        // 7. Проверка Layer'ов
        Debug.Log("\n7. Проверка Layer'ов:");

        if (LayerMask.NameToLayer("Default") == -1)
            Debug.LogError("❌ Слой Default не найден!");
        else
            Debug.Log("✅ Слой Default существует");

        Debug.Log("=== ДИАГНОСТИКА ЗАВЕРШЕНА ===");
    }

    void FixAllProblems()
    {
        Debug.Log("=== ИСПРАВЛЕНИЕ ПРОБЛЕМ ===");

        // 1. Создаем EventSystem если нет
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            Debug.Log("✅ Создан EventSystem");
        }

        // 2. Проверяем и создаем ConnectionManager если нет
        if (SuperSimpleConnectionManager.Instance == null)
        {
            SuperSimpleConnectionManager cm = FindObjectOfType<SuperSimpleConnectionManager>();
            if (cm == null)
            {
                GameObject cmObj = new GameObject("ConnectionManager");
                cm = cmObj.AddComponent<SuperSimpleConnectionManager>();
                Debug.Log("✅ Создан ConnectionManager");
            }
            else
            {
                Debug.Log("✅ ConnectionManager найден, но Instance не установлен");
            }
        }

        // 3. Проверяем пины
        PinHighlighter[] allPins = FindObjectsOfType<PinHighlighter>();
        foreach (var pin in allPins)
        {
            // Добавляем коллайдер если нет
            if (pin.GetComponent<Collider>() == null)
            {
                BoxCollider collider = pin.gameObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(0.2f, 0.2f, 0.2f);
                collider.isTrigger = true;
                Debug.Log($"✅ Добавлен коллайдер пину {pin.pinNumber}");
            }

            // Проверяем parentComponent
            if (pin.parentComponent == null)
            {
                // Вызываем метод FindParentComponent через reflection если он private
                var method = typeof(PinHighlighter).GetMethod("FindParentComponent",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(pin, null);
                    if (pin.parentComponent != null)
                        Debug.Log($"✅ Найден parentComponent для пина {pin.pinNumber}");
                }
            }
        }

        // 4. Проверяем компоненты
        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        if (arduino != null)
        {
            // Проверяем наличие ComponentDragger
            if (arduino.GetComponent<ComponentDragger>() == null)
            {
                arduino.gameObject.AddComponent<ComponentDragger>();
                Debug.Log($"✅ Добавлен ComponentDragger к Arduino");
            }
        }

        VirtualLED led = FindObjectOfType<VirtualLED>();
        if (led != null)
        {
            // Проверяем наличие ComponentDragger
            if (led.GetComponent<ComponentDragger>() == null)
            {
                led.gameObject.AddComponent<ComponentDragger>();
                Debug.Log($"✅ Добавлен ComponentDragger к LED");
            }
        }

        Debug.Log("=== ИСПРАВЛЕНИЯ ЗАВЕРШЕНЫ ===");
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;

        int y = 10;
        GUI.Label(new Rect(10, y, 400, 30), "F1 - Полная диагностика системы", style); y += 30;
        GUI.Label(new Rect(10, y, 400, 30), "F2 - Автоматическое исправление проблем", style); y += 30;

        // Показываем состояние системы
        y += 20;
        GUI.Label(new Rect(10, y, 400, 30), "=== СТАТУС СИСТЕМЫ ===", style); y += 30;

        if (SuperSimpleConnectionManager.Instance == null)
            GUI.Label(new Rect(10, y, 400, 30), "ConnectionManager: ❌ НЕ НАЙДЕН", style);
        else
        {
            string status = SuperSimpleConnectionManager.Instance.isConnecting ? "CONNECTING" : "IDLE";
            GUI.Label(new Rect(10, y, 400, 30), $"ConnectionManager: ✅ ({status})", style);
        }
        y += 30;

        VirtualArduino arduino = FindObjectOfType<VirtualArduino>();
        if (arduino == null)
            GUI.Label(new Rect(10, y, 400, 30), "Arduino: ❌ НЕ НАЙДЕН", style);
        else
            GUI.Label(new Rect(10, y, 400, 30), $"Arduino: ✅ ({arduino.name})", style);
        y += 30;

        VirtualLED led = FindObjectOfType<VirtualLED>();
        if (led == null)
            GUI.Label(new Rect(10, y, 400, 30), "LED: ❌ НЕ НАЙДЕН", style);
        else
            GUI.Label(new Rect(10, y, 400, 30), $"LED: ✅ ({led.name}, активен: {led.isActive})", style);
        y += 30;

        PinHighlighter[] pins = FindObjectsOfType<PinHighlighter>();
        GUI.Label(new Rect(10, y, 400, 30), $"Пинов в сцене: {pins.Length}", style);
    }
}