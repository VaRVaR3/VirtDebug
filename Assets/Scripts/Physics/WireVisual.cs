using UnityEngine;

public class WireVisual : MonoBehaviour
{
    public IConnectable sourceComponent;
    public int sourcePin;
    public IConnectable targetComponent;
    public int targetPin;
    public Color wireColor = Color.red;

    private LineRenderer lineRenderer;
    private Vector3[] positions = new Vector3[4];
    private float wireThickness = 0.02f;

    void Start()
    {
        // Используем LineRenderer вместо Cylinder для гибкости
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = wireThickness;
        lineRenderer.endWidth = wireThickness;

        // Создаем материал для провода
        Material wireMat = new Material(Shader.Find("Standard"));
        wireMat.color = wireColor;
        lineRenderer.material = wireMat;

        lineRenderer.positionCount = 4;
        lineRenderer.useWorldSpace = true;

        UpdateWirePositions();
    }

    void Update()
    {
        // Проверяем, что компоненты еще существуют
        if (sourceComponent == null || targetComponent == null)
        {
            Destroy(gameObject);
            return;
        }

        UpdateWirePositions();
    }

    // НОВЫЙ МЕТОД: Публичный метод для принудительного обновления
    public void ForceUpdateWire()
    {
        UpdateWirePositions();
    }

    private void UpdateWirePositions()
    {
        if (sourceComponent != null && targetComponent != null)
        {
            Vector3 startPos = sourceComponent.GetPinPosition(sourcePin);
            Vector3 endPos = targetComponent.GetPinPosition(targetPin);

            // Создаем изогнутый провод (кривая Безье)
            positions[0] = startPos;
            positions[1] = startPos + (endPos - startPos) * 0.25f + Vector3.up * 0.1f;
            positions[2] = startPos + (endPos - startPos) * 0.75f + Vector3.up * 0.1f;
            positions[3] = endPos;

            lineRenderer.SetPositions(positions);
        }
    }

    void OnDestroy()
    {
        // При удалении провода обновляем состояние пинов
        UpdatePinVisualState(sourceComponent, sourcePin, false);
        UpdatePinVisualState(targetComponent, targetPin, false);
    }

    private void UpdatePinVisualState(IConnectable component, int pin, bool connected)
    {
        if (component == null) return;

        MonoBehaviour monoComponent = component as MonoBehaviour;
        if (monoComponent != null)
        {
            PinHighlighter[] pins = monoComponent.GetComponentsInChildren<PinHighlighter>();
            foreach (PinHighlighter pinHighlighter in pins)
            {
                if (pinHighlighter != null && pinHighlighter.pinNumber == pin)
                {
                    pinHighlighter.SetConnected(connected);
                }
            }
        }
    }
}