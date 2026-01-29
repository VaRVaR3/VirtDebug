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
    private float tooltipTimer = 0f;

    void Start()
    {
        mainCamera = Camera.main;
        eventSystem = EventSystem.current;

        pinRenderer = GetComponent<Renderer>();
        if (pinRenderer == null)
            pinRenderer = GetComponentInChildren<Renderer>();

        if (pinRenderer != null && normalMaterial != null)
            pinRenderer.material = normalMaterial;

        // Инициализируем подсказку
        InitializeTooltip();

        FindParentComponent();
        SetupCollider();
        UpdatePinVisual();

        Debug.Log($"Pin {pinNumber} ({pinMode}) initialized");
    }

    void InitializeTooltip()
    {
        if (tooltipObject == null)
        {
            tooltipObject = new GameObject("PinTooltip");
            tooltipObject.transform.SetParent(transform);
            tooltipObject.transform.localPosition = new Vector3(0, 0.3f, 0);

            GameObject textObj = new GameObject("TooltipText");
            textObj.transform.SetParent(tooltipObject.transform);
            textObj.transform.localPosition = Vector3.zero;

            tooltipText = textObj.AddComponent<TextMesh>();
            tooltipText.characterSize = 0.05f;
            tooltipText.fontSize = 20;
            tooltipText.alignment = TextAlignment.Center;
            tooltipText.anchor = TextAnchor.MiddleCenter;
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

        string tooltip = $"Пин {pinNumber}\n";
        tooltip += $"Режим: {pinMode}\n";

        if (!string.IsNullOrEmpty(pinDescription))
            tooltip += $"{pinDescription}\n";

        if (isGroundPin)
            tooltip += "Земля (GND)\n";
        else if (isPowerPin)
            tooltip += "Питание (+5V)\n";

        tooltipText.text = tooltip;
    }

    void UpdatePinVisual()
    {
        if (pinRenderer == null) return;

        if (isGroundPin)
        {
            // Чёрный для GND
            Material groundMat = new Material(Shader.Find("Standard"));
            groundMat.color = Color.black;
            pinRenderer.material = groundMat;
            return;
        }
        else if (isPowerPin)
        {
            // Красный для питания
            Material powerMat = new Material(Shader.Find("Standard"));
            powerMat.color = Color.red;
            pinRenderer.material = powerMat;
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
        // Удаляем старый коллайдер если есть
        Collider oldCollider = GetComponent<Collider>();
        if (oldCollider != null)
            Destroy(oldCollider);

        // Добавляем новый коллайдер
        BoxCollider collider = gameObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.2f, 0.2f, 0.2f);
        collider.isTrigger = true;

        Debug.Log($"Added collider to pin {pinNumber}");
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
                    Debug.Log($"Found parent IConnectable for pin {pinNumber}: {component.GetType().Name}");
                    return;
                }
            }
            current = current.parent;
        }

        Debug.LogWarning($"Pin {pinNumber}: Parent IConnectable component not found");
    }

    void Update()
    {
        // Управление подсказкой
        if (isHighlighted)
        {
            tooltipTimer += Time.deltaTime;
            if (tooltipTimer > 0.5f && !tooltipObject.activeSelf)
            {
                tooltipObject.SetActive(true);
            }
        }
    }

    void OnMouseEnter()
    {
        if (eventSystem != null && eventSystem.IsPointerOverGameObject())
            return;

        if (!IsInteractable()) return;

        isHighlighted = true;
        HighlightPin();

        // Показываем подсказку с задержкой
        tooltipTimer = 0f;
    }

    void OnMouseExit()
    {
        if (!IsInteractable()) return;

        isHighlighted = false;
        UnhighlightPin();

        tooltipObject.SetActive(false);
    }

    void HighlightPin()
    {
        if (pinRenderer == null || highlightMaterial == null) return;

        // Сохраняем оригинальный материал
        if (!isConnected)
        {
            pinRenderer.material = highlightMaterial;
        }
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
        if (eventSystem != null && eventSystem.IsPointerOverGameObject())
            return;

        if (!IsInteractable()) return;

        Debug.Log($"=== CLICKED ON PIN {pinNumber} ===");

        if (parentComponent == null)
        {
            ShowPinError("Пин не подключен к компоненту!");
            FindParentComponent(); // Пытаемся найти снова
            return;
        }

        if (ConnectionManager.Instance == null)
        {
            ShowPinError("ConnectionManager не найден!");
            return;
        }

        // Проверяем, можно ли подключить этот пин
        if (!CanConnectBasedOnMode())
        {
            ShowPinError($"Пин в режиме {pinMode} не может быть подключён!");
            return;
        }

        if (ConnectionManager.Instance.isConnecting)
        {
            Debug.Log($"Completing connection to pin {pinNumber}");
            ConnectionManager.Instance.CompleteConnection(parentComponent, pinNumber);
        }
        else
        {
            Debug.Log($"Starting connection from pin {pinNumber}");
            ConnectionManager.Instance.StartConnection(parentComponent, pinNumber);
        }
    }

    bool CanConnectBasedOnMode()
    {
        // Определяем, можно ли подключать этот пин
        switch (pinMode)
        {
            case PinMode.Input:
            case PinMode.Output:
            case PinMode.Ground:
            case PinMode.Power:
            case PinMode.Analog:
            case PinMode.PWM:
                return true;
            default:
                return true;
        }
    }

    void ShowPinError(string message)
    {
        Debug.LogError($"Pin {pinNumber}: {message}");

        // Визуальная обратная связь
        if (pinRenderer != null && errorMaterial != null)
        {
            StartCoroutine(FlashError());
        }

        // Показываем в подсказке
        if (tooltipText != null)
        {
            string originalText = tooltipText.text;
            tooltipText.text = $"ОШИБКА!\n{message}";
            tooltipObject.SetActive(true);

            StartCoroutine(RestoreTooltip(originalText, 2f));
        }
    }

    IEnumerator FlashError()
    {
        Material originalMat = pinRenderer.material;

        for (int i = 0; i < 3; i++)
        {
            pinRenderer.material = errorMaterial;
            yield return new WaitForSeconds(0.1f);
            pinRenderer.material = originalMat;
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator RestoreTooltip(string originalText, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (tooltipText != null)
        {
            tooltipText.text = originalText;
        }

        if (!isHighlighted)
        {
            tooltipObject.SetActive(false);
        }
    }

    private bool IsInteractable()
    {
        if (pinRenderer == null) return false;
        if (!gameObject.activeInHierarchy) return false;

        return true;
    }

    public void SetConnected(bool connected)
    {
        isConnected = connected;

        if (connected)
        {
            if (connectedMaterial != null)
                pinRenderer.material = connectedMaterial;

            Debug.Log($"Pin {pinNumber} подключен");
        }
        else
        {
            UpdatePinVisual();
            Debug.Log($"Pin {pinNumber} отключен");
        }
    }

    public void SetPinMode(PinMode mode)
    {
        pinMode = mode;
        UpdatePinVisual();
        UpdateTooltip();
    }

    [ContextMenu("Тестировать пин")]
    public void TestPin()
    {
        Debug.Log($"=== Тест пина {pinNumber} ===");
        Debug.Log($"Режим: {pinMode}");
        Debug.Log($"GND: {isGroundPin}, Power: {isPowerPin}");
        Debug.Log($"Подключён: {isConnected}");
        Debug.Log($"Родитель: {parentComponent?.GetType().Name ?? "Нет"}");

        // Мигание для теста
        StartCoroutine(TestBlink());
    }

    IEnumerator TestBlink()
    {
        for (int i = 0; i < 3; i++)
        {
            pinRenderer.material.color = Color.white;
            yield return new WaitForSeconds(0.2f);
            UpdatePinVisual();
            yield return new WaitForSeconds(0.2f);
        }
    }
}