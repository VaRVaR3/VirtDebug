using UnityEngine;

[RequireComponent(typeof(Collider))]
public class UltraSimplePin : MonoBehaviour
{
    [Header("Pin Settings")]
    public int pinNumber = 1;
    public PinMode pinMode = PinMode.Output;
    public bool isGroundPin = false;
    public bool isPowerPin = false;

    [Header("Collider")]
    public float colliderRadius = 0.15f;

    [Header("Visual")]
    public Color normalColor = Color.gray;
    public Color connectedColor = Color.green;

    private Renderer pinRenderer;
    private Material pinMaterial;
    private bool isConnected = false;
    private IConnectable parentComponent;

    void Awake()
    {
        EnsureCollider();
        pinRenderer = GetComponent<Renderer>();
        if (pinRenderer == null) pinRenderer = GetComponentInChildren<Renderer>();

        if (pinRenderer != null)
        {
            pinMaterial = new Material(Shader.Find("Standard"));
            pinRenderer.material = pinMaterial;
        }

        FindParentComponent();
        UpdateVisual();
    }

    private void EnsureCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) col = gameObject.AddComponent<SphereCollider>();

        // Лучше SphereCollider
        SphereCollider sc = col as SphereCollider;
        if (sc == null)
        {
            Destroy(col);
            sc = gameObject.AddComponent<SphereCollider>();
        }

        sc.radius = colliderRadius;
        sc.isTrigger = true; // важно: клики будут через Raycast с QueryTriggerInteraction.Collide
    }

    private void FindParentComponent()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            var monos = current.GetComponents<MonoBehaviour>();
            foreach (var m in monos)
            {
                if (m is IConnectable c)
                {
                    parentComponent = c;
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

        pinMaterial.EnableKeyword("_EMISSION");
        pinMaterial.SetColor("_EmissionColor", pinMaterial.color * 0.25f);
    }

    public void SetConnected(bool connected)
    {
        isConnected = connected;
        UpdateVisual();
    }

    // ✅ вызывается InteractionManager-ом
    public void HandleClickFromManager()
    {
        if (parentComponent == null) FindParentComponent();
        if (parentComponent == null)
        {
            Debug.LogError($"❌ UltraSimplePin {pinNumber}: no parent IConnectable found.");
            return;
        }

        if (SuperSimpleConnectionManager.Instance == null)
        {
            Debug.LogError("❌ SuperSimpleConnectionManager not found in scene!");
            return;
        }

        if (SuperSimpleConnectionManager.Instance.isConnecting)
            SuperSimpleConnectionManager.Instance.CompleteConnection(parentComponent, pinNumber);
        else
            SuperSimpleConnectionManager.Instance.StartConnection(parentComponent, pinNumber);
    }
}
