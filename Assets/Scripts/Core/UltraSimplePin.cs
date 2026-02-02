using UnityEngine;

[RequireComponent(typeof(Collider))]
public class UltraSimplePin : MonoBehaviour
{
    [Header("Pin Settings")]
    public int pinNumber = 1;
    public PinMode pinMode = PinMode.Output;
    public bool isGroundPin = false;
    public bool isPowerPin = false;

    [Header("Visual")]
    public Color normalColor = Color.gray;
    public Color connectedColor = Color.green;

    private Renderer pinRenderer;
    private Material pinMaterial;
    private bool isConnected = false;
    private IConnectable parentComponent;

    void Start()
    {
        pinRenderer = GetComponent<Renderer>();
        if (pinRenderer != null)
        {
            pinMaterial = new Material(Shader.Find("Standard"));
            pinRenderer.material = pinMaterial;
        }

        FindParentComponent();
        UpdateVisual();
    }

    private void FindParentComponent()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            MonoBehaviour[] components = current.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp is IConnectable connectable)
                {
                    parentComponent = connectable;
                    return;
                }
            }
            current = current.parent;
        }
    }

    private void UpdateVisual()
    {
        if (pinMaterial == null) return;

        if (isGroundPin) pinMaterial.color = Color.black;
        else if (isPowerPin) pinMaterial.color = Color.red;
        else if (isConnected) pinMaterial.color = connectedColor;
        else pinMaterial.color = normalColor;
    }

    // ✅ вызывается InteractionManager-ом
    public void HandleClickFromManager()
    {
        if (parentComponent == null)
            FindParentComponent();

        if (parentComponent == null)
        {
            Debug.LogError($"❌ Pin {pinNumber}: no parent IConnectable found.");
            return;
        }

        if (SuperSimpleConnectionManager.Instance == null)
        {
            Debug.LogError("❌ SuperSimpleConnectionManager not found in scene!");
            return;
        }

        if (SuperSimpleConnectionManager.Instance.isConnecting)
        {
            SuperSimpleConnectionManager.Instance.CompleteConnection(parentComponent, pinNumber);
        }
        else
        {
            SuperSimpleConnectionManager.Instance.StartConnection(parentComponent, pinNumber);
        }
    }

    public void SetConnected(bool connected)
    {
        isConnected = connected;
        UpdateVisual();
    }
}
