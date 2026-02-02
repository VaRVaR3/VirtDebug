using UnityEngine;
using UnityEngine.EventSystems;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance;

    [Header("Raycast Masks (default: Everything)")]
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

    private DraggableComponent dragging;
    private Vector3 grabOffset;
    private float dragY;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        cam = Camera.main;
        if (cam == null)
            Debug.LogError("❌ InteractionManager: Camera.main == null. Проверь, что у камеры стоит Tag = MainCamera.");
        else if (debugLogs)
            Debug.Log("✅ InteractionManager initialized. Camera = " + cam.name);
    }

    void Update()
    {
        if (cam == null) return;

        // Если мышь над UI — не трогаем сцену
        if (IsPointerOverBlockingUI())
            return;


        if (Input.GetMouseButtonDown(0))
            HandleMouseDown();

        if (dragging != null && Input.GetMouseButton(0))
            UpdateDrag();

        if (dragging != null && Input.GetMouseButtonUp(0))
            EndDrag();
    }

    private void HandleMouseDown()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // Для диагностики: что вообще под курсором?
        if (debugLogs)
        {
            if (Physics.Raycast(ray, out RaycastHit anyHit, maxDistance, ~0, QueryTriggerInteraction.Collide))
            {
                Debug.Log($"🟦 Click hit: {anyHit.collider.name} (layer={LayerMask.LayerToName(anyHit.collider.gameObject.layer)})");
            }
            else
            {
                Debug.Log("🟦 Click hit: nothing");
            }
        }

        // 1) PIN приоритетнее (важно QueryTriggerInteraction.Collide!)
        if (Physics.Raycast(ray, out RaycastHit hitPin, maxDistance, pinMask, QueryTriggerInteraction.Collide))
        {
            UltraSimplePin pin = hitPin.collider.GetComponentInParent<UltraSimplePin>();
            if (pin != null)
            {
                if (debugLogs) Debug.Log($"🟡 PIN click: {pin.name} pinNumber={pin.pinNumber}");
                pin.HandleClickFromManager();
                return;
            }
        }

        // 2) COMPONENT
        if (Physics.Raycast(ray, out RaycastHit hitComp, maxDistance, componentMask, QueryTriggerInteraction.Collide))
        {
            DraggableComponent drag = hitComp.collider.GetComponentInParent<DraggableComponent>();
            if (drag != null && drag.canDrag)
            {
                if (debugLogs) Debug.Log($"🟩 COMPONENT drag start: {drag.name}");
                BeginDrag(drag);
                return;
            }
        }
    }

    private void BeginDrag(DraggableComponent drag)
    {
        // если идёт соединение — запрещаем драг
        if (SuperSimpleConnectionManager.Instance != null && SuperSimpleConnectionManager.Instance.isConnecting)
            return;

        dragging = drag;
        dragging.OnDragStart();

        dragY = dragging.transform.position.y;

        // lift
        dragging.transform.position = new Vector3(dragging.transform.position.x, dragY + liftHeight, dragging.transform.position.z);

        // offset по плоскости Y=dragY — чтобы НЕ тянуло к камере
        Vector3 mouseOnPlane = MouseOnPlane(dragY);
        grabOffset = dragging.transform.position - mouseOnPlane;

        SuperSimpleConnectionManager.Instance?.UpdateAllWires();
    }

    private void UpdateDrag()
    {
        bool vertical = Input.GetKey(verticalModifier);

        Vector3 target = dragging.transform.position;

        if (vertical)
        {
            float dy = Input.GetAxis("Mouse Y") * verticalSpeed;
            dragY = Mathf.Clamp(dragY + dy, minY, maxY);
            target = new Vector3(dragging.transform.position.x, dragY + liftHeight, dragging.transform.position.z);
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

        dragging.transform.position = Vector3.Lerp(dragging.transform.position, target, Time.deltaTime * dragLerp);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            dragging.transform.Rotate(0f, scroll * rotationSpeed, 0f, Space.World);

        SuperSimpleConnectionManager.Instance?.UpdateAllWires();
        dragging.OnDragUpdate();
    }

    private void EndDrag()
    {
        dragging.transform.position = new Vector3(dragging.transform.position.x, dragY, dragging.transform.position.z);
        dragging.OnDragEnd();
        dragging = null;

        SuperSimpleConnectionManager.Instance?.UpdateAllWires();
    }

    private Vector3 MouseOnBoardOrPlane(float yPlane)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // ВАЖНО: тоже QueryTriggerInteraction.Collide
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

        // Если нет попаданий по UI — не блокируем
        if (!EventSystem.current.IsPointerOverGameObject())
            return false;

        // Проверяем, есть ли под курсором "блокирующий" UI
        // (кнопки / поля ввода и т.п.)
        var pointer = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);

        foreach (var r in results)
        {
            if (r.gameObject == null) continue;

            // Если это интерактивный UI — блокируем сцену
            if (r.gameObject.GetComponent<UnityEngine.UI.Button>() != null) return true;
            if (r.gameObject.GetComponent<TMPro.TMP_InputField>() != null) return true;
            if (r.gameObject.GetComponent<UnityEngine.UI.InputField>() != null) return true;
            if (r.gameObject.GetComponent<UnityEngine.UI.Toggle>() != null) return true;
            if (r.gameObject.GetComponent<UnityEngine.UI.Slider>() != null) return true;
            if (r.gameObject.GetComponent<UnityEngine.UI.Scrollbar>() != null) return true;
            if (r.gameObject.GetComponent<UnityEngine.UI.Dropdown>() != null) return true;
            if (r.gameObject.GetComponent<TMPro.TMP_Dropdown>() != null) return true;
        }

        // UI есть, но он не интерактивный (фон/декор) — не блокируем
        return false;
    }

}
