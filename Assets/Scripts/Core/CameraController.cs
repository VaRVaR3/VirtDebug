using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float panSpeed = 10f;
    public float rotateSpeed = 100f;
    public float zoomSpeed = 20f;

    void Update()
    {
        // WASD для перемещения
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        transform.Translate(new Vector3(horizontal, 0, vertical) * panSpeed * Time.deltaTime);

        // Правая кнопка мыши для вращения
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up * mouseX, Space.World);
            transform.Rotate(Vector3.left * mouseY, Space.Self);
        }

        // Колесико для зума
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        transform.Translate(Vector3.forward * scroll * zoomSpeed * Time.deltaTime, Space.Self);
    }
}