using UnityEngine;

public class VirtualGround : MonoBehaviour, IConnectable
{
    [Header("Ground Settings")]
    public string groundName = "GND";
    public float voltage = 0f;

    [Header("Visual")]
    public Color groundColor = Color.black;
    public Material groundMaterial;

    void Start()
    {
        // Настраиваем визуализацию
        Renderer rend = GetComponent<Renderer>();
        if (rend != null && groundMaterial != null)
        {
            rend.material = groundMaterial;
        }
        else if (rend != null)
        {
            // Создаем простой материал если не назначен
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = groundColor;
            rend.material = mat;
        }

        Debug.Log($"Ground {name} initialized at {voltage}V");
    }

    // Реализация IConnectable
    public string GetName()
    {
        return groundName;
    }

    public void OnConnected(int pin, IConnectable otherComponent, int otherPin)
    {
        Debug.Log($"Ground {name} connected to {otherComponent.GetName()} pin {otherPin}");

        // Можно передать 0V подключенному компоненту
        CircuitComponent circuitComp = (otherComponent as MonoBehaviour)?.GetComponent<CircuitComponent>();
        if (circuitComp != null)
        {
            circuitComp.OnVoltageChanged(0f);
        }
    }

    public void OnDisconnected(int pin)
    {
        Debug.Log($"Ground {name} disconnected from pin {pin}");
    }

    public Vector3 GetPinPosition(int pin)
    {
        // Если есть дочерние объекты-пины, используем их
        PinHighlighter[] pins = GetComponentsInChildren<PinHighlighter>();
        foreach (PinHighlighter pinObj in pins)
        {
            if (pinObj.pinNumber == pin)
            {
                return pinObj.transform.position;
            }
        }

        // Иначе возвращаем позицию объекта с небольшим смещением
        return transform.position + new Vector3(0, 0.1f * pin, 0);
    }

    public bool CanConnectTo(int pin, IConnectable otherComponent, int otherPin)
    {
        // Земля может соединяться с любым компонентом, но обычно с отрицательными пинами
        return true;
    }

    // Метод для получения напряжения (всегда 0V)
    public float GetVoltage()
    {
        return voltage;
    }

    // Дополнительный метод для проверки, является ли объект землей
    public static bool IsGroundObject(GameObject obj)
    {
        if (obj == null) return false;

        VirtualGround ground = obj.GetComponent<VirtualGround>();
        if (ground != null) return true;

        // Проверка по имени
        string objName = obj.name.ToUpper();
        if (objName.Contains("GND") || objName.Contains("GROUND") || objName.Contains("EARTH"))
        {
            return true;
        }

        return false;
    }

    // Статический метод для поиска земли в сцене
    public static VirtualGround FindGroundInScene()
    {
        VirtualGround[] grounds = FindObjectsOfType<VirtualGround>();
        if (grounds.Length > 0)
        {
            return grounds[0];
        }

        // Ищем по имени
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (IsGroundObject(obj))
            {
                // Добавляем компонент VirtualGround если его нет
                VirtualGround ground = obj.GetComponent<VirtualGround>();
                if (ground == null)
                {
                    ground = obj.AddComponent<VirtualGround>();
                    Debug.Log($"Added VirtualGround component to {obj.name}");
                }
                return ground;
            }
        }

        // Создаем новый объект земли если нет
        Debug.LogWarning("No ground found in scene, creating one...");
        GameObject groundObj = new GameObject("GND");
        groundObj.transform.position = new Vector3(2, 0, 0);
        return groundObj.AddComponent<VirtualGround>();
    }
}