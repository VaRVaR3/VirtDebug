using UnityEngine;

public class PinHighlighter : MonoBehaviour
{
    [Header("Pin Information")]
    public int pinNumber = 1;
    public PinMode pinMode = PinMode.Input;
    public bool isConnected = false;
    public bool isGroundPin = false;
    public bool isPowerPin = false;

    [Header("Tooltip")]
    public string pinDescription = "";
    public GameObject tooltipObject;

    [Header("Component Reference")]
    public MonoBehaviour parentMonoBehaviour;
    public IConnectable parentComponent { get; private set; }

    private Renderer pinRenderer;
    private TextMesh tooltipText;

    void Awake()
    {
        pinRenderer = GetComponent<Renderer>();
        if (pinRenderer == null)
            pinRenderer = GetComponentInChildren<Renderer>();

        InitializeTooltip();
        FindParentComponent();
        UpdatePinVisual();
        UpdateTooltip();
    }

    private void InitializeTooltip()
    {
        if (tooltipObject == null) return;

        tooltipText = tooltipObject.GetComponentInChildren<TextMesh>();
        tooltipObject.SetActive(false);
    }

    private void FindParentComponent()
    {
        if (parentMonoBehaviour != null)
        {
            parentComponent = parentMonoBehaviour as IConnectable;
            if (parentComponent != null) return;
        }

        Transform current = transform.parent;
        while (current != null)
        {
            var monos = current.GetComponents<MonoBehaviour>();
            foreach (var m in monos)
            {
                if (m is IConnectable c)
                {
                    parentComponent = c;
                    parentMonoBehaviour = m;
                    return;
                }
            }
            current = current.parent;
        }
    }

    public void SetConnected(bool connected)
    {
        isConnected = connected;
        UpdatePinVisual();
        UpdateTooltip();
    }

    public void SetPinMode(PinMode mode)
    {
        pinMode = mode;
        UpdatePinVisual();
        UpdateTooltip();
    }

    public void ShowTooltip(bool show)
    {
        if (tooltipObject != null)
            tooltipObject.SetActive(show);
    }

    private void UpdateTooltip()
    {
        if (tooltipText == null) return;

        string t = $"Pin {pinNumber}\n{pinMode}\n";
        if (!string.IsNullOrEmpty(pinDescription)) t += pinDescription + "\n";
        if (isGroundPin) t += "GND\n";
        else if (isPowerPin) t += "+5V\n";
        tooltipText.text = t;
    }

    private void UpdatePinVisual()
    {
        if (pinRenderer == null) return;

        if (isGroundPin)
        {
            pinRenderer.material.color = Color.black;
            return;
        }

        if (isPowerPin)
        {
            pinRenderer.material.color = Color.red;
            return;
        }

        if (isConnected)
        {
            pinRenderer.material.color = Color.cyan;
            return;
        }

        switch (pinMode)
        {
            case PinMode.Input: pinRenderer.material.color = Color.blue; break;
            case PinMode.Output: pinRenderer.material.color = Color.green; break;
            case PinMode.Analog: pinRenderer.material.color = Color.yellow; break;
            case PinMode.PWM: pinRenderer.material.color = Color.magenta; break;
            default: pinRenderer.material.color = Color.gray; break;
        }
    }
}
