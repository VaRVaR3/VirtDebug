using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveLoadManager : MonoBehaviour
{
    [System.Serializable]
    public class ComponentData
    {
        public string componentType;
        public Vector3 position;
        public Quaternion rotation;
        public float resistance;
        public int positivePin;
        public int negativePin;
    }

    [System.Serializable]
    public class CircuitData
    {
        public string circuitName;
        public string savedDate;
        public List<ComponentData> components = new List<ComponentData>();
        public string arduinoCode;
    }

    public ArduinoSimulator simulator;

    private string savePath;

    void Start()
    {
        savePath = Application.persistentDataPath + "/Circuits/";

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
    }

    public void SaveCircuit(string circuitName)
    {
        CircuitData circuitData = new CircuitData
        {
            circuitName = circuitName,
            savedDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            arduinoCode = simulator.codeInput.text
        };

        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>();
        foreach (var component in allComponents)
        {
            if (component == null) continue;

            ComponentData compData = new ComponentData
            {
                componentType = component.GetType().Name,
                position = component.transform.position,
                rotation = component.transform.rotation,
                resistance = component.resistance,
                positivePin = component.positivePin,
                negativePin = component.negativePin
            };

            circuitData.components.Add(compData);
        }

        string jsonData = JsonUtility.ToJson(circuitData, true);
        string filePath = savePath + circuitName + ".json";

        File.WriteAllText(filePath, jsonData);

        Debug.Log($"Circuit saved to: {filePath}");
        simulator.SerialPrint($"Circuit '{circuitName}' saved successfully!");
    }

    public void LoadCircuit(string circuitName)
    {
        string filePath = savePath + circuitName + ".json";

        if (!File.Exists(filePath))
        {
            Debug.LogError($"Circuit file not found: {filePath}");
            simulator.SerialPrint($"Error: Circuit '{circuitName}' not found!");
            return;
        }

        simulator.ClearWorkspace();

        string jsonData = File.ReadAllText(filePath);
        CircuitData circuitData = JsonUtility.FromJson<CircuitData>(jsonData);

        simulator.codeInput.text = circuitData.arduinoCode;

        Debug.Log($"Circuit loaded from: {filePath}");
        simulator.SerialPrint($"Circuit '{circuitName}' loaded successfully!");
    }

    public List<string> GetSavedCircuits()
    {
        List<string> circuits = new List<string>();

        if (Directory.Exists(savePath))
        {
            string[] files = Directory.GetFiles(savePath, "*.json");
            foreach (string file in files)
            {
                circuits.Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        return circuits;
    }
}