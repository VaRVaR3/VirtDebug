using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class ComponentDragger : MonoBehaviour
{
    public bool isDragging { get; private set; } = false;

    private Vector3 offset;
    private Vector3 originalPosition;
    private Camera mainCamera;

    [Header("Dragging Settings")]
    public float dragSpeed = 5f;
    public float rotationSpeed = 100f;
    public bool useGrid = false;
    public float gridSize = 0.25f;
    public float liftHeight = 0.05f;

    [Header("Raycast Settings")]
    public LayerMask groundLayer = 1 << 0;
    public float maxDragDistance = 200f;

    [Header("Fix Collider Refresh")]
    [Tooltip("Автоматически 'перезапускает' коллайдер на старте (как будто ты вручную дернул IsTrigger), чтобы клики/drag стабильно работали.")]
    public bool autoRefreshCollider = true;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogError("Main camera not found!");

        var col = GetComponent<Collider>();
        if (col == null)
            Debug.LogError($"❌ {name}: No Collider found, dragging won't work!");

        if (autoRefreshCollider && col != null)
        {
            StartCoroutine(RefreshColliderNextFrame(col));
        }
    }

    private IEnumerator RefreshColliderNextFrame(Collider col)
    {
        // ждём кадр, чтобы после инстанса/Start других компонентов всё стабилизировалось
        yield return null;

        // безопасный "rebuild" коллайдера
        bool prevEnabled = col.enabled;
        col.enabled = false;
        Physics.SyncTransforms();   // на всякий случай
        col.enabled = prevEnabled;
        Physics.SyncTransforms();

        // дополнительный пинок: иногда помогает при странных bounds
        col.enabled = false;
        col.enabled = true;
        Physics.SyncTransforms();

        // Debug.Log($"✅ Collider refreshed on {name}");
    }

    void OnMouseDown()
    {
        Debug.Log($"ComponentDragger: Mouse down on {name}");

        if (!CanStartDragging())
        {
            Debug.Log("Cannot start dragging");
            return;
        }

        // Если клик пришёл через пин — не начинаем драг
        if (IsPointerOverPin())
        {
            Debug.Log("Click was on a pin, not dragging component.");
            return;
        }

        StartDragging();
    }

    void OnMouseDrag()
    {
        if (!isDragging || !this || gameObject == null) return;
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        bool hitGround = Physics.Raycast(ray, out hit, maxDragDistance, groundLayer);

        Vector3 targetPosition;

        if (hitGround)
        {
            targetPosition = hit.point + offset;
        }
        else
        {
            Plane plane = new Plane(Vector3.up, new Vector3(0f, originalPosition.y, 0f));
            float distance;

            if (plane.Raycast(ray, out distance))
            {
                Vector3 hitPoint = ray.GetPoint(distance);
                targetPosition = hitPoint + offset;
            }
            else
            {
                return;
            }
        }

        if (useGrid)
        {
            targetPosition.x = Mathf.Round(targetPosition.x / gridSize) * gridSize;
            targetPosition.z = Mathf.Round(targetPosition.z / gridSize) * gridSize;
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * dragSpeed * 2f);

        UpdateConnectedWires();

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            float rotation = scroll * rotationSpeed;
            transform.Rotate(0, rotation, 0, Space.World);
        }
    }

    void OnMouseUp()
    {
        if (!isDragging) return;

        Vector3 finalPosition = transform.position;
        finalPosition.y = originalPosition.y;
        transform.position = finalPosition;

        isDragging = false;
        Debug.Log($"✅ Stopped dragging {gameObject.name}");
    }

    private bool CanStartDragging()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("Pointer over UI, not dragging");
            return false;
        }

        if (SuperSimpleConnectionManager.Instance != null && SuperSimpleConnectionManager.Instance.isConnecting)
        {
            Debug.Log("Currently connecting pins, not dragging");
            return false;
        }

        CircuitComponent circuit = GetComponent<CircuitComponent>();
        if (circuit != null && !circuit.isActive)
        {
            Debug.LogWarning("Cannot drag burned component!");
            return false;
        }

        return true;
    }

    private bool IsPointerOverPin()
    {
        if (mainCamera == null) return false;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDragDistance))
        {
            if (hit.collider != null && hit.collider.GetComponent<UltraSimplePin>() != null)
                return true;
        }

        return false;
    }

    private void StartDragging()
    {
        if (IsParentBeingDragged())
        {
            Debug.Log("Parent is being dragged, not starting new drag");
            return;
        }

        isDragging = true;
        originalPosition = transform.position;

        Vector3 liftedPosition = originalPosition;
        liftedPosition.y += liftHeight;
        transform.position = liftedPosition;

        CalculateOffset(liftedPosition);

        Debug.Log($"✅ Started dragging {gameObject.name}");
    }

    private void CalculateOffset(Vector3 liftedPosition)
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, liftedPosition.y, 0f));
        float distance;

        if (plane.Raycast(ray, out distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            offset = transform.position - hitPoint;
        }
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

    private void UpdateConnectedWires()
    {
        if (SuperSimpleConnectionManager.Instance == null) return;
        SuperSimpleConnectionManager.Instance.UpdateAllWires();
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
        ForceStopDragging();
    }
}
