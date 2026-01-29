using UnityEngine;
using UnityEngine.UI;

public class CircuitBuilderUI : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button btnArduino;
    public Button btnLED;
    public Button btnResistor;
    public Button btnButton;
    public Button btnPotentiometer;
    public Button btnLCD;
    public Button btnCompile;
    public Button btnClear;

    [Header("References")]
    public ArduinoSimulator simulator;

    void Start()
    {
        // Находим симулятор, если не присвоен
        if (simulator == null)
            simulator = FindObjectOfType<ArduinoSimulator>();

        // Назначаем обработчики кнопок
        if (btnArduino != null)
            btnArduino.onClick.AddListener(OnPlaceArduinoClick);

        if (btnLED != null)
            btnLED.onClick.AddListener(OnPlaceLEDClick);

        if (btnResistor != null)
            btnResistor.onClick.AddListener(OnPlaceResistorClick);

        if (btnButton != null)
            btnButton.onClick.AddListener(OnPlaceButtonClick);

        if (btnPotentiometer != null)
            btnPotentiometer.onClick.AddListener(OnPlacePotentiometerClick);

        if (btnLCD != null)
            btnLCD.onClick.AddListener(OnPlaceLCDClick);

        if (btnCompile != null)
            btnCompile.onClick.AddListener(OnCompileClick);

        if (btnClear != null)
            btnClear.onClick.AddListener(OnClearWorkspaceClick);
    }

    public void OnPlaceArduinoClick()
    {
        if (simulator != null && simulator.arduinoUnoPrefab != null)
        {
            simulator.PlaceArduinoBoard();
        }
        else
        {
            Debug.LogError("Simulator or Arduino prefab not found!");
        }
    }

    public void OnPlaceLEDClick()
    {
        if (simulator != null && simulator.ledPrefab != null)
        {
            simulator.AddComponent(simulator.ledPrefab);
        }
    }

    public void OnPlaceResistorClick()
    {
        if (simulator != null && simulator.resistorPrefab != null)
        {
            simulator.AddComponent(simulator.resistorPrefab);
        }
    }

    public void OnPlaceButtonClick()
    {
        if (simulator != null && simulator.buttonPrefab != null)
        {
            simulator.AddComponent(simulator.buttonPrefab);
        }
    }

    public void OnPlacePotentiometerClick()
    {
        if (simulator != null && simulator.potentiometerPrefab != null)
        {
            simulator.AddComponent(simulator.potentiometerPrefab);
        }
    }

    public void OnPlaceLCDClick()
    {
        if (simulator != null && simulator.lcdDisplayPrefab != null)
        {
            simulator.AddComponent(simulator.lcdDisplayPrefab);
        }
    }

    public void OnCompileClick()
    {
        if (simulator != null)
        {
            simulator.CompileAndRun();
        }
    }

    public void OnClearWorkspaceClick()
    {
        if (simulator != null && simulator.workspace != null)
        {
            // Очистка рабочей области
            foreach (Transform child in simulator.workspace)
            {
                Destroy(child.gameObject);
            }

            simulator.ClearSerialMonitor();
            Debug.Log("Workspace cleared!");
        }
    }
}