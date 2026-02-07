using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    public float panSpeed = 10f;
    public float rotateSpeed = 100f;
    public float zoomSpeed = 20f;

    [Header("Zoom blocking")]
    public float rayDistance = 500f;

    // Если хочешь — можешь сузить до слоёв Components+Pins,
    // но можно оставить Everything.
    public LayerMask blockZoomMask = ~0;

    void Update()
    {
        // WASD для перемещения
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        transform.Translate(new Vector3(horizontal, 0, vertical) * panSpeed * Time.deltaTime, Space.Self);

        // Правая кнопка мыши для вращения
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime;

            transform.Rotate(Vector3.up * mouseX, Space.World);
            transform.Rotate(Vector3.left * mouseY, Space.Self);
        }

        // Колесико для зума (НО не когда крутим потенциометр/работаем с объектами)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            if (!ShouldBlockZoom())
            {
                transform.Translate(Vector3.forward * scroll * zoomSpeed * Time.deltaTime, Space.Self);
            }
        }
    }

    private bool ShouldBlockZoom()
    {
        // 1) если мышь над UI — не зумим
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return true;

        // 2) если под курсором пин/компонент/потенциометр — не зумим
        Camera cam = Camera.main;
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, blockZoomMask, QueryTriggerInteraction.Collide))
        {
            // потенциометр
            if (hit.collider.GetComponentInParent<VirtualPotentiometer>() != null)
                return true;

            // любые компоненты схемы
            if (hit.collider.GetComponentInParent<CircuitComponent>() != null)
                return true;

            // пины
            if (hit.collider.GetComponentInParent<UltraSimplePin>() != null)
                return true;
        }

        return false;
    }
}
