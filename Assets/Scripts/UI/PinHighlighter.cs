using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class PinHighlighter : MonoBehaviour
{
    [Header("Visual Settings")]
    public Material normalMaterial;
    public Material highlightMaterial;
    public Material connectedMaterial;
    public Material errorMaterial;

    [Header("Pin Information")]
    public int pinNumber = 1;
    public PinMode pinMode = PinMode.Input;
    public bool isConnected = false;
    public bool isGroundPin = false;
    public bool isPowerPin = false;

    [Header("Interaction Settings")]
    public float colliderSize = 0.3f; // УВЕЛИЧИЛИ
    public float interactionDistance = 5f;

    [Header("Tooltip")]
    public string pinDescription = "";
    public GameObject tooltipObject;

    [Header("Component Reference")]
    public MonoBehaviour parentMonoBehaviour;

    public IConnectable parentComponent { get; private set; }

    private Renderer pinRenderer;
    private bool isHighlighted = false;
    private Camera mainCamera;
    private EventSystem eventSystem;
    private TextMesh tooltipText;
    private Coroutine tooltipCoroutine;
    private Collider pinCollider;

    void Start()
    {
        Debug.LogError("PINHIGHLIGHTER FOUND ON: " + GetFullPath(transform));

        mainCamera = Camera.main;
        eventSystem = EventSystem.current;

        // Находим или создаем рендерер
        pinRenderer = GetComponent<Renderer>();
        if (pinRenderer == null)
        {
            // Ищем в дочерних объектах
            pinRenderer = GetComponentInChildren<Renderer>();
            if (pinRenderer == null)
            {
                // Создаем визуал для пина если его нет
                CreatePinVisual();
            }
        }

        if (pinRenderer != null)
        {
            if (normalMaterial == null)
            {
                normalMaterial = new Material(Shader.Find("Standard"));
            }
            pinRenderer.material = normalMaterial;
        }

        InitializeTooltip();
        FindParentComponent();
        SetupCollider(); // Теперь с увеличенным коллайдером
        UpdatePinVisual();

        Debug.Log($"✅ Pin {pinNumber} ({pinMode}) initialized");
    }

    private string GetFullPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    void CreatePinVisual()
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "PinVisual";
        visual.transform.SetParent(transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * 0.15f;

        Destroy(visual.GetComponent<Collider>()); // Удаляем стандартный коллайдер

        pinRenderer = visual.GetComponent<Renderer>();
        if (normalMaterial == null)
        {
            normalMaterial = new Material(Shader.Find("Standard"));
        }
        pinRenderer.material = normalMaterial;
    }

    void InitializeTooltip()
    {
        if (tooltipObject == null)
        {
            tooltipObject = new GameObject("PinTooltip");
            tooltipObject.transform.SetParent(transform);
            tooltipObject.transform.localPosition = new Vector3(0, 0.4f, 0); // Подняли выше

            GameObject textObj = new GameObject("TooltipText");
            textObj.transform.SetParent(tooltipObject.transform);
            textObj.transform.localPosition = Vector3.zero;

            tooltipText = textObj.AddComponent<TextMesh>();
            tooltipText.characterSize = 0.05f;
            tooltipText.fontSize = 24; // Увеличили
            tooltipText.alignment = TextAlignment.Center;
            tooltipText.anchor = TextAnchor.MiddleCenter;
            tooltipText.color = Color.white;
        }
        else
        {
            tooltipText = tooltipObject.GetComponentInChildren<TextMesh>();
        }

        UpdateTooltip();
        tooltipObject.SetActive(false);
    }

    void UpdateTooltip()
    {
        if (tooltipText == null) return;

        string tooltip = $"Pin {pinNumber}\n";
        tooltip += $"{pinMode}\n";

        if (!string.IsNullOrEmpty(pinDescription))
            tooltip += $"{pinDescription}\n";

        if (isGroundPin)
            tooltip += "GND\n";
        else if (isPowerPin)
            tooltip += "+5V\n";

        tooltipText.text = tooltip;
    }

    void UpdatePinVisual()
    {
        if (pinRenderer == null) return;

        if (isGroundPin)
        {
            pinRenderer.material.color = Color.black;
            return;
        }
        else if (isPowerPin)
        {
            pinRenderer.material.color = Color.red;
            return;
        }

        switch (pinMode)
        {
            case PinMode.Input:
                pinRenderer.material.color = Color.blue;
                break;
            case PinMode.Output:
                pinRenderer.material.color = Color.green;
                break;
            case PinMode.Analog:
                pinRenderer.material.color = Color.yellow;
                break;
            case PinMode.PWM:
                pinRenderer.material.color = Color.magenta;
                break;
            default:
                pinRenderer.material.color = Color.gray;
                break;
        }
    }

    void SetupCollider()
    {
        // Удаляем старые коллайдеры
        Collider[] oldColliders = GetComponents<Collider>();
        foreach (var col in oldColliders)
            Destroy(col);

        // Удаляем коллайдеры в дочерних объектах (кроме визуала)
        Collider[] childColliders = GetComponentsInChildren<Collider>();
        foreach (var col in childColliders)
        {
            if (col.gameObject != gameObject && col.GetComponent<Renderer>() == null)
                Destroy(col);
        }

        // Добавляем SphereCollider (лучше для взаимодействия)
        SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
        sphereCollider.radius = colliderSize / 2f;
        sphereCollider.isTrigger = true;

        pinCollider = sphereCollider;

        Debug.Log($"✅ Pin {pinNumber}: Sphere collider added (radius: {sphereCollider.radius})");
    }

    void FindParentComponent()
    {
        if (parentMonoBehaviour != null)
        {
            parentComponent = parentMonoBehaviour as IConnectable;
            if (parentComponent != null) return;
        }

        Transform current = transform.parent;
        while (current != null)
        {
            MonoBehaviour[] components = current.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component is IConnectable connectable)
                {
                    parentComponent = connectable;
                    parentMonoBehaviour = component;
                    Debug.Log($"✅ Found parent IConnectable for pin {pinNumber}: {component.GetType().Name}");
                    return;
                }
            }
            current = current.parent;
        }

        Debug.LogWarning($"⚠️ Pin {pinNumber}: Parent IConnectable not found. Trying to find in scene...");

        // Попробуем найти компонент по имени
        string parentName = transform.parent?.name ?? "Unknown";
        if (parentName.Contains("LED") || parentName.Contains("Led"))
        {
            VirtualLED led = GetComponentInParent<VirtualLED>();
            if (led != null)
            {
                parentComponent = led;
                parentMonoBehaviour = led;
                Debug.Log($"✅ Auto-assigned LED as parent for pin {pinNumber}");
                return;
            }
        }
        else if (parentName.Contains("Arduino") || parentName.Contains("Board"))
        {
            VirtualArduino arduino = GetComponentInParent<VirtualArduino>();
            if (arduino != null)
            {
                parentComponent = arduino;
                parentMonoBehaviour = arduino;
                Debug.Log($"✅ Auto-assigned Arduino as parent for pin {pinNumber}");
                return;
            }
        }
    }

    void OnMouseEnter()
    {
        if (eventSystem != null && eventSystem.IsPointerOverGameObject())
            return;

        if (!IsInteractable()) return;

        // Проверяем расстояние до камеры
        float distance = Vector3.Distance(transform.position, mainCamera.transform.position);
        if (distance > interactionDistance)
        {
            Debug.Log($"Pin {pinNumber} too far: {distance:F1}m");
            return;
        }

        isHighlighted = true;
        HighlightPin();
        ShowTooltipWithDelay();
    }

    void OnMouseExit()
    {
        if (!IsInteractable()) return;

        isHighlighted = false;
        UnhighlightPin();
        HideTooltip();
    }

    void ShowTooltipWithDelay()
    {
        if (tooltipCoroutine != null)
            StopCoroutine(tooltipCoroutine);

        tooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay(0.3f)); // Уменьшили задержку
    }

    IEnumerator ShowTooltipAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (isHighlighted && tooltipObject != null)
        {
            tooltipObject.SetActive(true);
        }
    }

    void HideTooltip()
    {
        if (tooltipCoroutine != null)
            StopCoroutine(tooltipCoroutine);

        if (tooltipObject != null)
            tooltipObject.SetActive(false);
    }

    void HighlightPin()
    {
        if (pinRenderer == null) return;

        if (highlightMaterial == null)
        {
            highlightMaterial = new Material(Shader.Find("Standard"));
            highlightMaterial.color = Color.white;
        }

        pinRenderer.material = highlightMaterial;
    }

    void UnhighlightPin()
    {
        if (pinRenderer == null) return;

        if (isConnected && connectedMaterial != null)
            pinRenderer.material = connectedMaterial;
        else
            UpdatePinVisual();
    }

    void OnMouseDown()
    {
        Debug.Log($"=== PIN {pinNumber} MOUSE DOWN ===");

        if (eventSystem != null && eventSystem.IsPointerOverGameObject())
        {
            Debug.Log("Over UI, ignoring");
            return;
        }

        if (!IsInteractable())
        {
            Debug.Log("Not interactable");
            return;
        }

        if (parentComponent == null)
        {
            Debug.LogError($"Pin {pinNumber}: No parent component!");
            FindParentComponent();

            if (parentComponent == null)
            {
                ShowPinError("No parent component found!");
                return;
            }
        }

        if (SuperSimpleConnectionManager.Instance == null)
        {
            Debug.LogError("ConnectionManager not found!");
            ShowPinError("Connection system not ready");
            return;
        }

        Debug.Log($"🔄 Pin {pinNumber} clicked. Parent: {parentComponent.GetName()}");

        // ПРОСТАЯ И ЯСНАЯ ЛОГИКА
        if (SuperSimpleConnectionManager.Instance.isConnecting)
        {
            Debug.Log($"🔄 Completing connection TO pin {pinNumber}");
            SuperSimpleConnectionManager.Instance.CompleteConnection(parentComponent, pinNumber);
        }
        else
        {
            Debug.Log($"🔄 Starting connection FROM pin {pinNumber}");
            SuperSimpleConnectionManager.Instance.StartConnection(parentComponent, pinNumber);
        }
    }

    void ShowPinError(string message)
    {
        Debug.LogError($"❌ Pin {pinNumber}: {message}");

        if (pinRenderer != null)
        {
            StartCoroutine(FlashError());
        }
    }

    IEnumerator FlashError()
    {
        if (pinRenderer == null) yield break;

        Color originalColor = pinRenderer.material.color;

        for (int i = 0; i < 3; i++)
        {
            pinRenderer.material.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            pinRenderer.material.color = originalColor;
            yield return new WaitForSeconds(0.15f);
        }

        UpdatePinVisual();
    }

    private bool IsInteractable()
    {
        if (pinRenderer == null)
        {
            Debug.Log($"Pin {pinNumber}: No renderer");
            return false;
        }

        if (!gameObject.activeInHierarchy)
        {
            Debug.Log($"Pin {pinNumber}: Not active");
            return false;
        }

        if (pinCollider == null || !pinCollider.enabled)
        {
            Debug.Log($"Pin {pinNumber}: No collider");
            return false;
        }

        return true;
    }

    public void SetConnected(bool connected)
    {
        isConnected = connected;

        if (connected)
        {
            if (connectedMaterial == null)
            {
                connectedMaterial = new Material(Shader.Find("Standard"));
                connectedMaterial.color = Color.cyan;
            }

            if (pinRenderer != null)
                pinRenderer.material = connectedMaterial;

            Debug.Log($"✅ Pin {pinNumber} connected");
        }
        else
        {
            UpdatePinVisual();
            Debug.Log($"✅ Pin {pinNumber} disconnected");
        }
    }

    public void SetPinMode(PinMode mode)
    {
        pinMode = mode;
        UpdatePinVisual();
        UpdateTooltip();
    }

    [ContextMenu("Test This Pin")]
    public void TestPin()
    {
        Debug.Log($"=== Testing Pin {pinNumber} ===");
        Debug.Log($"Mode: {pinMode}");
        Debug.Log($"GND: {isGroundPin}, Power: {isPowerPin}");
        Debug.Log($"Connected: {isConnected}");
        Debug.Log($"Parent: {parentComponent?.GetType().Name ?? "None"}");
        Debug.Log($"Has Collider: {GetComponent<Collider>() != null}");

        // Тестовое мигание
        StartCoroutine(TestBlink());
    }

    IEnumerator TestBlink()
    {
        if (pinRenderer == null) yield break;

        for (int i = 0; i < 3; i++)
        {
            pinRenderer.material.color = Color.white;
            yield return new WaitForSeconds(0.2f);
            UpdatePinVisual();
            yield return new WaitForSeconds(0.2f);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Визуализация коллайдера в редакторе
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, colliderSize / 2f);
    }
}