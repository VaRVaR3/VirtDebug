using UnityEngine;
using UnityEngine.EventSystems;

public class FullDiagnostic : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) RunFullDiagnostic();
        if (Input.GetKeyDown(KeyCode.F2)) FixAllProblems();
    }

    void RunFullDiagnostic()
    {
        Debug.Log("=== ПОЛНАЯ ДИАГНОСТИКА СИСТЕМЫ ===");

        // 1) База
        Debug.Log("1) База:");
        Debug.Log(Camera.main == null ? "❌ Camera.main = null" : "✅ Camera.main OK");
        Debug.Log(FindObjectOfType<EventSystem>() == null ? "⚠️ EventSystem не найден" : "✅ EventSystem OK");

        // 2) Менеджеры
        Debug.Log("\n2) Менеджеры:");
        Debug.Log(FindObjectOfType<InteractionManager>() == null ? "❌ InteractionManager НЕ найден" : "✅ InteractionManager найден");
        Debug.Log(SuperSimpleConnectionManager.Instance == null ? "❌ SuperSimpleConnectionManager.Instance = null" : $"✅ ConnectionManager OK | isConnecting={SuperSimpleConnectionManager.Instance.isConnecting}");

        // 3) Объекты
        Debug.Log("\n3) Компоненты:");
        var arduinos = FindObjectsOfType<VirtualArduino>();
        var leds = FindObjectsOfType<VirtualLED>();
        Debug.Log($"Arduino: {arduinos.Length} шт");
        Debug.Log($"LED: {leds.Length} шт");

        // 4) Пины UltraSimplePin
        Debug.Log("\n4) UltraSimplePin:");
        UltraSimplePin[] pins = FindObjectsOfType<UltraSimplePin>(true);
        Debug.Log($"UltraSimplePin в сцене: {pins.Length}");

        int pinsNoCollider = 0;
        int pinsNonTrigger = 0;

        foreach (var p in pins)
        {
            Collider c = p.GetComponent<Collider>();
            if (c == null) pinsNoCollider++;
            else if (!c.isTrigger) pinsNonTrigger++;

            string layerName = LayerMask.LayerToName(p.gameObject.layer);
            Debug.Log($"  Pin {p.pinNumber} | obj={p.name} | layer={layerName} | collider={(c ? c.GetType().Name : "NONE")} | trigger={(c ? c.isTrigger.ToString() : "-")}");
        }

        Debug.Log($"Пины без коллайдера: {pinsNoCollider}");
        Debug.Log($"Пины с коллайдером, но НЕ trigger: {pinsNonTrigger}");

        // 5) Компоненты для перетаскивания
        Debug.Log("\n5) ComponentDragger:");
        ComponentDragger[] drags = FindObjectsOfType<ComponentDragger>(true);
        Debug.Log($"ComponentDragger объектов: {drags.Length}");
        foreach (var d in drags)
        {
            string layerName = LayerMask.LayerToName(d.gameObject.layer);
            Debug.Log($"  {d.name} | canDrag={d.canDrag} | layer={layerName} | collider={(d.GetComponent<Collider>() ? "yes" : "NO")}");
        }

        Debug.Log("=== ДИАГНОСТИКА ЗАВЕРШЕНА ===");
    }

    void FixAllProblems()
    {
        Debug.Log("=== FIX ALL PROBLEMS ===");

        // 1) EventSystem
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            Debug.Log("✅ Создан EventSystem");
        }

        // 2) InteractionManager
        if (FindObjectOfType<InteractionManager>() == null)
        {
            GameObject im = new GameObject("InteractionManager");
            im.AddComponent<InteractionManager>();
            Debug.Log("✅ Создан InteractionManager");
        }

        // 3) ConnectionManager
        if (SuperSimpleConnectionManager.Instance == null)
        {
            var cm = FindObjectOfType<SuperSimpleConnectionManager>();
            if (cm == null)
            {
                GameObject cmObj = new GameObject("ConnectionManager");
                cmObj.AddComponent<SuperSimpleConnectionManager>();
                Debug.Log("✅ Создан ConnectionManager");
            }
            else
            {
                Debug.LogWarning("⚠️ ConnectionManager найден, но Instance не установлен (проверь Awake/дубликаты)");
            }
        }

        // 4) Починка пинов: коллайдер + trigger
        UltraSimplePin[] pins = FindObjectsOfType<UltraSimplePin>(true);
        foreach (var p in pins)
        {
            Collider c = p.GetComponent<Collider>();
            if (c == null)
            {
                SphereCollider sc = p.gameObject.AddComponent<SphereCollider>();
                sc.radius = 0.15f;
                sc.isTrigger = true;
                Debug.Log($"✅ Добавлен SphereCollider pin={p.pinNumber} ({p.name})");
            }
            else
            {
                if (!c.isTrigger)
                {
                    c.isTrigger = true;
                    Debug.Log($"✅ Сделал collider.isTrigger=true pin={p.pinNumber} ({p.name})");
                }
            }
        }

        Debug.Log("=== FIX DONE ===");
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle { fontSize = 18 };
        style.normal.textColor = Color.white;

        int y = 10;
        GUI.Label(new Rect(10, y, 600, 30), "F1 - Полная диагностика", style); y += 26;
        GUI.Label(new Rect(10, y, 600, 30), "F2 - Авто-фикс (EventSystem/Managers/Pin Colliders)", style); y += 26;

        y += 10;
        string cm = SuperSimpleConnectionManager.Instance == null ? "❌ null" : (SuperSimpleConnectionManager.Instance.isConnecting ? "✅ CONNECTING" : "✅ IDLE");
        GUI.Label(new Rect(10, y, 600, 30), $"ConnectionManager: {cm}", style); y += 26;

        string im = FindObjectOfType<InteractionManager>() == null ? "❌ null" : "✅ OK";
        GUI.Label(new Rect(10, y, 600, 30), $"InteractionManager: {im}", style); y += 26;

        GUI.Label(new Rect(10, y, 600, 30), $"UltraSimplePin count: {FindObjectsOfType<UltraSimplePin>(true).Length}", style);
    }
}
