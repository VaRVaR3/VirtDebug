using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WireVisual : MonoBehaviour
{
    [Header("Connection")]
    public IConnectable sourceComponent;
    public int sourcePin;
    public IConnectable targetComponent;
    public int targetPin;

    [Header("Style")]
    public Color wireColor = Color.red;
    public float wireThickness = 0.02f;

    [Header("Shape")]
    public bool useCurvedShape = true;
    public float curveHeight = 0.1f;

    private LineRenderer lineRenderer;
    private Vector3[] positions4 = new Vector3[4];

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();
    }

    void Start()
    {
        ApplyStyle();
        ForceUpdateWire();
    }

    void Update()
    {
        if (sourceComponent == null || targetComponent == null)
        {
            // Не уничтожаем сразу — чтобы не ловить "мигающий null" при удалении объектов
            return;
        }

        UpdateWirePositions();
    }

    private void ApplyStyle()
    {
        if (lineRenderer == null) return;

        lineRenderer.startWidth = wireThickness;
        lineRenderer.endWidth = wireThickness;

        // Улучшаем внешний вид
        lineRenderer.useWorldSpace = true;
        lineRenderer.numCapVertices = 6;
        lineRenderer.numCornerVertices = 6;
        lineRenderer.alignment = LineAlignment.View;

        // Материал устойчивый к URP/Built-in
        if (lineRenderer.material == null)
            lineRenderer.material = CreateLineMaterial(wireColor);
        else
            lineRenderer.material.color = wireColor;

        // На всякий случай emission
        if (lineRenderer.material != null && lineRenderer.material.HasProperty("_EmissionColor"))
        {
            lineRenderer.material.EnableKeyword("_EMISSION");
            lineRenderer.material.SetColor("_EmissionColor", wireColor * 0.5f);
        }
    }

    private Material CreateLineMaterial(Color color)
    {
        Shader shader =
            Shader.Find("Sprites/Default") ??
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Standard");

        if (shader == null)
        {
            Debug.LogError("❌ WireVisual: No suitable shader found!");
            shader = Shader.Find("Hidden/InternalErrorShader");
        }

        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    // Основной метод обновления позиций
    public void UpdateWirePositions()
    {
        if (lineRenderer == null) return;
        if (sourceComponent == null || targetComponent == null) return;

        Vector3 startPos = sourceComponent.GetPinPosition(sourcePin);
        Vector3 endPos = targetComponent.GetPinPosition(targetPin);

        if (!useCurvedShape)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);
            return;
        }

        // Твоя 4-точечная форма
        positions4[0] = startPos;
        positions4[1] = startPos + (endPos - startPos) * 0.25f + Vector3.up * curveHeight;
        positions4[2] = startPos + (endPos - startPos) * 0.75f + Vector3.up * curveHeight;
        positions4[3] = endPos;

        lineRenderer.positionCount = 4;
        lineRenderer.SetPositions(positions4);
    }

    // Метод для принудительного обновления (поддержка старого кода)
    public void ForceUpdateWire()
    {
        ApplyStyle();
        UpdateWirePositions();
    }

    void OnDestroy()
    {
        UpdatePinState(sourceComponent, sourcePin, false);
        UpdatePinState(targetComponent, targetPin, false);
    }

    private void UpdatePinState(IConnectable component, int pin, bool connected)
    {
        if (component == null) return;

        MonoBehaviour monoComponent = component as MonoBehaviour;
        if (monoComponent != null)
        {
            UltraSimplePin[] ultraPins = monoComponent.GetComponentsInChildren<UltraSimplePin>();
            foreach (var pinObj in ultraPins)
            {
                if (pinObj != null && pinObj.pinNumber == pin)
                {
                    pinObj.SetConnected(connected);
                    return;
                }
            }

            PinHighlighter[] oldPins = monoComponent.GetComponentsInChildren<PinHighlighter>();
            foreach (var pinObj in oldPins)
            {
                if (pinObj != null && pinObj.pinNumber == pin)
                {
                    pinObj.SetConnected(connected);
                    return;
                }
            }
        }
    }
}
