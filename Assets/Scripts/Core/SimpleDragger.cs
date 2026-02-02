using UnityEngine;

public class SimpleDragger : MonoBehaviour
{
    private bool isDragging = false;
    private Vector3 offset;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void OnMouseDown()
    {
        // Если соединяем пины - не перетаскиваем
        if (SuperSimpleConnectionManager.Instance != null &&
            SuperSimpleConnectionManager.Instance.isConnecting)
        {
            Debug.Log("Connecting pins, skip drag");
            return;
        }

        StartDragging();
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        float distance;

        if (plane.Raycast(ray, out distance))
        {
            Vector3 targetPos = ray.GetPoint(distance) + offset;
            transform.position = targetPos;

            // Обновляем провода
            UpdateConnectedWires();
        }
    }

    void OnMouseUp()
    {
        isDragging = false;
    }

    void StartDragging()
    {
        isDragging = true;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, transform.position);
        float distance;

        if (plane.Raycast(ray, out distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            offset = transform.position - hitPoint;
        }

        Debug.Log($"🧲 Started dragging {name}");
    }

    // ИСПРАВЛЕННЫЙ МЕТОД
    private void UpdateConnectedWires()
    {
        // Находим все провода на сцене и обновляем их
        WireVisual[] allWires = FindObjectsOfType<WireVisual>();
        foreach (WireVisual wire in allWires)
        {
            wire.UpdateWirePositions(); // ЗДЕСЬ ИСПРАВЛЕНО ИМЯ
        }
    }
}