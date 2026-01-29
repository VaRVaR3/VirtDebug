using UnityEngine;

public class UpdatePositionOnSnap : MonoBehaviour
{
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    void Start()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    void Update()
    {
        // Если позиция или вращение изменились
        if (transform.position != lastPosition || transform.rotation != lastRotation)
        {
            // Находим все пины на этом компоненте
            PinHighlighter[] pins = GetComponentsInChildren<PinHighlighter>();
            foreach (PinHighlighter pin in pins)
            {
                // Обновляем визуальное состояние пина
                pin.transform.hasChanged = true;
            }

            lastPosition = transform.position;
            lastRotation = transform.rotation;
        }
    }
}