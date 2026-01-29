using UnityEngine;
using System.Collections.Generic;

public class CircuitValidator : MonoBehaviour
{
    public static CircuitValidator Instance;

    [Header("Validation Settings")]
    public float validationInterval = 1f;
    public bool autoValidate = true;

    private List<VirtualLED> allLEDs = new List<VirtualLED>();
    private float timer = 0f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        InvokeRepeating("FindAllLEDs", 0f, 2f);
    }

    void Update()
    {
        if (autoValidate)
        {
            timer += Time.deltaTime;
            if (timer >= validationInterval)
            {
                ValidateAllCircuits();
                timer = 0f;
            }
        }

        // Ручная проверка
        if (Input.GetKeyDown(KeyCode.V))
        {
            ValidateAllCircuits();
        }
    }

    void FindAllLEDs()
    {
        VirtualLED[] foundLEDs = FindObjectsOfType<VirtualLED>();
        allLEDs = new List<VirtualLED>(foundLEDs);
    }

    void ValidateAllCircuits()
    {
        Debug.Log("=== CIRCUIT VALIDATION ===");

        foreach (VirtualLED led in allLEDs)
        {
            if (led == null) continue;

            bool circuitComplete = ValidateLEDCircuit(led);
            Debug.Log($"LED {led.name}: Circuit {(circuitComplete ? "VALID" : "INVALID")}");
        }
    }

    bool ValidateLEDCircuit(VirtualLED led)
    {
        // Проверка 1: подключены ли пины
        if (led.positivePin == -1 || led.negativePin == -1)
        {
            Debug.Log($"  LED {led.name}: Missing pin connections");
            return false;
        }

        // Проверка 2: есть ли подключение к Arduino
        if ((object)led.connectedArduino == null)
        {
            Debug.Log($"  LED {led.name}: Not connected to Arduino");
            return false;
        }

        // Проверка 3: есть ли соединения в ConnectionManager
        if ((object)ConnectionManager.Instance == null)
        {
            Debug.Log($"  LED {led.name}: ConnectionManager not found");
            return false;
        }

        // Проверяем через ConnectionManager
        return ConnectionManager.Instance.IsCircuitComplete(led);
    }

    // Метод для ручной проверки конкретного LED
    public bool CheckLEDCircuit(VirtualLED led)
    {
        return ValidateLEDCircuit(led);
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = Color.cyan;

        GUI.Label(new Rect(Screen.width - 200, 10, 190, 30), "Circuit Validator", style);
        GUI.Label(new Rect(Screen.width - 200, 40, 190, 30), "Press V to validate", style);
        GUI.Label(new Rect(Screen.width - 200, 70, 190, 30), $"LEDs: {allLEDs.Count}", style);
    }
}