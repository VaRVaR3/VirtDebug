using UnityEngine;
using UnityEngine.EventSystems;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance;

    [Header("Raycast Masks")]
    public LayerMask pinMask = ~0;
    public LayerMask componentMask = ~0;
    public LayerMask boardMask = ~0;

    [Header("Raycast Settings")]
    public float maxDistance = 500f;

    [Header("Drag Settings")]
    public float dragLerp = 25f;
    public float liftHeight = 0.02f;
    public bool useGrid = false;
    public float gridSize = 0.25f;

    [Header("Vertical Move")]
    public KeyCode verticalModifier = KeyCode.LeftShift;
    public float verticalSpeed = 0.15f;
    public float minY = -10f;
    public float maxY = 10f;

    [Header("Rotation")]
    public float rotationSpeed = 120f;

    [Header("Debug")]
    public bool debugLogs = true;

    private Camera cam;

    // мы держим transform, а не конкретный класс
    private Transform draggingTransform;
    private DraggableComponent draggingDraggable;
    private ComponentDragger draggingLegacy;

    private Vector3 grabOffset;
    private float dragY;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        cam = Camera.main;
        if (cam == null)
            Debug.LogError("❌ InteractionManager: Camera.main == null. Проверь Tag = MainCamera");
        else if (debugLogs)
            Debug.Log("✅ InteractionManager initialized. Camera = " + cam.name);
    }

    void Update()
    {
        if (cam == null) return;

        if (IsPointerOverBlockingUI())
            return;

        if (Input.GetMouseButtonDown(0))
            HandleMouseDown();

        if (draggingTransform != null && Input.GetMouseButton(0))
            UpdateDrag();

        if (draggingTransform != null && Input.GetMouseButtonUp(0))
            EndDrag();
    }

    private void HandleMouseDown()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (debugLogs)
        {
            if (Physics.Raycast(ray, out RaycastHit anyHit, maxDistance, ~0, QueryTriggerInteraction.Collide))
                Debug.Log($"🟦 Click hit: {anyHit.collider.name} (layer={LayerMask.LayerToName(anyHit.collider.gameObject.layer)})");
            else
                Debug.Log("🟦 Click hit: nothing");
        }

        // 1) PIN first
        if (Physics.Raycast(ray, out RaycastHit hitPin, maxDistance, pinMask, QueryTriggerInteraction.Collide))
        {
            UltraSimplePin pin = hitPin.collider.GetComponent<UltraSimplePin>() ??
                                 hitPin.collider.GetComponentInParent<UltraSimplePin>();

            if (pin != null)
            {
                if (debugLogs) Debug.Log($"🟡 PIN click: {pin.name} pinNumber={pin.pinNumber}");
                pin.HandleClickFromManager();
                return;
            }
        }

        // 2) COMPONENT drag
        if (Physics.Raycast(ray, out RaycastHit hitComp, maxDistance, componentMask, QueryTriggerInteraction.Collide))
        {
            // СНАЧАЛА ищем DraggableComponent (у тебя он реально есть)
            DraggableComponent draggable =
                hitComp.collider.GetComponent<DraggableComponent>() ??
                hitComp.collider.GetComponentInParent<DraggableComponent>();

            if (draggable != null)
            {
                if (!draggable.canDrag)
                {
                    if (debugLogs) Debug.Log("🟧 Drag blocked: DraggableComponent.canDrag = false");
                    return;
                }

                // если идёт соединение — запрещаем драг
                if (SuperSimpleConnectionManager.Instance != null && SuperSimpleConnectionManager.Instance.isConnecting)
                {
                    if (debugLogs) Debug.Log("🟧 Drag blocked: currently connecting pins");
                    return;
                }

                if (debugLogs) Debug.Log($"🟩 COMPONENT drag start (DraggableComponent): {draggable.name}");
                BeginDrag(draggable);
                return;
            }

            // fallback: если вдруг где-то остался старый ComponentDragger
            ComponentDragger legacy =
                hitComp.collider.GetComponent<ComponentDragger>() ??
                hitComp.collider.GetComponentInParent<ComponentDragger>();

            if (legacy != null)
            {
                if (SuperSimpleConnectionManager.Instance != null && SuperSimpleConnectionManager.Instance.isConnecting)
                {
                    if (debugLogs) Debug.Log("🟧 Drag blocked: currently connecting pins");
                    return;
                }

                CircuitComponent circuit = legacy.GetComponent<CircuitComponent>();
                if (circuit != null && !circuit.isActive)
                {
                    if (debugLogs) Debug.LogWarning("🟧 Drag blocked: burned component (CircuitComponent.isActive = false)");
                    return;
                }

                if (debugLogs) Debug.Log($"🟩 COMPONENT drag start (ComponentDragger): {legacy.name}");
                BeginDrag(legacy);
                return;
            }

            if (debugLogs)
                Debug.Log($"🟥 No draggable script found on '{hitComp.collider.name}' or parents. Path: {GetFullPath(hitComp.collider.transform)}");
        }
    }

    // --- BEGIN DRAG overloads ---

    private void BeginDrag(DraggableComponent drag)
    {
        draggingDraggable = drag;
        draggingLegacy = null;
        draggingTransform = drag.transform;

        dragY = draggingTransform.position.y;

        // lift
        draggingTransform.position = new Vector3(
            draggingTransform.position.x,
            dragY + liftHeight,
            draggingTransform.position.z
        );

        Vector3 mouseOnPlane = MouseOnPlane(dragY);
        grabOffset = draggingTransform.position - mouseOnPlane;

        drag.OnDragStart();
        SuperSimpleConnectionManager.Instance?.UpdateAllWires();
    }

    private void BeginDrag(ComponentDragger drag)
    {
        draggingLegacy = drag;
        draggingDraggable = null;
        draggingTransform = drag.transform;

        dragY = draggingTransform.position.y;

        draggingTransform.position = new Vector3(
            draggingTransform.position.x,
            dragY + liftHeight,
            draggingTransform.position.z
        );

        Vector3 mouseOnPlane = MouseOnPlane(dragY);
        grabOffset = draggingTransform.position - mouseOnPlane;

        SuperSimpleConnectionManager.Instance?.UpdateAllWires();
    }

    private void UpdateDrag()
    {
        bool vertical = Input.GetKey(verticalModifier);
        Vector3 target = draggingTransform.position;

        if (vertical)
        {
            float dy = Input.GetAxis("Mouse Y") * verticalSpeed;
            dragY = Mathf.Clamp(dragY + dy, minY, maxY);
            target = new Vector3(draggingTransform.position.x, dragY + liftHeight, draggingTransform.position.z);
        }
        else
        {
            Vector3 mouseWorld = MouseOnBoardOrPlane(dragY);
            target = mouseWorld + grabOffset;
            target.y = dragY + liftHeight;

            if (useGrid)
            {
                target.x = Mathf.Round(target.x / gridSize) * gridSize;
                target.z = Mathf.Round(target.z / gridSize) * gridSize;
            }
        }

        draggingTransform.position = Vector3.Lerp(draggingTransform.position, target, Time.deltaTime * dragLerp);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            draggingTransform.Rotate(0f, scroll * rotationSpeed, 0f, Space.World);

        draggingDraggable?.OnDragUpdate();
        SuperSimpleConnectionManager.Instance?.UpdateAllWires();
    }

    private void EndDrag()
    {
        // опускаем обратно
        draggingTransform.position = new Vector3(draggingTransform.position.x, dragY, draggingTransform.position.z);

        draggingDraggable?.OnDragEnd();

        draggingTransform = null;
        draggingDraggable = null;
        draggingLegacy = null;

        SuperSimpleConnectionManager.Instance?.UpdateAllWires();
    }

    private Vector3 MouseOnBoardOrPlane(float yPlane)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, boardMask, QueryTriggerInteraction.Collide))
            return hit.point;

        return MouseOnPlane(yPlane);
    }

    private Vector3 MouseOnPlane(float yPlane)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, yPlane, 0f));
        if (plane.Raycast(ray, out float dist))
            return ray.GetPoint(dist);
        return Vector3.zero;
    }

    private bool IsPointerOverBlockingUI()
    {
        if (EventSystem.current == null) return false;
        if (!EventSystem.current.IsPointerOverGameObject()) return false;

        var pointer = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);

        foreach (var r in results)
        {
            if (r.gameObject == null) continue;

            if (r.gameObject.GetComponent<UnityEngine.UI.Button>() != null) return true;
            if (r.gameObject.GetComponent<TMPro.TMP_InputField>() != null) return true;
            if (r.gameObject.GetComponent<UnityEngine.UI.InputField>() != null) return true;
            if (r.gameObject.GetComponent<UnityEngine.UI.Toggle>() != null) return true;
            if (r.gameObject.GetComponent<UnityEngine.UI.Slider>() != null) return true;
            if (r.gameObject.GetComponent<UnityEngine.UI.Scrollbar>() != null) return true;
            if (r.gameObject.GetComponent<UnityEngine.UI.Dropdown>() != null) return true;
            if (r.gameObject.GetComponent<TMPro.TMP_Dropdown>() != null) return true;
        }

        return false;
    }

    private string GetFullPath(Transform t)
    {
        if (t == null) return "null";
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
