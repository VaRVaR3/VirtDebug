using UnityEngine;

// Простой скрипт только для пинов Arduino (без сложной логики)
public class SimplePin : MonoBehaviour
{
    public int pinNumber = 1;
    public PinMode pinMode = PinMode.Output;
    public bool isGround = false;
    public bool isPower = false;

    private Renderer pinRenderer;
    private Material originalMaterial;

    void Start()
    {
        pinRenderer = GetComponent<Renderer>();
        if (pinRenderer == null)
            pinRenderer = GetComponentInChildren<Renderer>();

        if (pinRenderer != null)
        {
            originalMaterial = pinRenderer.material;
            UpdateVisual();
        }

        // Добавляем коллайдер если нет
        if (GetComponent<Collider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = 0.15f;
            collider.isTrigger = true;
        }
    }

    void UpdateVisual()
    {
        if (pinRenderer == null) return;

        if (isGround)
            pinRenderer.material.color = Color.black;
        else if (isPower)
            pinRenderer.material.color = Color.red;
        else
            pinRenderer.material.color = Color.gray;
    }

    void OnMouseEnter()
    {
        if (pinRenderer != null)
            pinRenderer.material.color = Color.yellow;
    }

    void OnMouseExit()
    {
        UpdateVisual();
    }

    void OnMouseDown()
    {
        Debug.Log($"SimplePin {pinNumber} clicked");

        // Ищем родительский Arduino
        VirtualArduino arduino = GetComponentInParent<VirtualArduino>();
        if (arduino == null)
        {
            Debug.LogError($"Pin {pinNumber}: No Arduino parent found!");
            return;
        }

        if (SuperSimpleConnectionManager.Instance == null)
        {
            Debug.LogError("ConnectionManager not found!");
            return;
        }

        // Подключаемся как IConnectable
        if (SuperSimpleConnectionManager.Instance.isConnecting)
        {
            Debug.Log($"Completing connection to Arduino pin {pinNumber}");
            SuperSimpleConnectionManager.Instance.CompleteConnection(arduino, pinNumber);
        }
        else
        {
            Debug.Log($"Starting connection from Arduino pin {pinNumber}");
            SuperSimpleConnectionManager.Instance.StartConnection(arduino, pinNumber);
        }
    }
}