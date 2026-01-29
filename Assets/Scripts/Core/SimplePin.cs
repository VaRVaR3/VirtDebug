using UnityEngine;

public class SimplePin : MonoBehaviour
{
    public int pinNumber = 1;

    private Renderer pinRenderer;
    private Color originalColor;

    void Start()
    {
        pinRenderer = GetComponent<Renderer>();
        if (pinRenderer != null)
        {
            originalColor = pinRenderer.material.color;
        }

        // Добавляем коллайдер
        if (GetComponent<Collider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = 0.1f;
            collider.isTrigger = true;
        }
    }

    void OnMouseEnter()
    {
        if (pinRenderer != null)
            pinRenderer.material.color = Color.yellow;
    }

    void OnMouseExit()
    {
        if (pinRenderer != null)
            pinRenderer.material.color = originalColor;
    }

    void OnMouseDown()
    {
        Debug.Log($"CLICKED ON PIN {pinNumber}!");

        if (ConnectionManager.Instance != null)
        {
            // Ищем родительский IConnectable
            IConnectable parent = FindParentIConnectable();

            if (parent != null)
            {
                if (ConnectionManager.Instance.isConnecting)
                {
                    Debug.Log($"Завершаем соединение на пин {pinNumber}");
                    ConnectionManager.Instance.CompleteConnection(parent, pinNumber);
                }
                else
                {
                    Debug.Log($"Начинаем соединение с пина {pinNumber}");
                    ConnectionManager.Instance.StartConnection(parent, pinNumber);
                }
            }
        }
    }

    private IConnectable FindParentIConnectable()
    {
        // Ищем в родителях
        Transform current = transform;
        while (current != null)
        {
            MonoBehaviour[] components = current.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component is IConnectable connectable)
                    return connectable;
            }
            current = current.parent;
        }
        return null;
    }
}