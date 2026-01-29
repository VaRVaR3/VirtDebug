using UnityEngine;

public class SimpleDrag : MonoBehaviour
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
        Debug.Log($"MouseDown на {name}");

        // Начинаем перетаскивание
        isDragging = true;

        // Рассчитываем смещение
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; // Расстояние от камеры
        offset = transform.position - mainCamera.ScreenToWorldPoint(mousePos);
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;
        Vector3 targetPos = mainCamera.ScreenToWorldPoint(mousePos) + offset;

        transform.position = targetPos;

        Debug.Log($"Dragging {name} to {targetPos}");
    }

    void OnMouseUp()
    {
        if (isDragging)
        {
            isDragging = false;
            Debug.Log($"MouseUp на {name}");
        }
    }
}