using UnityEngine;

public class SimpleComponentDragger : MonoBehaviour
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
        // Проверяем не подключение ли это
        if (ConnectionManager.Instance != null && ConnectionManager.Instance.isConnecting)
        {
            Debug.Log("Пропускаем перетаскивание - идет подключение");
            return;
        }

        // Проверяем не пин ли это
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.GetComponent<PinHighlighter>() != null)
            {
                Debug.Log("Кликнули по пину - не начинаем перетаскивание");
                return;
            }
        }

        // Начинаем перетаскивание
        isDragging = true;
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; // Расстояние от камеры
        offset = transform.position - mainCamera.ScreenToWorldPoint(mousePos);

        Debug.Log($"Начали перетаскивание {name}");
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;
        Vector3 targetPos = mainCamera.ScreenToWorldPoint(mousePos) + offset;

        transform.position = targetPos;
    }

    void OnMouseUp()
    {
        if (isDragging)
        {
            isDragging = false;
            Debug.Log($"Закончили перетаскивание {name}");
        }
    }
}