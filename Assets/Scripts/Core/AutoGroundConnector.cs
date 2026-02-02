using UnityEngine;

public class AutoGroundConnector : MonoBehaviour
{
    [Header("Auto Connect Settings")]
    public bool autoConnectToGround = true;
    public int groundPinIndex = 0;

    [Header("Components")]
    public CircuitComponent targetComponent;
    public VirtualArduino arduino;

    void Start()
    {
        if (targetComponent == null)
            targetComponent = GetComponent<CircuitComponent>();

        if (arduino == null)
            arduino = FindObjectOfType<VirtualArduino>();

        if (autoConnectToGround && targetComponent != null && arduino != null)
        {
            ConnectToGround();
        }
    }

    void ConnectToGround()
    {
        ArduinoGroundPin groundPin = arduino.GetGroundPin(groundPinIndex);
        if (groundPin != null)
        {
            Debug.Log($"Auto-connecting {targetComponent.name} to Arduino GND pin {groundPin.pinNumber}");

            // Создаем соединение через ConnectionManager
            if (SuperSimpleConnectionManager.Instance != null)
            {
                IConnectable componentConnectable = targetComponent as IConnectable;
                IConnectable groundConnectable = groundPin as IConnectable;

                if (componentConnectable != null && groundConnectable != null)
                {
                    // Предполагаем, что отрицательный пин компонента = 2
                    SuperSimpleConnectionManager.Instance.StartConnection(componentConnectable, 2);
                    SuperSimpleConnectionManager.Instance.CompleteConnection(groundConnectable, groundPin.pinNumber);

                    // Обновляем состояние компонента
                    targetComponent.negativePin = groundPin.pinNumber;
                }
            }
        }
        else
        {
            Debug.LogWarning($"No ground pin found at index {groundPinIndex}");
        }
    }

    void Update()
    {
        // Для тестирования
        if (Input.GetKeyDown(KeyCode.G))
        {
            ConnectToGround();
        }
    }
}