using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

public class ArduinoSimulator : MonoBehaviour
{
    public static ArduinoSimulator Instance;

    [Header("UI References")]
    public GameObject componentPanel;
    public TMP_InputField codeInput;
    public TextMeshProUGUI serialMonitor;
    public Transform workspace;

    [Header("Prefabs")]
    public GameObject arduinoUnoPrefab;
    public GameObject ledPrefab;
    public GameObject resistorPrefab;
    public GameObject buttonPrefab;
    public GameObject potentiometerPrefab;
    public GameObject lcdDisplayPrefab;

    private List<CircuitComponent> components = new List<CircuitComponent>();
    public VirtualArduino currentArduino { get; private set; }
    private ArduinoCodeInterpreter interpreter;
    private StringBuilder serialOutput = new StringBuilder();

    void Awake()
    {
        Instance = this;
        interpreter = new ArduinoCodeInterpreter();
    }

    public void PlaceArduinoBoard()
    {
        if (currentArduino != null)
            Destroy(currentArduino.gameObject);

        GameObject arduinoObj = Instantiate(arduinoUnoPrefab, workspace);
        currentArduino = arduinoObj.GetComponent<VirtualArduino>();
        Debug.Log("Arduino board placed!");
    }

    public void AddComponent(GameObject componentPrefab)
    {
        if (componentPrefab == null)
        {
            Debug.LogError("Component prefab is null!");
            return;
        }

        GameObject componentObj = Instantiate(componentPrefab, workspace);
        CircuitComponent component = componentObj.GetComponent<CircuitComponent>();

        if (component != null)
        {
            components.Add(component);
            if (currentArduino != null)
                component.Initialize(currentArduino);
        }
        else
        {
            Debug.LogError("No CircuitComponent found on prefab!");
        }
    }

    public void CompileAndRun()
    {
        if (currentArduino == null)
        {
            Debug.LogError("No Arduino board placed!");
            SerialPrint("Error: No Arduino board placed!");
            return;
        }

        if (codeInput == null || string.IsNullOrEmpty(codeInput.text))
        {
            Debug.LogError("No code to compile!");
            SerialPrint("Error: No code to compile!");
            return;
        }

        string code = codeInput.text;
        interpreter.ExecuteCode(code, currentArduino);
        SerialPrint("Code compiled and running...");
    }

    public void SerialPrint(string text)
    {
        serialOutput.AppendLine(text);

        if (serialMonitor != null)
            serialMonitor.text = serialOutput.ToString();
    }

    public void ClearSerialMonitor()
    {
        serialOutput.Clear();
        if (serialMonitor != null)
            serialMonitor.text = "";
    }

    public void ClearWorkspace()
    {
        // 1. Очищаем все провода через ConnectionManager
        if (ConnectionManager.Instance != null)
        {
            ConnectionManager.Instance.DisconnectAll();
            ConnectionManager.Instance.ClearAllWires();
        }

        // 2. Удаляем все объекты в workspace
        List<GameObject> objectsToDestroy = new List<GameObject>();
        foreach (Transform child in workspace)
        {
            objectsToDestroy.Add(child.gameObject);
        }

        foreach (GameObject obj in objectsToDestroy)
        {
            Destroy(obj);
        }

        // 3. Удаляем все провода, которые могут быть вне workspace
        GameObject[] allWires = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allWires)
        {
            if (obj.name.Contains("Wire") || obj.name.Contains("TempWire"))
            {
                Destroy(obj);
            }
        }

        // 4. Сбрасываем состояние
        currentArduino = null;
        components.Clear();
        SerialPrint("Workspace cleared");

        // 5. Принудительный сбор мусора
        System.GC.Collect();
        Resources.UnloadUnusedAssets();
    }
}