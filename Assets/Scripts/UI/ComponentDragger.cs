using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class ComponentDragger : MonoBehaviour
{
    public bool isDragging { get; private set; } = false;
    private Vector3 offset;
    private Vector3 originalPosition;
    private Camera mainCamera;

    [Header("Dragging Settings")]
    public float dragSpeed = 10f;
    public float rotationSpeed = 100f;
    public bool useGrid = true;
    public float gridSize = 0.25f;
    public float liftHeight = 0.1f;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void OnMouseDown()
    {
        // Проверяем, нет ли UI поверх
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("Pointer over UI, not dragging");
            return;
        }

        // Проверяем, не пытаемся ли мы соединять пины
        if (ConnectionManager.Instance != null && ConnectionManager.Instance.isConnecting)
        {
            Debug.Log("Currently connecting pins, not dragging");
            return;
        }

        // Проверяем, можно ли перетаскивать этот компонент
        if (!CanStartDragging())
        {
            return;
        }

        StartDragging();
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0, originalPosition.y + liftHeight, 0));
        float distance;

        if (plane.Raycast(ray, out distance))
        {
            Vector3 targetPosition = ray.GetPoint(distance) + offset;

            // Применяем сетку если нужно
            if (useGrid)
            {
                targetPosition.x = Mathf.Round(targetPosition.x / gridSize) * gridSize;
                targetPosition.z = Mathf.Round(targetPosition.z / gridSize) * gridSize;
            }

            // Плавное перемещение
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * dragSpeed);

            // Обновляем все провода, связанные с этим компонентом
            UpdateConnectedWires();
        }

        // Вращение колесиком мыши
        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            float rotation = Input.GetAxis("Mouse ScrollWheel") * rotationSpeed;
            transform.Rotate(0, rotation, 0, Space.World);
        }
    }

    void OnMouseUp()
    {
        if (!isDragging) return;

        // Опускаем объект обратно
        Vector3 finalPosition = transform.position;
        finalPosition.y = originalPosition.y;
        transform.position = finalPosition;

        isDragging = false;
        Debug.Log($"Stopped dragging {gameObject.name}");
    }

    private void StartDragging()
    {
        // Проверяем нет ли перетаскиваемого родителя
        if (IsParentBeingDragged())
        {
            Debug.Log("Parent is being dragged, not starting new drag");
            return;
        }

        isDragging = true;

        // Сохраняем начальную позицию
        originalPosition = transform.position;

        // Поднимаем объект
        Vector3 liftedPosition = originalPosition;
        liftedPosition.y += liftHeight;
        transform.position = liftedPosition;

        // Рассчитываем смещение
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, liftedPosition);
        float distance;

        if (plane.Raycast(ray, out distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            offset = transform.position - hitPoint;
        }

        Debug.Log($"Started dragging {gameObject.name}");
    }

    private bool IsParentBeingDragged()
    {
        Transform parent = transform.parent;
        while (parent != null)
        {
            ComponentDragger parentDragger = parent.GetComponent<ComponentDragger>();
            if (parentDragger != null && parentDragger.isDragging)
            {
                return true;
            }
            parent = parent.parent;
        }
        return false;
    }

    private bool CanStartDragging()
    {
        // Проверяем состояние компонента
        CircuitComponent circuit = GetComponent<CircuitComponent>();
        if (circuit != null && !circuit.isActive)
        {
            Debug.LogWarning("Cannot drag burned component!");
            return false;
        }

        return true;
    }

    private void UpdateConnectedWires()
    {
        // Находим все WireVisual в дочерних объектах и принудительно обновляем
        WireVisual[] wires = GetComponentsInChildren<WireVisual>();
        foreach (var wire in wires)
        {
            if (wire != null)
            {
                // Используем публичный метод для обновления
                wire.ForceUpdateWire();
            }
        }

        // Также обновляем все провода, которые соединены с этим компонентом
        // Используем старый способ проверки IConnectable
        MonoBehaviour mono = this as MonoBehaviour;
        if (mono != null)
        {
            IConnectable connectable = mono as IConnectable;
            if (connectable != null && ConnectionManager.Instance != null)
            {
                var connections = ConnectionManager.Instance.GetConnectionsForComponent(connectable);
                foreach (var conn in connections)
                {
                    if (conn.wireVisual != null)
                    {
                        WireVisual wire = conn.wireVisual.GetComponent<WireVisual>();
                        if (wire != null)
                        {
                            wire.ForceUpdateWire();
                        }
                    }
                }
            }
        }
    }

    public void ForceStopDragging()
    {
        if (isDragging)
        {
            OnMouseUp();
        }
    }

    void OnDisable()
    {
        // При отключении останавливаем перетаскивание
        ForceStopDragging();
    }
}