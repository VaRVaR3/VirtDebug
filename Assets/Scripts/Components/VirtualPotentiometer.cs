using UnityEngine;

public class VirtualPotentiometer : CircuitComponent
{
    [Header("Potentiometer Settings")]
    public float minResistance = 0f;
    public float maxResistance = 10000f;
    public float currentResistance = 1000f;
    public float rotationAngle = 0f;
    public float maxRotation = 270f;

    private Transform knobTransform;
    private Vector3 initialMousePosition;
    private bool isDragging = false;

    void Start()
    {
        knobTransform = transform.Find("Knob");
        if (knobTransform == null)
        {
            GameObject knob = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            knob.name = "Knob";
            knob.transform.parent = transform;
            knob.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f);
            knob.transform.localPosition = new Vector3(0, 0.2f, 0);
            knobTransform = knob.transform;
        }

        resistance = currentResistance;
        maxVoltage = 50f;
        maxCurrent = 0.1f;
        requiresPolarity = false;

        UpdateKnobRotation();
    }

    void OnMouseDown()
    {
        if (!isActive) return;

        isDragging = true;
        initialMousePosition = Input.mousePosition;
    }

    void OnMouseDrag()
    {
        if (!isDragging || !isActive) return;

        float deltaY = Input.mousePosition.y - initialMousePosition.y;
        float resistanceChange = deltaY * (maxResistance - minResistance) / 300f;

        currentResistance = Mathf.Clamp(
            currentResistance + resistanceChange,
            minResistance,
            maxResistance
        );

        resistance = currentResistance;

        rotationAngle = Mathf.Lerp(0, maxRotation,
            (currentResistance - minResistance) / (maxResistance - minResistance));

        UpdateKnobRotation();

        initialMousePosition = Input.mousePosition;

        OnResistanceChanged();
    }

    void OnMouseUp()
    {
        isDragging = false;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            currentResistance = Mathf.Min(currentResistance + 10f, maxResistance);
            resistance = currentResistance;
            UpdateKnobRotation();
            OnResistanceChanged();
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            currentResistance = Mathf.Max(currentResistance - 10f, minResistance);
            resistance = currentResistance;
            UpdateKnobRotation();
            OnResistanceChanged();
        }
    }

    private void UpdateKnobRotation()
    {
        if (knobTransform != null)
        {
            knobTransform.localRotation = Quaternion.Euler(0, rotationAngle, 0);
        }
    }

    private void OnResistanceChanged()
    {
        if (positivePin >= 0 && connectedArduino != null)
        {
            float voltage = connectedArduino.DigitalRead(positivePin) * 5.0f;
            OnVoltageChanged(voltage);
        }

        Debug.Log($"Potentiometer resistance: {currentResistance}?");
    }

    public void SetResistance(float newResistance)
    {
        currentResistance = Mathf.Clamp(newResistance, minResistance, maxResistance);
        resistance = currentResistance;

        rotationAngle = Mathf.Lerp(0, maxRotation,
            (currentResistance - minResistance) / (maxResistance - minResistance));

        UpdateKnobRotation();
        OnResistanceChanged();
    }

    public float GetResistancePercentage()
    {
        return (currentResistance - minResistance) / (maxResistance - minResistance) * 100f;
    }

    public override void OnVoltageChanged(float voltage)
    {
        base.OnVoltageChanged(voltage);

        Renderer rend = GetComponent<Renderer>();
        if (rend != null && isActive)
        {
            float intensity = Mathf.Clamp01(voltage / maxVoltage);
            Color baseColor = Color.Lerp(Color.gray, new Color(1f, 0.65f, 0f), intensity);
            rend.material.color = baseColor;
        }
    }

    public override void UpdateComponent()
    {
        // Пустая реализация
    }
}