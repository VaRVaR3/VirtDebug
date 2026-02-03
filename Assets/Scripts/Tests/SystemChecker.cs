using UnityEngine;

public class SystemChecker : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) CheckSystem();
        if (Input.GetKeyDown(KeyCode.F2)) TestPinConnection();
        if (Input.GetKeyDown(KeyCode.F3)) TestDraggingHint();
    }

    void CheckSystem()
    {
        Debug.Log("=== SYSTEM CHECK ===");

        Debug.Log(FindObjectOfType<InteractionManager>() == null ? "❌ InteractionManager missing" : "✅ InteractionManager OK");
        Debug.Log(SuperSimpleConnectionManager.Instance == null ? "❌ ConnectionManager.Instance NULL" : "✅ ConnectionManager OK");

        Debug.Log($"Arduino count: {FindObjectsOfType<VirtualArduino>().Length}");
        Debug.Log($"LED count: {FindObjectsOfType<VirtualLED>().Length}");
        Debug.Log($"Pins (UltraSimplePin) count: {FindObjectsOfType<UltraSimplePin>(true).Length}");
        Debug.Log($"Draggers (ComponentDragger) count: {FindObjectsOfType<ComponentDragger>(true).Length}");

        Debug.Log("=== CHECK COMPLETE ===");
    }

    void TestPinConnection()
    {
        Debug.Log("=== TEST PIN CONNECTION ===");

        var arduino = FindObjectOfType<VirtualArduino>();
        var led = FindObjectOfType<VirtualLED>();

        if (arduino == null || led == null)
        {
            Debug.LogError("Нужны Arduino и LED в сцене!");
            return;
        }

        if (SuperSimpleConnectionManager.Instance == null)
        {
            Debug.LogError("ConnectionManager.Instance = null");
            return;
        }

        // Ищем UltraSimplePin: Arduino pin 13 и LED pin 1
        UltraSimplePin arduinoPin = null;
        UltraSimplePin ledPin = null;

        foreach (var p in arduino.GetComponentsInChildren<UltraSimplePin>(true))
            if (p.pinNumber == 13) arduinoPin = p;

        foreach (var p in led.GetComponentsInChildren<UltraSimplePin>(true))
            if (p.pinNumber == 1) ledPin = p;

        if (arduinoPin == null || ledPin == null)
        {
            Debug.LogError($"Не нашёл нужные пины. Arduino13={(arduinoPin != null)}, LED1={(ledPin != null)}");
            return;
        }

        Debug.Log($"Found pins: Arduino={arduinoPin.name} pin={arduinoPin.pinNumber}, LED={ledPin.name} pin={ledPin.pinNumber}");

        // Тестовое соединение
        arduinoPin.HandleClickFromManager(); // Start
        ledPin.HandleClickFromManager();     // Complete
    }

    void TestDraggingHint()
    {
        Debug.Log("=== DRAG TEST ===");
        Debug.Log("Теперь drag делает InteractionManager, а ComponentDragger — просто метка.");
        Debug.Log("Если drag не работает: проверь слой Components + componentMask в InteractionManager.");
    }
}
